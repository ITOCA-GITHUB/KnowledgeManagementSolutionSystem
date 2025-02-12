﻿namespace Google_cloud_storage_solution.Models
{
    public class UserSessions
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public int? DurationHours { get; set; }
        public int? DurationMinutes { get; set; }
        public bool IsIdle { get; set; }

        // Navigation property
        public Users? User { get; set; }
    }
}
