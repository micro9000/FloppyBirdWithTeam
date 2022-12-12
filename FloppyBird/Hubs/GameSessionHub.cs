using FloppyBird.Data;
using Microsoft.AspNetCore.SignalR;
using static System.Formats.Asn1.AsnWriter;

namespace FloppyBird.Hubs
{
    public class GameSessionHub : Hub
    {
        private readonly IUserRepository _userRepository;
        private readonly ISessionUsersRepository _sessionUsersRepository;

        public GameSessionHub(IUserRepository userRepository, ISessionUsersRepository sessionUsersRepository)
        {
            _userRepository = userRepository;
            _sessionUsersRepository = sessionUsersRepository;
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
                var saveResult = await _sessionUsersRepository.AddUserScore(currentUserAccountToken, currentSessionToken, scoreInfo.Score);

                if (saveResult)
                {
                    var sessionUser = await _sessionUsersRepository.GetSessionUserByToken(currentSessionToken.ToString());
                    await Clients.Group(currentSessionToken.ToString()).SendAsync("ScoreboardUpdated", sessionUser);
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