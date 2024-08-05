namespace Google_cloud_storage_solution.Models
{
    public class AclEntry
    {
        public int Id { get; set; }
        public string? FilePath { get; set; }
        public string? UserEmail { get; set; }
        public string? Role { get; set; }

        public AclEntry()
        {
            
        }
    }
}
