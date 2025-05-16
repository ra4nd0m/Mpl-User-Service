namespace MplDbApi.Models.Dtos
{
    public record MaterialDateMetrics(
        int Id,
        DateOnly Date,
        List<int> PropsUsed,
        string? ValueAvg,
        string? ValueMin,
        string? ValueMax,
        string? PredWeekly,
        string? PredMonthly,
        string? Supply,
        string? WeeklyAvg = null,
        string? MonthlyAvg = null,
        string? QuarterlyAvg = null,
        string? YearlyAvg = null,
        CompactMaterialInfo? MaterialInfo = null
    );

    public record DateGroupedMaterialValues(
        DateOnly Date,
        List<MaterialDateMetrics> MaterialValues
    );
}