using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using QueueServer.Models;
using System.Text.Json;

namespace QueueServer.Services
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    public class TicketHelperService
    {
        private readonly SecurityService _securityService;
        public TicketHelperService(SecurityService securityService)
        {
            _securityService = securityService;
        }

        public TicketModel Deserialize(string secretTicket)
        {
            try
            {
                var decryptJson = _securityService.DecryptString(secretTicket);
                return JsonSerializer.Deserialize<TicketModel>(decryptJson);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
            return null;
        }
        public string Generation(string accountId, string ipAddressString)
        {
            return _securityService.Encrypt(new TicketModel()
            {
                AccountId = accountId,
                IP = ipAddressString,
                ExpirationTimeTicks = DateTime.Now.AddMinutes(10).Ticks
            });
        }
    }
}
