namespace MplDbApi.Models.Dtos
{
    public record RoleEnhancedReqDto<T>(
        T Data,
        string Role
    );
}