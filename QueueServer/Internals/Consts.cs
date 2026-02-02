namespace QueueServer.Internals
{
    public class Consts
    {
        public const string AvailableSessionsServerKey = "AvailableSessions:";
        public const string WaitingQueueKey = "WaitingQueue";
        public const string ExpirationQueueKey = "ExpirationQueue:";

        public readonly static TimeSpan WaitingHeartbeatTtl = TimeSpan.FromMinutes(5);
    }
}
