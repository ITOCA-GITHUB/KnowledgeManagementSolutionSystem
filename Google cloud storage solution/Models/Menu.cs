namespace Google_cloud_storage_solution.Models
{
    public class Menu
    {
        public int MenuId { get; set; }
        public string? Title { get; set; }
        public ICollection<MenuItem>? MenuItems { get; set; }

        public Menu()
        {

        }
    }
}
