using System.ComponentModel.DataAnnotations;

namespace Google_cloud_storage_solution.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        public string? Email { get; set; }

        public ForgotPasswordViewModel()
        {

        }
    }
}
