namespace MplDbApi.Models.Dtos
{
    public record FilterCreateReqDto(
        string AffectedRole,
        List<int>? Groups,
        List<int>? Sources,
        List<int>? Units,
        List<int>? MaterialIds,
        List<int>? Properties
    );
}