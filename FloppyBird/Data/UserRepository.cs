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
                Scores = new List<int>()
            };

            var result = await _cacheService.StringSetObjToCache<User>(user.AccountToken.ToString(), user);

            return result ? user : null;
        }

        public async Task<User> GetUserByAccountToken(string accountToken)
        {
            return await _cacheService.StringGetObjFromCache<User>(accountToken);
        }
    }
}
