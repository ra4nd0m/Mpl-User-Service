namespace MplAuthService.Models
{
    public class Organisation
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Inn { get; set; }
        public ICollection<User> Users { get; set; } = [];
    }
}