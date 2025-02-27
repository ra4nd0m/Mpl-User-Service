namespace MplDbApi.Models;
public partial class MaterialSource
{
    public int Id { get; set; }

    public int Uid { get; set; }

    public int MaterialId { get; set; }

    public int SourceId { get; set; }

    public string TargetMarket { get; set; } = null!;

    public int UnitId { get; set; }

    public int DeliveryTypeId { get; set; }

    public int MaterialGroupId { get; set; }

    public virtual DeliveryType DeliveryType { get; set; } = null!;

    public virtual Material Material { get; set; } = null!;

    public virtual MaterialGroup MaterialGroup { get; set; } = null!;

    public virtual ICollection<MaterialProperty> MaterialProperties { get; set; } = new List<MaterialProperty>();

    public virtual ICollection<MaterialValue> MaterialValues { get; set; } = new List<MaterialValue>();

    public virtual Source Source { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}