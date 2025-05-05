namespace MplDbApi.Utils
{
    public static class ValueChangeFormatter
    {
        public static string Format(decimal latest, decimal previous)
        {
            var diff = latest - previous;

            if (previous == 0 || diff == 0)
                return "\u2014";

            var percent = (diff / previous) * 100;

            var percentStr = $"{percent:0.##;-0.##;0}".Replace('.', ',') + "%";
            var diffStr = $"{diff:0.##;-0.##;0}".Replace('.', ',');

            return $"{percentStr} ({diffStr})";
        }
    }
}
