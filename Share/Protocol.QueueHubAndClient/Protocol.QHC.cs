using ShareModels.Network.Interface;

namespace Protocol.QueueHubAndClient
{
    public abstract class ServerResponseBase : IAPIResponse
    {
        public bool IsInMaintenance { get; set; }
        public bool Ok { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class GetWaitngTicketResponse : ServerResponseBase
    {
        public string WaitingTicket { get; set; }
    }

    public class CheckWaitngTicketResponse : ServerResponseBase
    {
        public long WaitingCount { get; set; }

        public string EntryTicket { get; set; }
    }

    public class IncreasedAvailableSessionResponse : ServerResponseBase
    {
    }

    public class LoginResponse : ServerResponseBase
    {

    }

    public class PurgeExpiredTicketsResponse : ServerResponseBase
    {
        public int RemoveTicketCount { get; set; }
    }
}
