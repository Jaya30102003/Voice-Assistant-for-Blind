using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VoiceAssistantForBlind.Models;
using VoiceAssistantForBlind.Services;
using VoiceAssistantForBlind.Data;
using Microsoft.EntityFrameworkCore;

namespace VoiceAssistantForBlind.Controllers
{
    public class AdminController : Controller
    {
        private readonly AuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminController> _logger;
        
        public AdminController(AuthService authService, IConfiguration configuration, ILogger<AdminController> logger)
        {
            _authService = authService;
            _configuration = configuration;
            _logger = logger;
        }
        
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            Console.WriteLine("========== ADMIN LOGIN ATTEMPT ==========");
            
            if (!ModelState.IsValid)
                return View(model);
            
            try
            {
                // FIXED: Changed from Authenticate to AuthenticateAdmin
                var user = await _authService.AuthenticateAdmin(model.Username, model.Password);
                
                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid username or password");
                    return View(model);
                }
                
                // ========== CREATE JWT TOKEN ==========
                var token = GenerateJwtToken(user);
                
                // Store token in cookie or session for API access
                Response.Cookies.Append("jwt_token", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                });
                
                // Create claims for cookie authentication (for MVC views)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username ?? ""),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim(ClaimTypes.Role, user.Role ?? "Admin"),
                    new Claim("user_type", "admin"),
                    new Claim("jwt_token", token)
                };
                
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };
                
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);
                
                _logger.LogInformation($"Admin {user.Username} logged in. JWT Token generated.");
                
                ViewBag.JwtToken = token;
                
                return RedirectToAction("Index", "JobAdmin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                ModelState.AddModelError("", "An error occurred during login");
                return View(model);
            }
        }
        
        private string GenerateJwtToken(AdminUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            // Get JWT settings from configuration
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "YourSuperSecretKeyForJWTTokenGeneration2024!MakeItLongAndSecure");
            var issuer = _configuration["Jwt:Issuer"] ?? "VoiceAssistantForBlind";
            var audience = _configuration["Jwt:Audience"] ?? "VoiceAssistantForBlindUsers";
            
            // Create claims for the token
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username ?? ""),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "Admin"),
                new Claim("user_type", "admin"),
                new Claim("full_name", user.FullName ?? ""),
                new Claim("is_active", user.IsActive.ToString())
            };
            
            // Create token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Remove JWT cookie
            Response.Cookies.Delete("jwt_token");
            
            // Sign out from cookie auth
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            return RedirectToAction("Login");
        }
        
        [HttpGet]
        [Authorize]
        public IActionResult AccessDenied()
        {
            return View();
        }
        
        // API endpoint to get a fresh token (useful for mobile apps)
        [HttpPost]
        [Authorize]
        public IActionResult RefreshToken()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.FindFirstValue(ClaimTypes.Name);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var role = User.FindFirstValue(ClaimTypes.Role);
            
            // Create a minimal user object
            var user = new AdminUser
            {
                Id = int.Parse(userId ?? "0"),
                Username = username,
                Email = email,
                Role = role
            };
            
            var newToken = GenerateJwtToken(user);
            
            return Ok(new { token = newToken });
        }
        
        // Seed initial admin user (call this once)
        [HttpGet]
        public async Task<IActionResult> SeedAdmin()
        {
            var context = HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            
            if (!context.AdminUsers.Any())
            {
                var authService = HttpContext.RequestServices.GetRequiredService<AuthService>();
                var (hash, salt) = authService.HashPassword("Admin@123");
                
                var admin = new AdminUser
                {
                    Username = "admin",
                    Email = "admin@voiceassistant.com",
                    FullName = "System Administrator",
                    PasswordHash = hash,
                    Salt = salt,
                    Role = "SuperAdmin",
                    CreatedAt = DateTime.UtcNow
                };
                
                context.AdminUsers.Add(admin);
                await context.SaveChangesAsync();
                
                return Content("✅ Admin user created! Username: admin, Password: Admin@123");
            }
            
            return Content("✅ Admin user already exists");
        }
    }
}