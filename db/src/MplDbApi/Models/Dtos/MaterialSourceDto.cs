namespace MplDbApi.Models;

public class MaterialSourceDto
{
    public int Id { get; set; }
    public string MaterialName { get; set; } = null!;
    public string Source { get; set; } = null!;
    public string DeliveryType { get; set; } = null!;
    public string Group { get; set; } = null!;
    public string Market { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public DateOnly? LastCreatedDate { get; set; }
}