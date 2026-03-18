using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VoiceAssistantForBlind.Models;
using VoiceAssistantForBlind.Models.ViewModels;
using VoiceAssistantForBlind.Services;

namespace VoiceAssistantForBlind.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;
        private readonly ProfileService _profileService;
        private readonly ILogger<AccountController> _logger;
        
        public AccountController(
            AuthService authService,
            ProfileService profileService,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _profileService = profileService;
            _logger = logger;
        }
        
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            try
            {
                // Check if username exists
                if (await _authService.UsernameExists(model.Username ?? ""))
                {
                    ModelState.AddModelError("Username", "Username already exists");
                    return View(model);
                }
                
                // Check if email exists
                if (await _authService.EmailExists(model.Email ?? ""))
                {
                    ModelState.AddModelError("Email", "Email already registered");
                    return View(model);
                }
                
                // Register user
                var user = await _authService.RegisterUser(model);
                
                _logger.LogInformation($"New user registered: {user.Username}");
                
                // Automatically create a profile for the user
                var profile = new UserProfileViewModel
                {
                    FullName = model.FullName ?? model.Username,
                    Email = model.Email
                };
                
                var savedProfile = await _profileService.SaveProfileAsync(profile);
                
                // Link profile to user
                await _authService.LinkUserToProfile(user.Id, savedProfile.Id);
                
                // Redirect to login page with success message
                TempData["SuccessMessage"] = "Registration successful! Please login with your credentials.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                return View(model);
            }
        }
        
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UserLoginViewModel model)
        {
            Console.WriteLine("========== LOGIN ATTEMPT ==========");
            Console.WriteLine($"Username/Email: '{model.UsernameOrEmail}'");
            
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState invalid");
                return View(model);
            }
            
            try
            {
                var user = await _authService.AuthenticateUser(model.UsernameOrEmail ?? "", model.Password ?? "");
                
                if (user == null)
                {
                    Console.WriteLine("Authentication failed - user not found or wrong password");
                    ModelState.AddModelError("", "Invalid username/email or password");
                    return View(model);
                }
                
                Console.WriteLine($"✅ User authenticated: {user.Username}, ID: {user.Id}, Profile ID: {user.UserProfileId}");
                
                await SignInUser(user, model.RememberMe);
                
                _logger.LogInformation($"User logged in: {user.Username}");
                
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Login error: {ex.Message}");
                _logger.LogError(ex, "Error during login");
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View(model);
            }
        }
        
        private async Task SignInUser(User user, bool rememberMe = false)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("user_type", "user"),
                new Claim("profile_id", user.UserProfileId?.ToString() ?? "")
            };
            
            if (!string.IsNullOrEmpty(user.FullName))
            {
                claims.Add(new Claim(ClaimTypes.GivenName, user.FullName));
            }
            
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(rememberMe ? 30 : 1)
            };
            
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
                
            Console.WriteLine($"✅ User signed in with {claims.Count} claims");
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
        
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToAction("Login");
            }
            
            var user = await _authService.GetUserById(int.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }
            
            return View(user);
        }
    }
}