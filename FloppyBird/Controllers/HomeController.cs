using FloppyBird.Data;
using FloppyBird.DomainModels;
using FloppyBird.Dtos;
using FloppyBird.Hubs;
using FloppyBird.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Reflection;

namespace FloppyBird.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUserRepository _userRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IHubContext<GameSessionHub> _gameSessionhubContext;
        private const string sessionTokenCookieKey = "currentSessionToken";
        private const string userTokenCookieKey = "currentUserToken";
        private const string highscoreCookieKey = "highscore";
        private readonly CookieOptions cookieOption;

        public HomeController(ILogger<HomeController> logger,
                            IOptions<RedisConfigOptions> redisConfigOptions,
                            IUserRepository userRepository,
                            ISessionRepository sessionRepository,
                            IHubContext<GameSessionHub> gameSessionhubContext)
        {
            _logger = logger;
            var redisConfig = redisConfigOptions.Value;
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _gameSessionhubContext = gameSessionhubContext;
            cookieOption = new CookieOptions
            {
                Expires = DateTime.Now.AddHours(redisConfig.expirationInHr)
            };
        }

        public async Task<IActionResult> Index()
        {
            HomeIndexModel model = new HomeIndexModel
            {
                BaseUrl = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}"
            };

            if (IsCurrentUserAccountTokenExistsInCookies())
            {
                var currentUserAccountToken = GetCurrentUserAccountTokenInCookies();
                model.User = await _userRepository.GetUserByAccountToken(currentUserAccountToken);
            }

            if (IsSessionTokenExistsInCookies())
            {
                var currentSessionToken = GetSessionTokenInCookies();
                var currentSession = await _sessionRepository.GetSessionbyToken(currentSessionToken);
                if (currentSession != null)
                {
                    model.CurrentSession = currentSession;
                    model.CurrentUserIsTheGameMaster = currentSession?.GameMasterAccountToken == model.User?.AccountToken;
                    model.SessionScoreCard = new DomainModels.SessionScorecard(currentSession.Users, currentSession.ScoreCountingType);
                }
            }

            return View(model);
        }

        public async Task<IActionResult> ScoreCard()
        {
            HomeIndexModel model = new HomeIndexModel
            {
                BaseUrl = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}"
            };

            if (IsCurrentUserAccountTokenExistsInCookies())
            {
                var currentUserAccountToken = GetCurrentUserAccountTokenInCookies();
                model.User = await _userRepository.GetUserByAccountToken(currentUserAccountToken);
            }

            if (IsSessionTokenExistsInCookies())
            {
                var currentSessionToken = GetSessionTokenInCookies();
                var currentSession = await _sessionRepository.GetSessionbyToken(currentSessionToken);
                if (currentSession != null)
                {
                    model.CurrentSession = currentSession;
                    model.CurrentUserIsTheGameMaster = currentSession?.GameMasterAccountToken == model.User?.AccountToken;
                    model.SessionScoreCard = new DomainModels.SessionScorecard(currentSession.Users, currentSession.ScoreCountingType);
                }
            }
            else
            {
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult SignOut()
        {
            DeleteCurrentUserAccountTokenInCookies();
            DeleteSessionTokenInCookies();
            DeleteHighestScore();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> StartTheGameSession()
        {
            var currentSessionToken = GetSessionTokenInCookies();
            if (Guid.TryParse(currentSessionToken, out var sessionToken))
            {
                var isStartedSuccessfully = await _sessionRepository.StartTheSession(sessionToken);
                if (isStartedSuccessfully)
                {
                    await SendScoreboardUpdates(sessionToken);
                    await _gameSessionhubContext.Clients.Group(sessionToken.ToString()).SendAsync("GameSessionHasBeenStarted", "Started");
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> EndTheGameSession()
        {
            var currentSessionToken = GetSessionTokenInCookies();
            if (Guid.TryParse(currentSessionToken, out var sessionToken))
            {
                var isEnded = await _sessionRepository.EndTheSession(sessionToken);
                if (isEnded)
                {
                    DeleteSessionTokenInCookies();
                    DeleteHighestScore();
                    await _gameSessionhubContext.Clients.Group(sessionToken.ToString()).SendAsync("GameSessionHasBeenEnded", "Session has ended, please exit on this session.");
                }
            }

            return RedirectToAction("Index");
        }

        [HttpGet()]
        public async Task<IActionResult> JoinTheSession([FromQuery]string sessionToken)
        {
            if (!Guid.TryParse(sessionToken, out var sessionTokenGuid))
            {
                ModelState.AddModelError("sessionToken", "Invalid Session token");
                return BadRequest(ModelState);
            }

            SetSessionTokenInCookies(sessionToken);
            if (!IsCurrentUserAccountTokenExistsInCookies())
            {
                return RedirectToAction("Index");
            }

            var currentUserAccountToken = GetCurrentUserAccountTokenInCookies();
            var userObj = await _userRepository.GetUserByAccountToken(currentUserAccountToken);

            await _sessionRepository.AddUserToSession(userObj, sessionTokenGuid);

            await SendScoreboardUpdates(sessionTokenGuid);
            await _gameSessionhubContext.Clients.Group(sessionToken).SendAsync("UserHasJoinedTheSession", $"{userObj.Name} has joined the session");

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> LeaveTheSession()
        {
            var currentUserAccountToken = GetCurrentUserAccountTokenInCookies();
            var currentSessionToken = GetSessionTokenInCookies();

            if (Guid.TryParse(currentUserAccountToken, out var userAccountToken) && Guid.TryParse(currentSessionToken, out var sessionTokenGuid))
            {
                var userObj = await _userRepository.GetUserByAccountToken(userAccountToken.ToString());
                await _sessionRepository.RemoveUserFromSession(userAccountToken, sessionTokenGuid);
                
                DeleteSessionTokenInCookies();
                DeleteHighestScore();

                await SendScoreboardUpdates(sessionTokenGuid);
                await _gameSessionhubContext.Groups.RemoveFromGroupAsync(userObj.HubConnectionId, currentSessionToken.ToString());
                await _gameSessionhubContext.Clients.Group(sessionTokenGuid.ToString()).SendAsync("UserHasLeftTheSession", $"{userObj.Name} has left the session");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewAccount([FromForm] CreateNewAccountParams createNewAccountParams)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userObj = await _userRepository.CreateNewUserAccount(createNewAccountParams.Username);
            if (userObj == null)
            {
                ModelState.AddModelError("Username", "Unable to create your account, please try again.");
                return BadRequest(ModelState);
            }
            SetCurrentUserAccountTokenInCookies(userObj.AccountToken.ToString());

            if (IsSessionTokenExistsInCookies())
            {
                var sessionToken = GetSessionTokenInCookies();
                var sessionTokenGuid = Guid.Parse(sessionToken);
                await _sessionRepository.AddUserToSession(userObj, sessionTokenGuid);
                await SendScoreboardUpdates(sessionTokenGuid);
                await _gameSessionhubContext.Clients.Group(sessionToken).SendAsync("UserHasJoinedTheSession", $"{userObj.Name} has joined the session");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewSession([FromForm] CreateNewSessionParams createNewSessionParams)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUserAccountToken = GetCurrentUserAccountTokenInCookies();

            var sessionObj = await _sessionRepository.CreateNewSession(createNewSessionParams, currentUserAccountToken);

            SetSessionTokenInCookies(sessionObj.SessionToken.ToString());

            return RedirectToAction("Index");
        }

        private async Task SendScoreboardUpdates(Guid sessionToken)
        {
            var session = await _sessionRepository.GetSessionbyToken(sessionToken.ToString());
            var scoreBoard = new SessionScorecard(session?.Users, session.ScoreCountingType);
            await _gameSessionhubContext.Clients.Group(sessionToken.ToString()).SendAsync("ScoreboardUpdated", scoreBoard);
        }

        // SessionToken
        private bool IsSessionTokenExistsInCookies() => Request.Cookies.ContainsKey(sessionTokenCookieKey);
        private void SetSessionTokenInCookies(string SessionToken) => Response.Cookies.Append(sessionTokenCookieKey, SessionToken, cookieOption);
        private string GetSessionTokenInCookies () => Request.Cookies[sessionTokenCookieKey].ToString();
        private void DeleteSessionTokenInCookies () => Response.Cookies.Delete(sessionTokenCookieKey);

        // CurrentUserAccount
        private bool IsCurrentUserAccountTokenExistsInCookies() => Request.Cookies.ContainsKey(userTokenCookieKey);
        private string GetCurrentUserAccountTokenInCookies() => Request.Cookies[userTokenCookieKey].ToString();
        private void SetCurrentUserAccountTokenInCookies(string accountToken) => Response.Cookies.Append(userTokenCookieKey, accountToken, cookieOption);
        private void DeleteCurrentUserAccountTokenInCookies() => Response.Cookies.Delete(userTokenCookieKey);

        // HighestScore
        private void DeleteHighestScore() => Response.Cookies.Delete(highscoreCookieKey);

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}