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

        public DbSet<Cloud_File_Index> cloud_File_Index { get; set; }
        public DbSet<File_Index> file_index { get; set; }
        public DbSet<File_Path> file_paths { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<UserSessions> UserSessions { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }
        public DbSet<Menu> Menu { get; set; }
        public DbSet<MenuItem> MenuItem { get; set; }
        public DbSet<RolePermissions> RolePermissions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string? connectionString = _configuration.GetConnectionString("GoogleStorageDatabaseConnection");
            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cloud_File_Index>().ToTable("cloud_file_index");
            modelBuilder.Entity<File_Index>().HasKey(f => f.Id);
            modelBuilder.Entity<File_Path>().HasKey(c => c.Name);
            modelBuilder.Entity<Users>().HasKey(u => u.UserId);
            modelBuilder.Entity<UserSessions>().HasOne(us => us.User).WithMany().HasForeignKey(us => us.UserId).IsRequired();
            modelBuilder.Entity<UserActivity>().HasKey(z => z.Id);
            modelBuilder.Entity<Menu>().HasKey(m => m.MenuId);
            modelBuilder.Entity<MenuItem>().HasKey(mi => mi.MenuItemId);
            modelBuilder.Entity<MenuItem>().HasOne(mi => mi.Menu).WithMany(m => m.MenuItems).HasForeignKey(mi => mi.MenuId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Users>()
                 .HasMany(u => u.RolePermissions)
                 .WithOne(rp => rp.User)
                 .HasForeignKey(rp => rp.UserId);
        }

        public async Task<string?> GetEmailByUsernameAsync(string username)
        {
            return await Task.FromResult("admin@itoca.org");
        }
    }
}
