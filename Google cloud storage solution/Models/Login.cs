using System.ComponentModel.DataAnnotations;

namespace Google_cloud_storage_solution.Models
{
    public class Login
    {
        [Required(ErrorMessage = "Username is required.")]
        [Display(Name = "Username")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        public Login()
        {

        }

        public Login(string username, string passWord, bool rememberMe)
        {
            Username = username;
            Password = passWord;
            RememberMe = rememberMe;
        }
    }
}
