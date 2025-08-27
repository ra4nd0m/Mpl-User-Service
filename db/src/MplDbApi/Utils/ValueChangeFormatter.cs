namespace MplDbApi.Utils
{
    public static class ValueChangeFormatter
    {
        public static string FormatValueChange(decimal oldValue, decimal newValue)
        {
            var diff = newValue - oldValue;

            if (oldValue == 0 || diff == 0)
                return "\u2014";

            var percent = diff / oldValue * 100;

            var percentStr = $"{percent:0.##;-0.##;0}".Replace('.', ',') + "%";
            var diffStr = $"{diff:0.##;-0.##;0}".Replace('.', ',');

            return $"{percentStr} ({diffStr})";
        }
    }
}