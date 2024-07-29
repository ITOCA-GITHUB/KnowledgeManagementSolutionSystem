using System.ComponentModel.DataAnnotations.Schema;

namespace Google_cloud_storage_solution.Models
{
    public class Users
    {
        public int UserId { get; set; }
        public string? UserName { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? PasswordHash { get; set; } = string.Empty;

        public string? Fullname { get; set; } = string.Empty;

        public int PhoneNumber { get; set; }
        public string? PhysicalAddress { get; set; } = string.Empty;

        public string? Role { get; set; } = string.Empty;

        [NotMapped]
        public TimeSpan? LastLoginTime { get; set; }
        [NotMapped]
        public TimeSpan? LastLogoutTime { get; set; }

        public Users()
        {

        }

        public Users(string _username, string _email, string _passwordHash, string _fullname, int _phoneNumber, string _physicalAddress, string _role)
        {
            UserName = _username;
            Email = _email;
            PasswordHash = _passwordHash;
            Fullname = _fullname;
            PhoneNumber = _phoneNumber;
            PhysicalAddress = _physicalAddress;
            Role = _role;
        }
    }
}
