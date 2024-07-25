using System.ComponentModel.DataAnnotations;

namespace Google_cloud_storage_solution.Models
{
    public class File_Index
    {
        public string? Id { get; set; }
        public string? name { get; set; }
        public string? creator_email { get; set; }
        public DateTime? date_created { get; set; }
        public string? file_type { get; set; }

        public File_Index()
        {
            
        }
    }
}
