using Protocol.QueueServerAndClient;
using QueueServer.Internals;
using QueueServer.Services;
using ShareModels.Network.Interface;

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
            var redisDB = _redisService.GetDatabase();
            var candidateTickets = await redisDB.SortedSetRangeByRankAsync(Consts.WaitingQueueKey, 0, request.Range - 1, StackExchange.Redis.Order.Ascending);
            var removedCount = 0;
            foreach (var ticket in candidateTickets)
            {
                var memberKey = $"{Consts.WaitingQueueTicketKey}{ticket}";
                if (!await redisDB.KeyExistsAsync(memberKey))
                {
                    await redisDB.SortedSetRemoveAsync(Consts.WaitingQueueKey, ticket);
                    removedCount++;
                }
            }
            return new PurgeExpiredTicketsResponse()
            {
                RemoveTicketCount = removedCount,
                Ok = true,
            };
        }
    }
}
