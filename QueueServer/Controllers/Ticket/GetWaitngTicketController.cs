using Protocol.QueueServerAndClient;
using QueueServer.Internals;
using QueueServer.Models;
using QueueServer.Services;
using ShareModels.Network.Interface;

namespace QueueServer.Controllers.Ticket
{
    public class GetWaitngTicketController : APIController<GetWaitngTicket>
    {
        private readonly RedisService _redisService;
        private readonly SecurityService _securityService;
        public GetWaitngTicketController(RedisService redisService,
            SecurityService securityService)
        {
            _redisService = redisService;
            _securityService = securityService;
        }
        protected override async Task<IAPIResponse> Process(GetWaitngTicket request)
        {
            var redisDB = _redisService.GetDatabase();

            var nowTime = DateTime.Now;
            var expirationTimeTicks = nowTime.Add(Consts.WaitingHeartbeatTtl).Ticks;
            var ticket = GetTicket(request.AccountId, expirationTimeTicks);

            var savedQueue = await redisDB.SortedSetAddAsync(Consts.WaitingQueueKey, ticket, nowTime.Ticks);

            if (!savedQueue)
            {
                return MakeCommonErrorMessage("failed to save wait ticket");
            }

            var expireAtTicks = nowTime.Ticks + Consts.WaitingHeartbeatTtl.Ticks;
            var savedExpiry = await redisDB.SortedSetAddAsync(Consts.ExpirationQueueKey, ticket, expireAtTicks);
            if (!savedExpiry)
            {
                _ = redisDB.SortedSetRemoveAsync(Consts.WaitingQueueKey, ticket);
                return MakeCommonErrorMessage("failed to save wait ticket expiry");
            }

            return new GetWaitngTicketResponse()
            {
                Ok = true,
                WaitingTicket = ticket,
            };
        }
        private string GetTicket(string accountId, long expirationTimeTicks)
        {
            return _securityService.Encrypt(new TicketModel()
            {
                AccountId = accountId,
                IP = ControllerContext.HttpContext.Connection.RemoteIpAddress.ToString(),
                ExpirationTimeTicks = expirationTimeTicks
            });
        }
    }
}
