namespace MplDbApi.Models.Filters
{
    public class DataFilter
    {
        public int Id { get; set; }
        public string AffectedRole { get; set; } = string.Empty;
        public List<int>? Groups { get; set; }
        public List<int>? Sources { get; set; }
        public List<int>? Units { get; set; }
        public List<int>? MaterialIds { get; set; }
        public List<int>? Properties { get; set; }
    }
}