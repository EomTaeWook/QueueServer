using ShareModels.Network.Interface;

namespace Protocol.QueueHubAndClient
{
    public interface ICQHRequest : IAPIRequest
    {
    }

    public class GetWaitngTicket : ICQHRequest
    {
        public string AccountId { get; set; }
    }
    public class CheckWaitngTicket : ICQHRequest
    {
        public string Ticket { get; set; }

        public string ServerName { get; set; }
    }

    public class Login : ICQHRequest
    {
        public string WaitingTicket { get; set; }
        public string ServerName { get; set; }
        public string EntryTicket { get; set; }
    }

    public class IncreasedAvailableSession : ICQHRequest
    {
        public string ServerName { get; set; }

        public int SessionIncreaseCount { get; set; }
    }

    public class PurgeExpiredTickets : ICQHRequest
    {
        public int Range { get; set; }
    }
}
