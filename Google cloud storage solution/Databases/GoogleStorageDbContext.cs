using Google_cloud_storage_solution.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Google_cloud_storage_solution.Databases
{
    public class GoogleStorageDbContext : DbContext
    {
        private readonly IConfiguration _configuration;
        public GoogleStorageDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public DbSet<log_in_info> LogInInfos { get; set; }
        public DbSet<File_Index> file_index { get; set; }
        public DbSet<File_Path> file_paths { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<UserSessions> UserSessions { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }



        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string? connectionString = _configuration.GetConnectionString("GoogleStorageDatabaseConnection");
            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<File_Index>().HasKey(f => f.Id);
            modelBuilder.Entity<File_Path>().HasKey(c => c.Name);
            modelBuilder.Entity<log_in_info>().HasKey(u => u.email);
            modelBuilder.Entity<Users>().HasKey(u => u.UserId);
            modelBuilder.Entity<UserSessions>().HasKey(us => us.Id);
            modelBuilder.Entity<UserActivity>().HasKey(z => z.Id);
        }

         public async Task<string?> GetEmailByObjectNameAsync(string objectName)
        {
            objectName = "tafadzwa@itoca.org";
            var file = await LogInInfos.FirstOrDefaultAsync(f => f.email == objectName);
            return file?.email;
        }
    }
}
