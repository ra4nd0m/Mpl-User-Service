namespace MplDataReceiver.Models.DTOs;

public record AddRoundingToMaterialReq(
    int MaterialId,
    int RoundTo
);