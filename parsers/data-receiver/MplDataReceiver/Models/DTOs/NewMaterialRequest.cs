namespace MplDataReceiver.Models.DTOs
{
    public record NewMaterialRequest(string MaterialName, string MaterialGroupName,
        string MaterialSource, string DeliveryTypeName, string UnitName, string TargetMarket);

}