using FloppyBird.Cache;
using FloppyBird.DomainModels;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace FloppyBird.Data
{
    public interface ISessionUsersRepository
    {
        Task AddUserToSession(User user, Guid sessionToken);
        Task RemoveUserFromSession(Guid userAccountToken, Guid sessionToken);
        Task<SessionUser> GetSessionUserByToken(string sessionToken);
    }

    public class SessionUsersRepository : ISessionUsersRepository
    {
        private readonly Task<RedisConnection> _redisConnectionFactory;
        private TimeSpan _cacheExpirationTimeSpan;

        public SessionUsersRepository(Task<RedisConnection> redisConnectionFactory, 
                                    IOptions<RedisConfigOptions> redisConfigOptions)
        {
            _redisConnectionFactory = redisConnectionFactory;
            var options = redisConfigOptions.Value;
            _cacheExpirationTimeSpan = TimeSpan.FromHours(options.expirationInHr);
        }

        private string GetToken(string sessionToken) => $"currentSessionUsers_{sessionToken}";

        public async Task AssignUsersToGroup (Guid sessionToken)
        {
            var sessionUserInCache = await this.GetSessionUserByToken(sessionToken.ToString());
            sessionUserInCache.Users.Shuffle();

            for (int i = 0; i < sessionUserInCache.Users.Count; i++)
            {
                if (i % 2 == 0)
                    sessionUserInCache.Users[i].Group = Groups.Avengers;
                else
                    sessionUserInCache.Users[i].Group = Groups.JusticeLeague;
            }

            var sessionUserToken = GetToken(sessionToken.ToString());
            await SetSessionUserByToken(sessionUserToken, sessionUserInCache);
        }

        public async Task AddUserToSession(User user, Guid sessionToken)
        {
            var sessionUserInCache = await this.GetSessionUserByToken(sessionToken.ToString());
            var sessionUserToken = GetToken(sessionToken.ToString());

            if (sessionUserInCache == null)
            {
                var newSessionUser = new SessionUser
                {
                    SessionToken = sessionToken,
                    Users = new List<User> { user }
                };

                await SetSessionUserByToken(sessionUserToken, newSessionUser);
            }
            else
            {
                if (sessionUserInCache.Users.FirstOrDefault(x => x.AccountToken == user.AccountToken) != null)
                    return;

                sessionUserInCache.Users.Add(user);
                await SetSessionUserByToken(sessionUserToken, sessionUserInCache);
            }
        }

        public async Task RemoveUserFromSession (Guid userAccountToken, Guid sessionToken)
        {
            var sessionUserInCache = await this.GetSessionUserByToken(sessionToken.ToString());
            if (sessionUserInCache == null) 
                return;

            var user = sessionUserInCache.Users.SingleOrDefault(x => x.AccountToken == userAccountToken);

            if (user != null)
            {
                sessionUserInCache.Users.Remove(user);
                var sessionUserToken = GetToken(sessionToken.ToString());
                await SetSessionUserByToken(sessionUserToken, sessionUserInCache);
            }
        }

        private async Task SetSessionUserByToken(string sessionUserToken, SessionUser sessionUser)
        {
            var sessionUserJson = JsonSerializer.Serialize(sessionUser);
            var redisConnection = await _redisConnectionFactory;
            await redisConnection.BasicRetryAsync(async db =>
                await db.StringSetAsync(sessionUserToken, sessionUserJson, _cacheExpirationTimeSpan));
        }

        public async Task<SessionUser> GetSessionUserByToken(string sessionToken)
        {
            var sessionUserToken = GetToken(sessionToken.ToString());
            var redisConnection = await _redisConnectionFactory;
            RedisValue getSessionUserResult = await redisConnection.BasicRetryAsync(async db => await db.StringGetAsync(sessionUserToken));

            if (getSessionUserResult != RedisValue.Null)
            {
                var sessionUserObj = JsonSerializer.Deserialize<SessionUser>(getSessionUserResult);
                return sessionUserObj;
            }

            return null;
        }
    }
}
