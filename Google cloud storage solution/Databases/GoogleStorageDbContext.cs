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

        public DbSet<File_Index> file_index { get; set; }
        public DbSet<File_Path> file_paths { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string? connectionString = _configuration.GetConnectionString("GoogleStorageDatabaseConnection");
            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<File_Index>().HasKey(u => u.Id);
            modelBuilder.Entity<File_Path>().HasKey(c => c.Name);       
        }
    }
}
