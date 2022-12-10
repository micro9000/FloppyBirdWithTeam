using FloppyBird.Cache;
using FloppyBird.DomainModels;
using FloppyBird.Dtos;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace FloppyBird.Data
{
    public interface IUserRepository
    {
        Task<User> CreateNewUserAccount(string username);
        Task<User> GetUserByAccountToken(string accountToken);
    }

    public class UserRepository : IUserRepository
    {
        private readonly Task<RedisConnection> _redisConnectionFactory;
        private TimeSpan _cacheExpirationTimeSpan;

        public UserRepository(Task<RedisConnection> redisConnectionFactory, IOptions<RedisConfigOptions> redisConfigOptions)
        {
            _redisConnectionFactory = redisConnectionFactory;
            var options = redisConfigOptions.Value;
            _cacheExpirationTimeSpan = TimeSpan.FromHours(options.expirationInHr);
        }

        public async Task<User> CreateNewUserAccount(string username)
        {
            var user = new User
            {
                AccountToken = Guid.NewGuid(),
                Name = username
            };

            var userJson = JsonSerializer.Serialize(user);

            var redisConnection = await _redisConnectionFactory;
            var result = await redisConnection.BasicRetryAsync(async db =>
                    await db.StringSetAsync(user.AccountToken.ToString(), userJson, _cacheExpirationTimeSpan));

            return result ? user : null;
        }

        public async Task<User> GetUserByAccountToken(string accountToken)
        {
            var redisConnection = await _redisConnectionFactory;
            RedisValue getUserResult = await redisConnection.BasicRetryAsync(async db => await db.StringGetAsync(accountToken));

            if (getUserResult != RedisValue.Null)
            {
                var userObj = JsonSerializer.Deserialize<User>(getUserResult);
                return userObj;
            }

            return null;
        }
    }
}
