using FloppyBird.Cache;
using FloppyBird.DomainModels;
using FloppyBird.Dtos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace FloppyBird.Data
{
    public interface ISessionRepository
    {
        Task<Session> CreateNewSession(CreateNewSessionParams createNewSessionParams, string currentUserAccountToken);
        Task<Session> GetSessionbyToken(string sessionToken);
    }

    public class SessionRepository : ISessionRepository
    {
        private readonly Task<RedisConnection> _redisConnectionFactory;
        private TimeSpan _cacheExpirationTimeSpan;

        public SessionRepository(Task<RedisConnection> redisConnectionFactory, IOptions<RedisConfigOptions> redisConfigOptions)
        {
            _redisConnectionFactory = redisConnectionFactory;
            var options = redisConfigOptions.Value;
            _cacheExpirationTimeSpan = TimeSpan.FromHours(options.expirationInHr);
        }

        public async Task<Session> CreateNewSession(CreateNewSessionParams createNewSessionParams, string currentUserAccountToken)
        {
            var session = new Session
            {
                SessionToken = Guid.NewGuid(),
                Name = createNewSessionParams.Name,
                StartedAt = DateTime.UtcNow,
                GameMasterAccountToken = Guid.Parse(currentUserAccountToken)
            };

            var sessionJson = JsonSerializer.Serialize(session);

            var redisConnection = await _redisConnectionFactory;
            var result = await redisConnection.BasicRetryAsync(async db =>
                    await db.StringSetAsync(session.SessionToken.ToString(), sessionJson, _cacheExpirationTimeSpan));

            return result ? session : null;
        }

        public async Task<Session> GetSessionbyToken(string sessionToken)
        {
            var redisConnection = await _redisConnectionFactory;
            RedisValue getSessionResult = await redisConnection.BasicRetryAsync(async db => await db.StringGetAsync(sessionToken));

            if (getSessionResult != RedisValue.Null)
            {
                var sessionObj = JsonSerializer.Deserialize<Session>(getSessionResult);
                return sessionObj;
            }

            return null;
        }

    }
}
