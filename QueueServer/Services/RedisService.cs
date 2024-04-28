using Dignus.DependencyInjection.Attributes;
using QueueServer.Models;
using StackExchange.Redis;

namespace QueueHubServer.Service
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    public class RedisService
    {
        private IDatabase _redisDb;
        private ConnectionMultiplexer _redisConnection;
        private readonly ConfigurationOptions _option;
        public RedisService(RedisConfig redisConfig)
        {
            var connectionString = $"{redisConfig.EndPoint}:{redisConfig.Port}";
            _option = ConfigurationOptions.Parse(connectionString);
            _option.AllowAdmin = true;
            _option.ConnectTimeout = 10000;
            _option.SyncTimeout = 10000;
            Init();
        }
        public IDatabase GetDatabase()
        {
            if (_redisDb == null)
            {
                Init();
            }
            return _redisDb;
        }
        private void Init()
        {
            _redisConnection = ConnectionMultiplexer.Connect(_option);

            if (_redisConnection.IsConnected)
            {
                _redisDb = _redisConnection.GetDatabase();
            }
        }
    }
}
