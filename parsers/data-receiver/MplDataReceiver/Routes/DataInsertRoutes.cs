using MplDataReceiver.Models.DTOs;
using MplDataReceiver.Services;

namespace MplDataReceiver.Routes;

public static class DataInsertRoutes
{
    public static void MapDataInsertRoutes(this WebApplication app)
    {
        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("MaterialValueRoutes");

        app.MapPost("/insertBatch", async (Payload<List<MaterialUpdate>> payload, DataInsertService dataInsertService) =>
        {
            try
            {
                var updates = payload.Data;
                await dataInsertService.InsertValuesRange(updates);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error inserting data");
                return Results.Problem("Error inserting data");
            }

        });

        app.MapPost("/addNewMaterial", async (NewMaterialRequest materialRequest, DataInsertService dataInsertService) =>
        {
            try
            {
                int resultId = await dataInsertService.AddNewMaterial(materialRequest);
                return Results.Ok(resultId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding new material");
                return Results.Problem("Error adding new material");
            }
        });

        app.MapPost("/addMaterialDescription", async (AddMaterialDescriptionReq descriptionReq, DataInsertService dataInsertService) =>
        {
            try
            {
                await dataInsertService.AddMaterialDescription(descriptionReq);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding material description");
                return Results.Problem("Error adding material description");
            }
        });

        app.MapPost("/addMaterialRounding", async (AddRoundingToMaterialReq roundingReq, DataInsertService dataInsertService) =>
        {
            try
            {
                await dataInsertService.AddRoundingToMaterial(roundingReq);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding material rounding");
                return Results.Problem("Error adding material rounding");
            }
        });
    }
}