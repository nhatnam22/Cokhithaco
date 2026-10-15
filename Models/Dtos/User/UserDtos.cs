using System;
using System.Collections.Generic;

namespace CoKhiThaCo.Models.Dtos.User
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int GlobalRole { get; set; }
        public string GlobalRoleName { get; set; }
        public Guid? PositionId { get; set; }
        public string PositionName { get; set; }
        public bool IsActive { get; set; }
        public bool MustChangePass { get; set; }
        public DateTime? CreatedDate { get; set; }
        public List<string> PermissionCodes { get; set; }
    }

    public class CreateUserRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int GlobalRole { get; set; } = 2;
        public Guid? PositionId { get; set; }
    }

    public class UpdateUserRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int GlobalRole { get; set; }
        public Guid? PositionId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UserListQuery
    {
        public string Keyword { get; set; }
        public string Sorts { get; set; }
        public string Filters { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
