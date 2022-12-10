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
        Task<bool> AddUserScore(Guid userAccountToken, Guid sessionToken, int score);
        Task AddUserToSession(User user, Guid sessionToken);
        Task RemoveUserFromSession(Guid userAccountToken, Guid sessionToken);
        Task<SessionUser> GetSessionUserByToken(string sessionToken);
    }

    public class SessionUsersRepository : ISessionUsersRepository
    {
        private readonly ICacheService _cacheService;

        public SessionUsersRepository(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        private string GetToken(string sessionToken) => $"currentSessionUsers_{sessionToken}";

        public async Task<bool> AddUserScore(Guid userAccountToken, Guid sessionToken, int score)
        {
            var sessionUserInCache = await this.GetSessionUserByToken(sessionToken.ToString());
            if (sessionUserInCache == null)
                return false;

            int userIndex = sessionUserInCache.Users.FindIndex(x => x.AccountToken == userAccountToken);
            var user = sessionUserInCache.Users[userIndex];
            if (user != null)
            {
                user.Scores.Add(score);
                sessionUserInCache.Users[userIndex] = user;

                var sessionUserToken = GetToken(sessionToken.ToString());
                var saveResult = await SetSessionUserByToken(sessionUserToken, sessionUserInCache);
                return saveResult;
            }

            return false;
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

                // User Groupings
                for (int i = 0; i < sessionUserInCache.Users.Count; i++)
                {
                    var currentUser = sessionUserInCache.Users[i];
                    if (currentUser.Group == Groups.NoGroup)
                    {
                        if (i % 2 == 0)
                            currentUser.Group = Groups.Avengers;
                        else
                            currentUser.Group = Groups.JusticeLeague;
                    }
                }

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

        private async Task<bool> SetSessionUserByToken(string sessionUserToken, SessionUser sessionUser)
        {
            return await _cacheService.StringSetObjToCache<SessionUser>(sessionUserToken, sessionUser);
        }

        public async Task<SessionUser> GetSessionUserByToken(string sessionToken)
        {
            var sessionUserToken = GetToken(sessionToken.ToString());
            return await _cacheService.StringGetObjFromCache<SessionUser>(sessionUserToken);
        }
    }
}
