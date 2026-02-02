using Protocol.QueueServerAndClient;
using QueueServer.Internals;
using QueueServer.Services;
using ShareModels.Network.Interface;
using StackExchange.Redis;

namespace QueueServer.Controllers.Ticket
{
    public class PurgeExpiredTicketsController : APIController<PurgeExpiredTickets>
    {
        private readonly RedisService _redisService;
        public PurgeExpiredTicketsController(RedisService redisService)
        {
            _redisService = redisService;
        }
        protected override async Task<IAPIResponse> Process(PurgeExpiredTickets request)
        {
            if (request.Range <= 0)
            {
                return new PurgeExpiredTicketsResponse { Ok = true, RemoveTicketCount = 0 };
            }

            var redisDB = _redisService.GetDatabase();
            var nowTicks = DateTime.Now.Ticks;

            var expiredTickets = await redisDB.SortedSetRangeByScoreAsync(
                Consts.ExpirationQueueKey,
                start: double.NegativeInfinity,
                stop: nowTicks,
                exclude: Exclude.None,
                order: Order.Ascending,
                skip: 0,
                take: request.Range);

            if (expiredTickets.Length == 0)
            {
                return new PurgeExpiredTicketsResponse { Ok = true, RemoveTicketCount = 0 };
            }

            var batch = redisDB.CreateBatch();
            var t1 = batch.SortedSetRemoveAsync(Consts.ExpirationQueueKey, expiredTickets);
            var t2 = batch.SortedSetRemoveAsync(Consts.WaitingQueueKey, expiredTickets);
            batch.Execute();

            await Task.WhenAll(t1, t2);

            return new PurgeExpiredTicketsResponse()
            {
                RemoveTicketCount = (int)expiredTickets.Length,
                Ok = true,
            };
        }
    }
}
