namespace MplUserService.Models
{
    public class User
    {
        public string Id { get; set; } = null!;
        public List<int> FavouriteIds { get; set; } = [];
        public string? SettingsJson { get; set; }
    }
}