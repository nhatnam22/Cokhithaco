using System;
using System.Collections.Generic;

namespace CoKhiThaCo.Models.Dtos.Auth
{
    public class LoginRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int GlobalRole { get; set; }
        public ICollection<string> Permissions { get; set; }
        public ICollection<string> MachineIds { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
