namespace MplDbApi.Models.Dtos
{
    public record MaterialDateMetrics (
        int Id,
        DateOnly Date,
        List<int> PropsUsed,
        string? ValueAvg,
        string? ValueMin,
        string? ValueMax,
        string? PredWeekly,
        string? PredMonthly,
        string? Supply,
        string? MonthlyAvg,
        CompactMaterialInfo? MaterialInfo = null
    );
}