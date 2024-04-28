using Protocol.QueueHubAndClient;
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
            var ticket = GetTicket(request.AccountId);
            var nowTicks = DateTime.Now.Ticks;

            var memberKey = $"{Consts.WaitingQueueTicketKey}{ticket}";
            var cacheSet = await redisDB.StringSetAsync(memberKey, ticket, TimeSpan.FromSeconds(300));
            if (cacheSet == false)
            {
                return MakeCommonErrorMessage("failed to save or set expiration on wait ticket");
            }

            var saved = await redisDB.SortedSetAddAsync(Consts.WaitingQueueKey, ticket, nowTicks);
            if (saved == false)
            {
                return MakeCommonErrorMessage($"failed to save wait ticket");
            }

            return new GetWaitngTicketResponse()
            {
                Ok = true,
                WaitingTicket = ticket,
            };
        }
        private string GetTicket(string accountId)
        {
            return _securityService.Encrypt(new TicketModel()
            {
                AccountId = accountId,
                IP = ControllerContext.HttpContext.Connection.RemoteIpAddress.ToString(),
                ExpirationTimeTicks = DateTime.Now.AddMinutes(10).Ticks
            });
        }
    }
}
