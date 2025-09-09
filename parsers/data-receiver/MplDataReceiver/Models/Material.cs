namespace MplDataReceiver.Models;
public partial class Material
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<MaterialSource> MaterialSources { get; set; } = new List<MaterialSource>();
}