using CommandSystem;
using Dignus.Extensions.DependencyInjection;
using Dignus.Log;
using Protocol.QueueServerAndClient;
using QueueServer.Internals;
using QueueServer.Internals.Interface;
using QueueServer.Models;
using QueueServer.Services;
using ShareModels.Network.Interface;
using System.Reflection;
using System.Text.Json;

internal class Program
{
    private static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        LogBuilder.Configuration(LogConfigXmlReader.Load("DignusLog.config")).Build();
#if DEBUG
        Environment.CurrentDirectory = AppContext.BaseDirectory;
#endif
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        InitDependency(builder);

        InitCacheRequest();
        InitCmdSystem(builder);
        var app = builder.Build();

        app.MapControllers();
        app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (Exception ex)
            {
                LogHelper.Fatal(ex);
            }
        });

        StartModule(app.Services);
        app.Run();
    }
    private static void InitCmdSystem(WebApplicationBuilder builder)
    {
        var localCmdModule = new LocalCmdModule();
        builder.Services.AddSingleton(localCmdModule);

        localCmdModule.AddCommandAction("inc", "서버 가용 인원 늘리기", (args, cancellationToken) =>
        {
            var response = RequestHelper.Request<IncreasedAvailableSession, IncreasedAvailableSessionResponse>(new IncreasedAvailableSession()
            {
                ServerName = ShareModels.Consts.ServerName,
                SessionIncreaseCount = 1,
            });
            return Task.CompletedTask;
        });

        localCmdModule.AddCommandAction("purge", "만료 토큰 갱신", (args, cancellationToken) =>
        {
            var response = RequestHelper.Request<PurgeExpiredTickets, PurgeExpiredTicketsResponse>(new PurgeExpiredTickets());
            return Task.CompletedTask;
        });

        localCmdModule.Build();
    }

    private static void StartModule(IServiceProvider serviceProvider)
    {
        var modules = serviceProvider.GetServices<IModule>();

        foreach (var item in modules)
        {
            item.StartAsync();
        }
    }

    private static void InitDependency(WebApplicationBuilder builder)
    {
        var configPath = $"./config.json";
        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<Config>(json);
        builder.Services.AddSingleton<Config>(config);
        builder.Services.AddSingleton<RedisConfig>(config.Redis);

        builder.Services.AddSingleton<IModule, LocalCmdSystem>();
        builder.Services.AddSingleton<IModule, ScheduleService>();

        builder.Services.RegisterDependencies(Assembly.GetExecutingAssembly());
    }
    private static void InitCacheRequest()
    {
        string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string[] dllFiles = Directory.GetFiles(currentDirectory, "*.dll");

        foreach (string dllPath in dllFiles)
        {
            Assembly assembly = Assembly.LoadFrom(dllPath);
            RequestValidator.CacheRequestProperties(assembly, typeof(IAPIRequest), typeof(ICQHRequest));
        }
    }
    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        LogHelper.Fatal(e.ExceptionObject as Exception);
    }
}