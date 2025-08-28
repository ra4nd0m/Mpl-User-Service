using Microsoft.EntityFrameworkCore;
using MplDataReceiver.Data;
using MplDataReceiver.Models;
using MplDataReceiver.Models.DTOs;

namespace MplDataReceiver.Services;

public class DataInsertService(BMplbaseContext context)
{
    public async Task InsertValuesRange(List<MaterialUpdate> updates)
    {
        var materialIds = updates.Select(u => u.MaterialId).Distinct().ToList();
        var validMaterialIds = await context.MaterialSources
            .Where(ms => materialIds.Contains(ms.Uid))
            .Select(ms => ms.Uid)
            .ToHashSetAsync();

        var propertyIds = updates
            .SelectMany(u => u.DateValues)
            .SelectMany(dv => dv.PropertyValues)
            .Select(pv => pv.PropertyId)
            .Distinct()
            .ToList();

        var validPropertyIds = await context.Properties
                .Where(p => propertyIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToHashSetAsync();

        var materialValues = new List<MaterialValue>();

        foreach (var update in updates)
        {
            if (!validMaterialIds.Contains(update.MaterialId))
                continue;

            foreach (var dateValue in update.DateValues)
            {
                if (!DateOnly.TryParse(dateValue.Date, out var parsedDate))
                    continue;

                foreach (var propertyValue in dateValue.PropertyValues)
                {
                    if (!validPropertyIds.Contains(propertyValue.PropertyId))
                        continue;
                        
                    var materialValue = new MaterialValue(update.MaterialId, propertyValue.PropertyId, parsedDate);

                    if (decimal.TryParse(propertyValue.Value, out var parsedDecimal))
                        materialValue.ValueDecimal = parsedDecimal;
                    else
                        materialValue.ValueStr = propertyValue.Value;

                    materialValue.LastUpdated = DateTime.UtcNow;
                    materialValues.Add(materialValue);
                }
            }

        }

        if (materialValues.Count != 0)
        {
            await context.MaterialValues.AddRangeAsync(materialValues);
            await context.SaveChangesAsync();
        }
    }
}