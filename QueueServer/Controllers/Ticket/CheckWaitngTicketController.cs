using Protocol.QueueServerAndClient;
using QueueServer.Internals;
using QueueServer.Services;
using ShareModels.Network.Interface;

namespace QueueServer.Controllers.Ticket
{
    public class CheckWaitngTicketController : APIController<CheckWaitngTicket>
    {
        private readonly RedisService _redisService;
        private readonly TicketHelperService _ticketHelperService;

        public CheckWaitngTicketController(RedisService redisService,
            TicketHelperService ticketHelperService)
        {
            _redisService = redisService;
            _ticketHelperService = ticketHelperService;
        }
        protected override async Task<IAPIResponse> Process(CheckWaitngTicket request)
        {
            var ticketModel = _ticketHelperService.Deserialize(request.Ticket);

            if (ticketModel == null)
            {
                return MakeCommonErrorMessage("invalid ticket");
            }

            if (ticketModel.ExpirationTimeTicks < DateTime.Now.Ticks)
            {
                return MakeCommonErrorMessage("the ticket has expired. please request a new ticket");
            }

            var redisDB = _redisService.GetDatabase();

            var newExpirationTicks = DateTime.Now.Add(Consts.WaitingHeartbeatTtl).Ticks;

            var expireAtScore = await redisDB.SortedSetScoreAsync(Consts.ExpirationQueueKey, request.Ticket);

            if (expireAtScore == null)
            {
                return MakeCommonErrorMessage("ticket is no longer active. please request a new ticket");
            }

            if (expireAtScore.Value <= DateTime.Now.Ticks)
            {
                _ = redisDB.SortedSetRemoveAsync(Consts.WaitingQueueKey, request.Ticket);
                _ = redisDB.SortedSetRemoveAsync(Consts.ExpirationQueueKey, request.Ticket);

                return MakeCommonErrorMessage("ticket is no longer active. please request a new ticket");
            }

            var refreshed = await redisDB.SortedSetAddAsync(Consts.ExpirationQueueKey, request.Ticket, newExpirationTicks);

            if (!refreshed)
            {
                return MakeCommonErrorMessage("failed to refresh ticket heartbeat");
            }

            var rank = await redisDB.SortedSetRankAsync(Consts.WaitingQueueKey,
                request.Ticket,
                StackExchange.Redis.Order.Ascending);

            if (rank == null)
            {
                return MakeCommonErrorMessage($"failed to load waitng count");
            }

            var waitingCount = rank.Value + 1;

            var availableSessionsValue = await redisDB.StringGetAsync($"{Consts.AvailableSessionsServerKey}{request.ServerName}");

            var availableSessions = 0;
            if (availableSessionsValue.IsInteger)
            {
                availableSessions = (int)availableSessionsValue;
            }

            if (availableSessions <= 0)
            {
                return new CheckWaitngTicketResponse()
                {
                    WaitingCount = waitingCount,
                    Ok = true,
                };
            }

            var entryTicket = string.Empty;

            if (availableSessions > 0 && rank.Value < availableSessions)
            {
                entryTicket = GetEntryTicket(request.AccountId);
            }
            
            return new CheckWaitngTicketResponse()
            {
                WaitingCount = waitingCount,
                Ok = true,
                EntryTicket = entryTicket,
            };
        }
        private string GetEntryTicket(string accountId)
        {
            return _ticketHelperService.Generation(accountId,
                HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString());
        }
    }
}
