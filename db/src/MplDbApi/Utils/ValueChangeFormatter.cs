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

            static string FormatSigned(decimal value, string suffix = "")
            {
                var absStr = Math.Abs(value).ToString("0.##").Replace('.', ',');
                var sign = value > 0 ? "+" : "-";
                return $"{sign}{absStr}{suffix}";
            }

            var percentStr = FormatSigned(percent, "%");
            var diffStr = FormatSigned(diff);

            return $"{percentStr} ({diffStr})";
        }
    }
}