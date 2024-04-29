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
                WaitingCount = waitingCount.Value + 1,
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
