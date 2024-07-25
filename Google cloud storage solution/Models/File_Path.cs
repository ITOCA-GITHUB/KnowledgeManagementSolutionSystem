using System.ComponentModel.DataAnnotations;

namespace Google_cloud_storage_solution.Models
{
    public class File_Path
    {
        public string? Name { get; set; }
        public string? Id { get; set; }
        public string? Link { get; set; }
        public File_Path()
        {
            
        }
    }
}
