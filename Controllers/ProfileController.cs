using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceAssistantForBlind.Models;
using VoiceAssistantForBlind.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace VoiceAssistantForBlind.Controllers
{
    [Authorize(Policy = "UserOnly")] // Require user authentication
    public class ProfileController : Controller
    {
        private readonly ProfileService _profileService;
        private readonly AuthService _authService;
        private readonly ILogger<ProfileController> _logger;
        
        public ProfileController(
            ProfileService profileService, 
            AuthService authService,
            ILogger<ProfileController> logger)
        {
            _profileService = profileService;
            _authService = authService;
            _logger = logger;
        }
        
        // GET: Profile/Edit
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            try
            {
                Console.WriteLine("========== GET Edit ==========");
                
                // Get the logged-in user's profile ID from claims
                var profileIdClaim = User.FindFirst("profile_id")?.Value;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                Console.WriteLine($"User ID from claims: {userIdClaim}");
                Console.WriteLine($"Profile ID from claims: {profileIdClaim}");
                
                UserProfileViewModel profile;
                
                if (!string.IsNullOrEmpty(profileIdClaim) && int.TryParse(profileIdClaim, out int profileId))
                {
                    Console.WriteLine($"Loading profile with ID: {profileId}");
                    // Load the user's existing profile
                    profile = await _profileService.GetProfileByIdAsync(profileId);
                    if (profile == null)
                    {
                        Console.WriteLine("Profile not found, creating new one");
                        profile = new UserProfileViewModel();
                    }
                    else
                    {
                        Console.WriteLine($"Profile loaded: {profile.FullName}");
                    }
                }
                else
                {
                    Console.WriteLine("No profile ID in claims, creating empty profile");
                    // New user, create empty profile
                    profile = new UserProfileViewModel();
                }
                
                return View(profile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GET Edit: {ex.Message}");
                return View(new UserProfileViewModel());
            }
        }
        
        // POST: Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserProfileViewModel model)
        {
            Console.WriteLine("========== POST Edit HIT ==========");
            Console.WriteLine($"Received model - ID: {model?.Id}, Name: '{model?.FullName ?? "NULL"}', Email: '{model?.Email ?? "NULL"}'");
            
            try
            {
                // Get user info from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var profileIdClaim = User.FindFirst("profile_id")?.Value;
                
                Console.WriteLine($"User ID from claims: {userIdClaim}");
                Console.WriteLine($"Profile ID from claims: {profileIdClaim}");
                
                // If model ID is 0, use the profile ID from claims
                if (model.Id == 0 && !string.IsNullOrEmpty(profileIdClaim) && int.TryParse(profileIdClaim, out int claimProfileId))
                {
                    model.Id = claimProfileId;
                    Console.WriteLine($"Set model ID to claim profile ID: {model.Id}");
                }
                
                // Manually read form data for debugging
                var form = await Request.ReadFormAsync();
                var fullName = form["FullName"].ToString();
                var email = form["Email"].ToString();
                var id = form["Id"].ToString();
                
                Console.WriteLine($"Form data - Name: '{fullName}', Email: '{email}', Id: '{id}'");
                
                // Update model with form data if empty
                if (string.IsNullOrEmpty(model.FullName) && !string.IsNullOrEmpty(fullName))
                    model.FullName = fullName;
                    
                if (string.IsNullOrEmpty(model.Email) && !string.IsNullOrEmpty(email))
                    model.Email = email;
                    
                if (model.Id == 0 && !string.IsNullOrEmpty(id) && int.TryParse(id, out var parsedId))
                    model.Id = parsedId;
                
                // Manual validation
                if (string.IsNullOrWhiteSpace(model.FullName))
                {
                    ModelState.AddModelError("FullName", "Full name is required");
                    Console.WriteLine("Validation failed: FullName empty");
                }
                
                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    ModelState.AddModelError("Email", "Email is required");
                    Console.WriteLine("Validation failed: Email empty");
                }
                
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("Returning with validation errors");
                    return View(model);
                }
                
                // Try to save
                Console.WriteLine($"Attempting to save profile for: {model.FullName} with ID: {model.Id}");
                var savedProfile = await _profileService.SaveProfileAsync(model);
                Console.WriteLine($"✅ Save successful! Profile ID: {savedProfile.Id}");
                
                // If this is a new profile, link it to the user
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    // Check if this profile needs to be linked
                    var user = await _authService.GetUserById(userId);
                    if (user != null && user.UserProfileId != savedProfile.Id)
                    {
                        Console.WriteLine($"Linking user {userId} to profile {savedProfile.Id}");
                        await _authService.LinkUserToProfile(userId, savedProfile.Id);
                        
                        // Update the claims with new profile ID
                        await UpdateUserClaims(userId, savedProfile.Id);
                    }
                }
                
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in POST Edit: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return View(model ?? new UserProfileViewModel());
            }
        }
        
        private async Task UpdateUserClaims(int userId, int profileId)
        {
            var user = await _authService.GetUserById(userId);
            if (user != null)
            {
                Console.WriteLine($"Updating claims for user {userId} with profile {profileId}");
                
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username ?? ""),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim("user_type", "user"),
                    new Claim("profile_id", profileId.ToString())
                };
                
                if (!string.IsNullOrEmpty(user.FullName))
                {
                    claims.Add(new Claim(ClaimTypes.GivenName, user.FullName));
                }
                
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                };
                
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);
                
                Console.WriteLine("Claims updated successfully");
            }
        }
        
        // POST: Test endpoint
        [HttpPost]
        public async Task<IActionResult> TestPost()
        {
            Console.WriteLine("========== TEST POST HIT ==========");
            
            try
            {
                var form = await Request.ReadFormAsync();
                var name = form["FullName"].ToString();
                var email = form["Email"].ToString();
                
                Console.WriteLine($"TestPost received - Name: '{name}', Email: '{email}'");
                
                return Content($"✅ TEST POST SUCCESSFUL!<br>Name: {name}<br>Email: {email}", "text/html");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ TestPost error: {ex.Message}");
                return Content($"Error: {ex.Message}");
            }
        }
        
        // GET: Test endpoint
        [HttpGet]
        public IActionResult Test()
        {
            Console.WriteLine("========== TEST GET HIT ==========");
            return Content("✅ Profile controller GET is working!");
        }
        
        // GET: Test form page
        [HttpGet]
        public IActionResult TestForm()
        {
            Console.WriteLine("========== TEST FORM GET ==========");
            return View();
        }
        
        // GET: Debug database
        [HttpGet]
        public async Task<IActionResult> Debug()
        {
            Console.WriteLine("========== DEBUG ENDPOINT ==========");
            
            try
            {
                var profile = await _profileService.GetLatestProfileAsync();
                
                string result = "=== DATABASE DEBUG ===\n";
                result += $"Profile exists: {(profile != null ? "YES" : "NO")}\n";
                
                if (profile != null)
                {
                    result += $"Profile ID: {profile.Id}\n";
                    result += $"Name: {profile.FullName}\n";
                    result += $"Email: {profile.Email}\n";
                }
                else
                {
                    result += "No profile in database yet.\n";
                }
                
                return Content(result);
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}");
            }
        }
    }
}