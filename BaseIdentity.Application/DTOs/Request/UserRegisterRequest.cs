using BaseIdentity.Domain.Common.Enums;

namespace BaseIdentity.Application.DTOs.Request
{
    public class UserRegisterRequest
    {
        public string? FirstName { get;  set; }
        public string? LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public GenderEnums? Gender { get; set; }
    }
}
