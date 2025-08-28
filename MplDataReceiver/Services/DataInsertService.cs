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

        var materialValuesToProcess = new List<(int Uid, int PropertyId, DateOnly CreatedOn, string Value)>();

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
                        
                    materialValuesToProcess.Add((update.MaterialId, propertyValue.PropertyId, parsedDate, propertyValue.Value));
                }
            }
        }

        if (materialValuesToProcess.Count == 0)
            return;

        // Get existing MaterialValues that match our criteria
        var existingValues = await context.MaterialValues
            .Where(mv => materialValuesToProcess.Any(mvp => 
                mvp.Uid == mv.Uid && 
                mvp.PropertyId == mv.PropertyId && 
                mvp.CreatedOn == mv.CreatedOn))
            .ToListAsync();

        var newMaterialValues = new List<MaterialValue>();

        foreach (var (uid, propertyId, createdOn, value) in materialValuesToProcess)
        {
            // Check if this combination already exists
            var existingValue = existingValues.FirstOrDefault(ev => 
                ev.Uid == uid && 
                ev.PropertyId == propertyId && 
                ev.CreatedOn == createdOn);

            if (existingValue != null)
            {
                // Update existing value
                if (decimal.TryParse(value, out var parsedDecimal))
                {
                    existingValue.ValueDecimal = parsedDecimal;
                    existingValue.ValueStr = null; // Clear string value when setting decimal
                }
                else
                {
                    existingValue.ValueStr = value;
                    existingValue.ValueDecimal = null; // Clear decimal value when setting string
                }
                existingValue.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                // Create new MaterialValue
                var materialValue = new MaterialValue(uid, propertyId, createdOn);

                if (decimal.TryParse(value, out var parsedDecimal))
                    materialValue.ValueDecimal = parsedDecimal;
                else
                    materialValue.ValueStr = value;

                materialValue.LastUpdated = DateTime.UtcNow;
                newMaterialValues.Add(materialValue);
            }
        }

        // Add new values to context
        if (newMaterialValues.Count > 0)
        {
            await context.MaterialValues.AddRangeAsync(newMaterialValues);
        }

        await context.SaveChangesAsync();
    }
}