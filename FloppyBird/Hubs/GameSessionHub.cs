using FloppyBird.Data;
using FloppyBird.DomainModels;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
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
                    if (session.IsStarted && !session.IsEnded)
                    {
                        var scoreBoard = new SessionScorecard(session?.Users, session.ScoreCountingType);
                        await Clients.Group(currentSessionToken.ToString()).SendAsync("ScoreboardUpdated", scoreBoard);
                    }
                }
            }
        }

        public ChannelReader<string> StartTimer (string sessionToken, CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<string>();
            if (Guid.TryParse(sessionToken, out var currentSessionToken))
            {
                _ = WriteItemsAsync(channel.Writer, currentSessionToken.ToString(), cancellationToken);
            }
            return channel.Reader;
        }

        private async Task WriteItemsAsync (ChannelWriter<string> writer, string sessionToken, CancellationToken cancellationToken)
        {
            Exception localException = null;
            try
            {
                var session = await _sessionRepository.GetSessionbyToken(sessionToken);
                if (session != null && session.IsStarted)
                {
                    DateTime startDate = session.StartedAt;
                    DateTime endDate = startDate.AddMinutes(session.NumberOfMinutes);

                    TimeSpan timeRange = endDate - startDate;
                    while (DateTime.Now < endDate)
                    {
                        TimeSpan timeSpan = DateTime.Now - startDate;
                        TimeSpan remaining = timeRange.Subtract(timeSpan);
                        var result = string.Format("{0:D2}:{1:D2}", (int)remaining.TotalMinutes, remaining.Seconds);
                        await writer.WriteAsync(result, cancellationToken);
                        await Task.Delay(1000, cancellationToken);

                        var updatedSession = await _sessionRepository.GetSessionbyToken(sessionToken);

                        if ((int)remaining.TotalMinutes == 0 && remaining.Seconds == 0)
                        {
                            await _sessionRepository.EndTheSession(Guid.Parse(sessionToken));
						}

                        if (updatedSession.IsEnded)
                        {
                            var scoreBoard = new SessionScorecard(updatedSession?.Users, updatedSession.ScoreCountingType);
                            await Clients.Group(sessionToken).SendAsync("ScoreboardUpdated", scoreBoard);
                            await Clients.Group(sessionToken).SendAsync("GameSessionHasBeenEnded", "Finished");
                            return;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                localException = ex;
            }
            finally
            {
                writer.Complete(localException);
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