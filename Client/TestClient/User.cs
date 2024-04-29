using Protocol.QueueServerAndClient;

namespace TestClient
{
    internal class User
    {
        public int Id { get => _id; }
        private int _id;
        private string _ticket;
        private string _entryTicket;
        public User(int id)
        {
            _id = id;
        }

        public void GetWaitngTicket()
        {
            var request = new GetWaitngTicket()
            {
                AccountId = _id.ToString(),
            };
            var response = RequestHelper.Request<GetWaitngTicket, GetWaitngTicketResponse>(request);

            _ticket = response.WaitingTicket;
        }

        public CheckWaitngTicketResponse CheckWaitngTicket()
        {
            var request = new CheckWaitngTicket()
            {
                Ticket = _ticket,
                ServerName = ShareModels.Consts.ServerName
            };
            var response = RequestHelper.Request<CheckWaitngTicket, CheckWaitngTicketResponse>(request);

            Console.WriteLine($"[Ok : {response.Ok}] user id : {_id} waiting count : {response.WaitingCount}");

            _entryTicket = response.EntryTicket;
            return response;
        }

        public void Login()
        {
            var request = new Login()
            {
                EntryTicket = _entryTicket,
                ServerName = ShareModels.Consts.ServerName,
                WaitingTicket = _ticket
            };
            var response = RequestHelper.Request<Login, LoginResponse>(request);

            Console.WriteLine($"[Login] client id : {_id} ok : {response.Ok}");
        }
    }
}
