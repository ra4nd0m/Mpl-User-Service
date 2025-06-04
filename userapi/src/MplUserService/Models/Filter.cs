namespace MplUserService.Models
{
    public class Filter
    {
        public int Id { get; set; }
        public List<int>? Groups { get; set; }
        public List<int>? Sources { get; set; }
        public List<int>? Units { get; set; }
        public List<int>? MaterialIds { get; set; }
        public List<int>? Properties { get; set; }
    }
}