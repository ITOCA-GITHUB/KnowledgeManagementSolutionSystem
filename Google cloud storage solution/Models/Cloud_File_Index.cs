using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Google_cloud_storage_solution.Models
{
    public class Cloud_File_Index
    {
        [Column("id")]
        public string Id { get; set; }

        [Column("name")]
        [MaxLength(500)]
        public string? Name { get; set; }

        [Column("date_created")]
        public string DateCreated { get; set; }

        [Column("folder")]
        public string? Folder { get; set; }

        [Column("creator_email")]
        [MaxLength(50)]
        public string? CreatorEmail { get; set; }

        public Cloud_File_Index()
        {
            
        }
    }
}
