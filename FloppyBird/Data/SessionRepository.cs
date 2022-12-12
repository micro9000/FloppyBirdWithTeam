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
        Task<bool> AddUserToSession(User user, Guid sessionToken);
        Task RemoveUserFromSession(Guid userAccountToken, Guid sessionToken);
        Task<bool> AddUserScore(Guid userAccountToken, Guid sessionToken, int score);
    }

    public class SessionRepository : ISessionRepository
    {
        private readonly ICacheService _cacheService;

        public SessionRepository(ICacheService cacheService)
        {
            _cacheService = cacheService;
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

            var result = await SetSessionInCache(session.SessionToken.ToString(), session);
            return result ? session : null;
        }

        public async Task<bool> AddUserToSession(User user, Guid sessionToken)
        {
            if (user == null) return false;
            string sessionTokenStr = sessionToken.ToString();
            var sessionInCache = await GetSessionbyToken(sessionTokenStr);

            if (sessionInCache.Users.Count == 0)
            {
                user.Group = Groups.Avengers;
                sessionInCache.Users = new List<User> { user };
                var result = await SetSessionInCache(sessionTokenStr, sessionInCache);
                return result;
            }
            else
            {
                if (sessionInCache.Users.FirstOrDefault(x => x.AccountToken == user.AccountToken) != null)
                    return false;

                sessionInCache.Users.Add(user);

                // User Groupings
                for (int i = 0; i < sessionInCache.Users.Count; i++)
                {
                    var currentUser = sessionInCache.Users[i];
                    if (currentUser.Group == Groups.NoGroup)
                    {
                        if (i % 2 == 0)
                            currentUser.Group = Groups.Avengers;
                        else
                            currentUser.Group = Groups.JusticeLeague;
                    }
                }

                return await SetSessionInCache(sessionTokenStr, sessionInCache);
            }
        }

        public async Task RemoveUserFromSession(Guid userAccountToken, Guid sessionToken)
        {
            var sessionInCache = await this.GetSessionbyToken(sessionToken.ToString());
            if (sessionInCache == null)
                return;

            var user = sessionInCache.Users.SingleOrDefault(x => x.AccountToken == userAccountToken);

            if (user != null)
            {
                sessionInCache.Users.Remove(user);
                await SetSessionInCache(sessionToken.ToString(), sessionInCache);
            }
        }

        public async Task<bool> AddUserScore(Guid userAccountToken, Guid sessionToken, int score)
        {
            var sessionInCache = await this.GetSessionbyToken(sessionToken.ToString());
            if (sessionInCache == null)
                return false;

            int userIndex = sessionInCache.Users.FindIndex(x => x.AccountToken == userAccountToken);
            var user = sessionInCache.Users[userIndex];
            if (user != null)
            {
                user.Scores.Add(score);
                sessionInCache.Users[userIndex] = user;

                var saveResult = await SetSessionInCache(sessionToken.ToString(), sessionInCache);
                return saveResult;
            }

            return false;
        }

        private async Task<bool> SetSessionInCache(string sessionToken, Session session)
        {
            return await _cacheService.StringSetObjToCache<Session>(sessionToken, session);
        }

        public async Task<Session> GetSessionbyToken(string sessionToken)
        {
            return await _cacheService.StringGetObjFromCache<Session>(sessionToken);
        }

    }
}
