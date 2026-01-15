using StackExchange.Redis;

namespace Data
{
    public class RedisClient
    {
        private readonly IConnectionMultiplexer _mux;
        public RedisClient(IConnectionMultiplexer mux) => _mux = mux;

        public async Task<string?> GetAsync(string key)
        {
            var value = await _mux.GetDatabase().StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }

        public Task SetAsync(string key, string value, TimeSpan? expiry = null)
            => _mux.GetDatabase().StringSetAsync(
                key,
                value,
                expiry,
                When.Always // ¼öÁ¤
            );

        // Pub/Sub
        public void Subscribe(string channel, Action<RedisChannel, RedisValue> handler)
            => _mux.GetSubscriber().Subscribe(RedisChannel.Literal(channel), (ch, msg) => handler(ch, msg));

        public Task PublishAsync(string channel, string message)
            => _mux.GetSubscriber().PublishAsync(RedisChannel.Literal(channel), message);
    }
}
