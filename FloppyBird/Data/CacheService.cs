using FloppyBird.Cache;
using FloppyBird.DomainModels;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace FloppyBird.Data
{
    public interface ICacheService
    {
        Task<T> StringGetObjFromCache<T>(string cacheKey) where T : class;
        Task<bool> StringSetObjToCache<T>(string cacheKey, T obj) where T : class;
    }

    public class CacheService : ICacheService
    {
        private readonly Task<RedisConnection> _redisConnectionFactory;
        private TimeSpan _cacheExpirationTimeSpan;

        public CacheService(Task<RedisConnection> redisConnectionFactory, IOptions<RedisConfigOptions> redisConfigOptions)
        {
            _redisConnectionFactory = redisConnectionFactory;
            var options = redisConfigOptions.Value;
            _cacheExpirationTimeSpan = TimeSpan.FromHours(options.expirationInHr);
        }

        public async Task<bool> StringSetObjToCache<T>(string cacheKey, T obj) where T : class
        {
            var objJson = JsonSerializer.Serialize(obj);

            var redisConnection = await _redisConnectionFactory;
            var result = await redisConnection.BasicRetryAsync(async db =>
                    await db.StringSetAsync(cacheKey, objJson, _cacheExpirationTimeSpan));

            return result;
        }

        public async Task<T> StringGetObjFromCache<T>(string cacheKey) where T : class
        {
            var redisConnection = await _redisConnectionFactory;
            RedisValue getUserResult = await redisConnection.BasicRetryAsync(async db => await db.StringGetAsync(cacheKey));

            if (getUserResult != RedisValue.Null)
            {
                var userObj = JsonSerializer.Deserialize<T>(getUserResult);
                return userObj;
            }

            return null;
        }
    }
}
