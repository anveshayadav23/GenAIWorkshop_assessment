using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace restful_api.Services
{
    public interface ILoginService
    {
        AuthResult Login(string username, string password);
        void Logout(string token);
        bool ValidateToken(string token);
    }

    public class LoginService : ILoginService
    {
        // In-memory user store for demo purposes
        private static readonly Dictionary<string, User> Users = new()
        {
            { "admin", new User { Username = "admin", PasswordHash = HashPassword("admin123"), Role = "Admin" } },
            { "user", new User { Username = "user", PasswordHash = HashPassword("user123"), Role = "User" } }
        };

        // In-memory token blacklist for logout
        private static readonly HashSet<string> BlacklistedTokens = new();

        private readonly string _jwtSecret = "YourSuperSecretKeyForJwtTokenGeneration!"; // Use config in production
        private readonly int _jwtLifespanMinutes = 60;

        public AuthResult Login(string username, string password)
        {
            if (!Users.TryGetValue(username, out var user))
                throw new AuthException("Invalid username or password.");

            if (!VerifyPassword(password, user.PasswordHash))
                throw new AuthException("Invalid username or password.");

            var token = GenerateJwtToken(user);
            return new AuthResult
            {
                Token = token,
                Username = user.Username,
                Role = user.Role,
                Message = "Login successful."
            };
        }

        public void Logout(string token)
        {
            if (!string.IsNullOrEmpty(token))
                BlacklistedTokens.Add(token);
        }

        public bool ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token) || BlacklistedTokens.Contains(token))
                return false;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                }, out _);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtLifespanMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }

    public class User
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
    }

    public class AuthResult
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string Message { get; set; }
    }

    public class AuthException : Exception
    {
        public AuthException(string message) : base(message) { }
    }
}