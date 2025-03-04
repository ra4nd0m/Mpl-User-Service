namespace MplUserService.Models
{
    public class User
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public List<string> FavouriteIds { get; set; } = [];
    }
}