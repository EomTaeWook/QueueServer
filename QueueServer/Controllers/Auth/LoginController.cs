using Protocol.QueueServerAndClient;
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

            if (ticketModel.AccountId != request.AccountId)
            {
                return MakeCommonErrorMessage("ticket ownership mismatch");
            }

            var redisDB = _redisService.GetDatabase();

            var serverSessionCountKey = $"{Consts.AvailableSessionsServerKey}{request.ServerName}";

            var sessionsCount = await redisDB.StringDecrementAsync(serverSessionCountKey, 1);

            if (sessionsCount < 0)
            {
                await redisDB.StringIncrementAsync(serverSessionCountKey);
                return MakeCommonErrorMessage("no available sessions");
            }

            var trans = redisDB.CreateTransaction();
            _ = trans.SortedSetRemoveAsync(Consts.WaitingQueueKey, request.WaitingTicket);
            _ = trans.SortedSetRemoveAsync(Consts.ExpirationQueueKey, request.WaitingTicket);
            var executed = await trans.ExecuteAsync();

            if (!executed)
            {
                await redisDB.StringIncrementAsync(serverSessionCountKey);
                return MakeCommonErrorMessage("transaction failed");
            }

            return new LoginResponse()
            {
                Ok = true,
            };
        }
    }
}
