using BaseIdentity.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace BaseIdentity.Domain.Entities
{
    public class ApplicationUser : IdentityUser<Guid>, IEntity<Guid>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Gender { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdateAt { get; set; } = DateTime.UtcNow;
    }
}
