namespace Google_cloud_storage_solution.Models
{
    public class UserActivity
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? PageName { get; set; }
        public TimeSpan EntryTime { get; set; }
        public TimeSpan ExitTime { get; set; }
        public DateTime? ActivityDate { get; set; }

        public UserActivity()
        {

        }
    }
}
