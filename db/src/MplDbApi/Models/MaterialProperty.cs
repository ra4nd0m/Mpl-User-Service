namespace MplDbApi.Models;

public partial class MaterialProperty
{
    public int Id { get; set; }

    public int Uid { get; set; }

    public int PropertyId { get; set; }

    public virtual Property Property { get; set; } = null!;

    public virtual MaterialSource UidNavigation { get; set; } = null!;
}