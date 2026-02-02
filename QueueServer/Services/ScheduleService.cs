using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Protocol.QueueServerAndClient;
using QueueServer.Internals;
using QueueServer.Internals.Interface;

namespace QueueServer.Services
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    public class ScheduleService : IModule
    {
        private CancellationTokenSource _stopCts;
        public ScheduleService()
        {
            _stopCts = new CancellationTokenSource();
        }

        public async Task StopAsync()
        {
            await _stopCts.CancelAsync();
        }

        public async Task StartAsync()
        {
            try
            {
                await Task.Delay(60 * 1000, _stopCts.Token);

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
            }
            catch(OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }

            _ = StartAsync();
        }
    }
}
