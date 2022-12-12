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
                var userInfo = await _userRepository.GetUserByAccountToken(userAccountToken);
                if (userInfo == null) return;
                await Groups.AddToGroupAsync(Context.ConnectionId, sessionToken);
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
                    var sessionUser = await _sessionRepository.GetSessionbyToken(currentSessionToken.ToString());
                    var scoreBoard = new SessionScorecard(sessionUser?.Users);
                    await Clients.Group(currentSessionToken.ToString()).SendAsync("ScoreboardUpdated", scoreBoard);
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