namespace MplDbApi.Models.Dtos
{
    public record MaterialDateMetricReq(
        int MaterialId,
        List<int> PropertyIds,
        DateOnly StartDate,
        DateOnly EndDate
    );
}