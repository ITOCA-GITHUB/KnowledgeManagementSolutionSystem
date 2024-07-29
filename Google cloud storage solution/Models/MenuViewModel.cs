namespace Google_cloud_storage_solution.Models
{
    public class MenuViewModel
    {
        public List<Menu>? Menus { get; set; }
        public List<MenuItem>? MenuItems { get; set; }
        public Menu? NewMenu { get; set; } // Add a property for the new menu
        public MenuItem? NewMenuItem { get; set; }
    }
}
