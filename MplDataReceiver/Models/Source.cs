namespace MplDataReceiver.Models;
public partial class Source
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Url { get; set; }

    public string? Kind { get; set; }

    public virtual ICollection<MaterialSource> MaterialSources { get; set; } = new List<MaterialSource>();
}