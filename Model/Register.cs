using Microsoft.AspNetCore.Identity;

namespace SecurityApi.Model
{
    public class Register
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
    }

    public class Login
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class RequestToken
    {
        public string RefreshToken { get; set; }

    }

    public class ApplicationUser : IdentityUser
    {
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }

    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsUsed { get; set; }
        public bool IsRevoked { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Revoked { get; set; }
    }

}
