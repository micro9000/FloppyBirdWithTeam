using FloppyBird.Data;
using FloppyBird.DomainModels;
using Microsoft.AspNetCore.SignalR;
using static System.Formats.Asn1.AsnWriter;

namespace FloppyBird.Hubs
{
    public class GameSessionHub : Hub
    {
        private readonly IUserRepository _userRepository;
        private readonly ISessionRepository _sessionRepository;

        public GameSessionHub(IUserRepository userRepository, ISessionRepository sessionRepository)
        {
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
        }

        public async Task AddUserToSession(string sessionToken, string userAccountToken)
        {
            if (Guid.TryParse(userAccountToken, out var currentUserAccountToken) && Guid.TryParse(sessionToken, out var currentSessionToken))
            {
                var userInfo = await _userRepository.GetUserByAccountToken(currentUserAccountToken.ToString());
                if (userInfo == null) return;
                string hubConnectionId = Context.ConnectionId;
                await _userRepository.UpdateUserHubConnectionId(userAccountToken, hubConnectionId);
                await Groups.AddToGroupAsync(hubConnectionId, currentSessionToken.ToString());
            }
        }

        public async Task SaveUserScore (SaveUserScoreParams scoreInfo)
        {
            if (Guid.TryParse(scoreInfo.UserAccountToken, out var currentUserAccountToken) 
                && Guid.TryParse(scoreInfo.SessionToken, out var currentSessionToken))
            {
                var saveResult = await _sessionRepository.AddUserScore(currentUserAccountToken, currentSessionToken, scoreInfo.Score);

                if (saveResult)
                {
                    var session = await _sessionRepository.GetSessionbyToken(currentSessionToken.ToString());
                    if (session.IsStarted)
                    {
                        var scoreBoard = new SessionScorecard(session?.Users);
                        await Clients.Group(currentSessionToken.ToString()).SendAsync("ScoreboardUpdated", scoreBoard);
                    }
                }
            }
        }
    }

    public class SaveUserScoreParams
    {
        public string SessionToken { get; set; }
        public string UserAccountToken { get; set; }
        public int Score { get; set; }
    }
}