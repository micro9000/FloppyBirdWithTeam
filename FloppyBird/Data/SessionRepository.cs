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
            var result = await _cacheService.StringSetObjToCache<Session>(session.SessionToken.ToString(), session);
            return result ? session : null;
        }

        public async Task<Session> GetSessionbyToken(string sessionToken)
        {
            return await _cacheService.StringGetObjFromCache<Session>(sessionToken);
        }

    }
}
