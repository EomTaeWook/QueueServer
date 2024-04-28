namespace QueueHubServer.Models
{
    public class TicketModel
    {
        public string IP { get; set; }

        public string AccountId { get; set; }

        public long ExpirationTimeTicks { get; set; }
    }
}
