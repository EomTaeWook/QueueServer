using Dignus.Log;
using Protocol.QueueHubAndClient;
using QueueHubServer.Internals;
using QueueHubServer.Models;
using QueueHubServer.Service;
using ShareModels.Network.Interface;
using System.Text.Json;

namespace QueueHubServer.Controllers.Auth
{
    public class LoginController : APIController<Login>
    {
        private readonly RedisService _redisService;
        private readonly SecurityService _securityService;
        public LoginController(RedisService redisService,
            SecurityService securityService)
        {
            _redisService = redisService;
            _securityService = securityService;
        }
        protected override async Task<IAPIResponse> Process(Login request)
        {
            TicketModel ticketModel;
            try
            {
                var decryptJson = _securityService.DecryptString(request.EntryTicket);
                ticketModel = JsonSerializer.Deserialize<TicketModel>(decryptJson);
            }
            catch (Exception e)
            {
                LogHelper.Error(e);
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
