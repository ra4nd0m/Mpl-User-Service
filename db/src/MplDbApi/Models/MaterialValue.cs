namespace MplDbApi.Models;

public partial class MaterialValue
{
    public int Id { get; set; }

    public int Uid { get; set; }

    public int PropertyId { get; set; }

    public decimal? ValueDecimal { get; set; }

    public string? ValueStr { get; set; }

    public DateOnly CreatedOn { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual Property Property { get; set; } = null!;

    public virtual MaterialSource UidNavigation { get; set; } = null!;
    public MaterialValue() { }
    public MaterialValue(int matId, int propId, DateOnly createdOn)
    {
        Uid = matId;
        PropertyId = propId;
        CreatedOn = createdOn;
    }
}