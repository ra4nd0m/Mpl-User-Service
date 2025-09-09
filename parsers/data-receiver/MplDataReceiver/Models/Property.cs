namespace MplDataReceiver.Models;
public partial class Property
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Kind { get; set; } = null!;

    public virtual ICollection<MaterialProperty> MaterialProperties { get; set; } = new List<MaterialProperty>();

    public virtual ICollection<MaterialValue> MaterialValues { get; set; } = new List<MaterialValue>();
}
