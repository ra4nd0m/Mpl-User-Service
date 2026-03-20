namespace MplUserService.Config
{
    public sealed class CurrencyApiOptions
    {
        public const string SectionName = "CurrencyApi";
        public string BaseUrl { get; set; } = "";
        public int CacheHours { get; set; } = 6;
    }
}