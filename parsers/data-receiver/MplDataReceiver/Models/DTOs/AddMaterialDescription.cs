namespace MplDataReceiver.Models.DTOs;

public record AddMaterialDescriptionReq(
    int MaterialId,
    string Description
);