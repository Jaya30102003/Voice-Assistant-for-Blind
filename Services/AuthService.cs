using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using VoiceAssistantForBlind.Data;
using VoiceAssistantForBlind.Models;
using Microsoft.EntityFrameworkCore;
using VoiceAssistantForBlind.Models.ViewModels;

namespace VoiceAssistantForBlind.Services
{
    public class AuthService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        
        public AuthService(IConfiguration configuration, AppDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }
        
        // Admin JWT token generation
        public string GenerateJwtToken(AdminUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "YourSuperSecretKeyForJWTTokenGeneration2024!MakeItLongAndSecure");
            var issuer = _configuration["Jwt:Issuer"] ?? "VoiceAssistantForBlind";
            var audience = _configuration["Jwt:Audience"] ?? "VoiceAssistantForBlindUsers";
            
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
                new Claim("user_type", "admin")
            };
            
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
        
        // User JWT token generation
        public string GenerateUserJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "YourSuperSecretKeyForJWTTokenGeneration2024!MakeItLongAndSecure");
            var issuer = _configuration["Jwt:Issuer"] ?? "VoiceAssistantForBlind";
            var audience = _configuration["Jwt:Audience"] ?? "VoiceAssistantForBlindUsers";
            
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username ?? ""),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("user_type", "user"),
                new Claim("user_profile_id", user.UserProfileId?.ToString() ?? "")
            };
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(30),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        
        // Password hashing methods
        public (string hash, string salt) HashPassword(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var saltBytes = new byte[32];
            rng.GetBytes(saltBytes);
            var salt = Convert.ToBase64String(saltBytes);
            
            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
            var hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
            
            return (hash, salt);
        }
        
        public bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
                return false;
                
            var saltBytes = Convert.FromBase64String(storedSalt);
            
            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
            var hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
            
            return hash == storedHash;
        }
        
        // Admin authentication
        public async Task<AdminUser?> AuthenticateAdmin(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;
                
            var user = await _context.AdminUsers
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            
            if (user == null) return null;
            
            if (VerifyPassword(password, user.PasswordHash, user.Salt))
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return user;
            }
            
            return null;
        }
        
        // User authentication
        public async Task<User?> AuthenticateUser(string usernameOrEmail, string password)
        {
            if (string.IsNullOrEmpty(usernameOrEmail) || string.IsNullOrEmpty(password))
                return null;
                
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => 
                    (u.Username == usernameOrEmail || u.Email == usernameOrEmail) 
                    && u.IsActive);
            
            if (user == null) return null;
            
            if (VerifyPassword(password, user.PasswordHash, user.Salt))
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return user;
            }
            
            return null;
        }
        
        // Check if username exists
        public async Task<bool> UsernameExists(string username)
        {
            if (string.IsNullOrEmpty(username)) return false;
            return await _context.Users.AnyAsync(u => u.Username == username);
        }
        
        // Check if email exists
        public async Task<bool> EmailExists(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
        
        // Register new user
        public async Task<User> RegisterUser(RegisterViewModel model)
        {
            var (hash, salt) = HashPassword(model.Password ?? "");
            
            var user = new User
            {
                Username = model.Username ?? "",
                Email = model.Email ?? "",
                FullName = model.FullName,
                PasswordHash = hash,
                Salt = salt,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            return user;
        }
        
        // Link user to profile
        public async Task LinkUserToProfile(int userId, int profileId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.UserProfileId = profileId;
                await _context.SaveChangesAsync();
            }
        }

        // Get user by ID
        public async Task<User?> GetUserById(int id)
        {
            return await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}