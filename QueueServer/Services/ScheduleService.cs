using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Protocol.QueueHubAndClient;
using QueueHubServer.Internals;
using QueueServer.Internals.Interface;

namespace QueueServer.Services
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    public class ScheduleService : IModule
    {
        public ScheduleService()
        {
        }

        public async Task StartAsync()
        {
            await Task.Delay(300 * 1000);

            var response = RequestHelper.Request<PurgeExpiredTickets, PurgeExpiredTicketsResponse>(new PurgeExpiredTickets()
            {
                Range = 10
            });
            if (response == null)
            {
                LogHelper.Error($"purge expired tickets response is null");
            }
            if (response.Ok == false)
            {
                LogHelper.Error($"purge expired tickets response is not ok. error : {response.ErrorMessage}");
            }

            _ = StartAsync();
        }
    }
}
