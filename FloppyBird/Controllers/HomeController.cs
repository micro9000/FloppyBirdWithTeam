using FloppyBird.Data;
using FloppyBird.Dtos;
using FloppyBird.Hubs;
using FloppyBird.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace FloppyBird.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUserRepository _userRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly ISessionUsersRepository _sessionUsersRepository;
        private readonly IHubContext<GameSessionHub> _gameSessionhubContext;
        private const string sessionTokenCookieKey = "currentSessionToken";
        private const string userTokenCookieKey = "currentUserToken";
        private readonly CookieOptions cookieOption;

        public HomeController(ILogger<HomeController> logger,
                            IOptions<RedisConfigOptions> redisConfigOptions,
                            IUserRepository userRepository,
                            ISessionRepository sessionRepository,
                            ISessionUsersRepository sessionUsersRepository,
                            IHubContext<GameSessionHub> gameSessionhubContext)
        {
            _logger = logger;
            var redisConfig = redisConfigOptions.Value;
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _sessionUsersRepository = sessionUsersRepository;
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
                model.CurrentSession = await _sessionRepository.GetSessionbyToken(currentSessionToken);
                model.CurrentUserIsTheGameMaster = model.CurrentSession?.GameMasterAccountToken == model.User?.AccountToken;
                model.SessionUser = await _sessionUsersRepository.GetSessionUserByToken(currentSessionToken);
            }

            return View(model);
        }

        public IActionResult Chat()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> SaveUserScore (int score)
        {
            if (score == 0) return new JsonResult(new { isSuccessful = false });

            var currentUserAccountToken = GetCurrentUserAccountTokenInCookies();
            var currentSessionToken = GetSessionTokenInCookies();

            if (Guid.TryParse(currentUserAccountToken, out var userAccountToken) && Guid.TryParse(currentSessionToken, out var sessionToken))
            {
                var saveResult = await _sessionUsersRepository.AddUserScore(userAccountToken, sessionToken, score);
                return new JsonResult(new { isSuccessful = saveResult });
            }

            return new JsonResult(new { isSuccessful = false });
        }

        [HttpPost]
        public async Task<IActionResult> JoinTheSession([FromForm] JoinTheSessionParams joinTheSessionParams)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!Guid.TryParse(joinTheSessionParams.SessionToken, out var sessionTokenGuid))
            {
                ModelState.AddModelError("sessionToken", "Invalid Session token");
                return BadRequest(ModelState);
            }
            string sessionToken = sessionTokenGuid.ToString();

            var currentUserAccountToken = GetCurrentUserAccountTokenInCookies();
            var userObj = await _userRepository.GetUserByAccountToken(currentUserAccountToken);

            SetSessionTokenInCookies(sessionToken);
            await _sessionUsersRepository.AddUserToSession(userObj, sessionTokenGuid);

            await _gameSessionhubContext.Clients.Group(sessionToken).SendAsync("UserHasJoinedTheSession", $"{userObj.Name} has joined the session");

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> LeaveTheSession()
        {
            var currentUserAccountToken = GetCurrentUserAccountTokenInCookies();
            var currentSessionToken = GetSessionTokenInCookies();

            if (Guid.TryParse(currentUserAccountToken, out var userAccountToken) && Guid.TryParse(currentSessionToken, out var sessionToken))
            {
                await _sessionUsersRepository.RemoveUserFromSession(userAccountToken, sessionToken);
                DeleteCurrentUserAccountTokenInCookies();
                DeleteSessionTokenInCookies();
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

        private bool IsSessionTokenExistsInCookies() => Request.Cookies.ContainsKey(sessionTokenCookieKey);
        private void SetSessionTokenInCookies(string SessionToken) => Response.Cookies.Append(sessionTokenCookieKey, SessionToken, cookieOption);
        private string GetSessionTokenInCookies () => Request.Cookies[sessionTokenCookieKey].ToString();
        private void DeleteSessionTokenInCookies () => Response.Cookies.Delete(sessionTokenCookieKey);

        private bool IsCurrentUserAccountTokenExistsInCookies() => Request.Cookies.ContainsKey(userTokenCookieKey);
        private string GetCurrentUserAccountTokenInCookies() => Request.Cookies[userTokenCookieKey].ToString();
        private void SetCurrentUserAccountTokenInCookies(string accountToken) => Response.Cookies.Append(userTokenCookieKey, accountToken, cookieOption);
        private void DeleteCurrentUserAccountTokenInCookies() => Response.Cookies.Delete(userTokenCookieKey);

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}