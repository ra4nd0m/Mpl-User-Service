namespace MplDbApi.Models;

public partial class DeliveryType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<MaterialSource> MaterialSources { get; set; } = new List<MaterialSource>();
}