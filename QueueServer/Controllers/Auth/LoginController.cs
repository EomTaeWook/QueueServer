using Protocol.QueueHubAndClient;
using QueueServer.Internals;
using QueueServer.Services;
using ShareModels.Network.Interface;

namespace QueueServer.Controllers.Auth
{
    public class LoginController : APIController<Login>
    {
        private readonly RedisService _redisService;
        private readonly TicketHelperService _ticketHelperService;
        public LoginController(RedisService redisService,
            TicketHelperService ticketHelperService)
        {
            _ticketHelperService = ticketHelperService;
            _redisService = redisService;
        }

        protected override async Task<IAPIResponse> Process(Login request)
        {
            var ticketModel = _ticketHelperService.Deserialize(request.EntryTicket);

            if (ticketModel == null)
            {
                return MakeCommonErrorMessage("invalid ticket");
            }

            if (ticketModel.ExpirationTimeTicks < DateTime.Now.Ticks)
            {
                return MakeCommonErrorMessage("the entry ticket has expired.");
            }

            var memberKey = $"{Consts.WaitingQueueTicketKey}{request.WaitingTicket}";

            var redisDB = _redisService.GetDatabase();

            var trans = redisDB.CreateTransaction();
            var removeWaitingKey = trans.SortedSetRemoveAsync(Consts.WaitingQueueKey, request.WaitingTicket);
            var removeMemberKey = trans.KeyDeleteAsync(memberKey);
            var currentSessions = trans.StringDecrementAsync($"{Consts.AvailableSessionsServerKey}{request.ServerName}", 1);

            var executed = await trans.ExecuteAsync();

            if (!executed)
            {
                return MakeCommonErrorMessage("transaction failed");
            }
            long sessionsCount = await currentSessions;
            if (sessionsCount < 0)
            {
                await redisDB.StringSetAsync($"{Consts.AvailableSessionsServerKey}{request.ServerName}", 0);
                return MakeCommonErrorMessage("no available sessions");
            }

            return new LoginResponse()
            {
                Ok = true,
            };
        }
    }
}
