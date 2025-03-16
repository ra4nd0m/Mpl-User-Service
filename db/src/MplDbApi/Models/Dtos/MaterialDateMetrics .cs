namespace MplDbApi.Models.Dtos
{
    public record DateGroupedMaterialValues(
        DateOnly Date,
        List<MaterialDateMetrics> MaterialValues
    );
}