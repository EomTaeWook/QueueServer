using Protocol.QueueHubAndClient;
using QueueServer.Internals;
using QueueServer.Services;
using ShareModels.Network.Interface;

namespace QueueServer.Controllers.GameServer
{
    public class IncreasedAvailableSessionController : APIController<IncreasedAvailableSession>
    {
        private readonly RedisService _redisService;
        public IncreasedAvailableSessionController(RedisService redisService)
        {
            _redisService = redisService;
        }
        protected override async Task<IAPIResponse> Process(IncreasedAvailableSession request)
        {
            var redisDB = _redisService.GetDatabase();

            await redisDB.StringIncrementAsync($"{Consts.AvailableSessionsServerKey}{request.ServerName}", request.SessionIncreaseCount);

            return new IncreasedAvailableSessionResponse()
            {
                Ok = true,
            };
        }
    }
}
