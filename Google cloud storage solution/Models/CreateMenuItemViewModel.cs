namespace Google_cloud_storage_solution.Models
{
    public class CreateMenuItemViewModel
    {
        public int MenuId { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
        public string? ActionItems { get; set; }
        public string? Assigned { get; set; }
        public DateTime? Deadline { get; set; }
        public string? Status { get; set; }
    }
}
