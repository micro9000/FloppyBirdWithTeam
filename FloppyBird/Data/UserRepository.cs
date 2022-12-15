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
        Task UpdateUserHubConnectionId(string accountToken, string hubConnectionId);
    }

    public class UserRepository : IUserRepository
    {
        private readonly ICacheService _cacheService;

        public UserRepository(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public async Task<User> CreateNewUserAccount(string username)
        {
            var user = new User
            {
                AccountToken = Guid.NewGuid(),
                Name = username,
                Scores = new List<int>(),
                HubConnectionId = ""
            };

            var result = await this.SetUserInCache(user.AccountToken.ToString(), user);

            return result ? user : null;
        }

        public async Task UpdateUserHubConnectionId (string accountToken, string hubConnectionId)
        {
            var userInfo = await this.GetUserByAccountToken(accountToken);
            if (userInfo != null)
            {
                userInfo.HubConnectionId = hubConnectionId;
                await this.SetUserInCache(accountToken, userInfo);
            }
        }

        private async Task<bool> SetUserInCache(string accountToken, User user)
        {
            return await _cacheService.StringSetObjToCache<User>(accountToken, user);
        }

        public async Task<User> GetUserByAccountToken(string accountToken)
        {
            return await _cacheService.StringGetObjFromCache<User>(accountToken);
        }
    }
}
