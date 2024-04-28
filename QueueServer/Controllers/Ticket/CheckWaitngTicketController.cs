using Dignus.Log;
using Protocol.QueueHubAndClient;
using QueueHubServer.Internals;
using QueueHubServer.Models;
using QueueHubServer.Service;
using ShareModels.Network.Interface;
using System.Text.Json;

namespace QueueHubServer.Controllers.Ticket
{
    public class CheckWaitngTicketController : APIController<CheckWaitngTicket>
    {
        private readonly RedisService _redisService;
        private readonly SecurityService _securityService;
        public CheckWaitngTicketController(RedisService redisService,
            SecurityService securityService)
        {
            _redisService = redisService;
            _securityService = securityService;
        }
        protected override async Task<IAPIResponse> Process(CheckWaitngTicket request)
        {
            try
            {
                var decryptJson = _securityService.DecryptString(request.Ticket);
                var ticketModel = JsonSerializer.Deserialize<TicketModel>(decryptJson);

                if (ticketModel.ExpirationTimeTicks < DateTime.Now.Ticks)
                {
                    return MakeCommonErrorMessage("the ticket has expired. please request a new ticket");
                }
            }
            catch (Exception e)
            {
                LogHelper.Error(e);
                return MakeCommonErrorMessage("invalid ticket");
            }

            var redisDB = _redisService.GetDatabase();

            var memberKey = $"{Consts.WaitingQueueTicketKey}{request.Ticket}";
            var cacheSet = await redisDB.StringSetAsync(memberKey, request.Ticket, TimeSpan.FromSeconds(300));

            if (cacheSet == false)
            {
                return MakeCommonErrorMessage("failed to save or set expiration on wait ticket");
            }

            var waitingCount = await redisDB.SortedSetRankAsync(Consts.WaitingQueueKey,
                request.Ticket,
                StackExchange.Redis.Order.Ascending);

            if (waitingCount == null)
            {
                return MakeCommonErrorMessage($"failed to load waitng count");
            }

            var availableSessions = (int)await redisDB.StringGetAsync($"{Consts.AvailableSessionsServerKey}{request.ServerName}");
            if (availableSessions <= 0)
            {
                return new CheckWaitngTicketResponse()
                {
                    WaitingCount = waitingCount.Value,
                    Ok = true,
                };
            }

            var candidateTickets = await redisDB.SortedSetRangeByRankAsync(Consts.WaitingQueueKey,
                0,
                availableSessions - 1,
                StackExchange.Redis.Order.Ascending);

            var entryTicket = string.Empty;
            foreach (var ticket in candidateTickets)
            {
                if (ticket == request.Ticket)
                {
                    entryTicket = GetEntryTicket(request.ServerName);
                    break;
                }
            }
            return new CheckWaitngTicketResponse()
            {
                WaitingCount = waitingCount.Value,
                Ok = true,
                EntryTicket = entryTicket,
            };
        }
        private string GetEntryTicket(string accountId)
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
