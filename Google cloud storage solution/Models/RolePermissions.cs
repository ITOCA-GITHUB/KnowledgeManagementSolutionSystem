namespace Google_cloud_storage_solution.Models
{
    public class RolePermissions
    {
        public int Id { get; set; }
        public bool CanView { get; set; }
        public bool CanUpload { get; set; }
        public bool CanEdit { get; set; }
        public int UserId { get; set; }
        public Users? User { get; set; }
        public string? Folder { get; set; }

        public RolePermissions()
        {
            
        }
    }
}
