using Microsoft.EntityFrameworkCore;
using MplDataReceiver.Data;
using MplDataReceiver.Models;
using MplDataReceiver.Models.DTOs;

namespace MplDataReceiver.Services;

public class DataInsertService(BMplbaseContext context, IHttpClientFactory httpClientFactory, ILogger<DataInsertService> logger)
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

        // Map external UIDs -> internal IDs (PK) to satisfy FK material_value_material_source_fk
        var uidToId = await context.MaterialSources
            .Where(ms => validMaterialIds.Contains(ms.Uid))
            .Select(ms => new { ms.Uid, ms.Id })
            .ToDictionaryAsync(x => x.Uid, x => x.Id);

        var materialValuesToProcess = new List<(int Uid, int PropertyId, DateOnly CreatedOn, string Value)>();

        foreach (var update in updates)
        {
            if (!validMaterialIds.Contains(update.MaterialId))
                continue;

            foreach (var dateValue in update.DateValues)
            {
                if (!DateOnly.TryParseExact(dateValue.Date, "dd.MM.yyyy", out var parsedDate))
                    continue;

                foreach (var propertyValue in dateValue.PropertyValues)
                {
                    if (!validPropertyIds.Contains(propertyValue.PropertyId))
                    {
                        continue;
                    }

                    var normalized = propertyValue.Value?.Trim();
                    if (string.IsNullOrEmpty(normalized))
                    {
                        logger.LogWarning("Skipped empty value for MaterialUid={Uid}, PropertyId={PropertyId}, Date={Date}", update.MaterialId, propertyValue.PropertyId, parsedDate);
                        continue;
                    }
                    materialValuesToProcess.Add((update.MaterialId, propertyValue.PropertyId, parsedDate, normalized));
                }
            }
        }

        if (materialValuesToProcess.Count == 0)
            return;

        // Resolve to internal IDs before DB interactions
        var resolvedItems = new List<(int Id, int PropertyId, DateOnly CreatedOn, string Value)>();
        foreach (var item in materialValuesToProcess)
        {
            if (uidToId.TryGetValue(item.Uid, out var id))
            {
                resolvedItems.Add((id, item.PropertyId, item.CreatedOn, item.Value));
            }
            else
            {
                // Skip unknown UID; optional: log warning
                // logger.LogWarning("Material UID {Uid} not found in MaterialSources map", item.Uid);
            }
        }

        if (resolvedItems.Count == 0)
            return;

        // Extract unique combinations for querying (using internal IDs)
        var ids = resolvedItems.Select(i => i.Id).Distinct().ToList();
        var processPropertyIds = resolvedItems.Select(i => i.PropertyId).Distinct().ToList();
        var dates = resolvedItems.Select(i => i.CreatedOn).Distinct().ToList();

        // Get existing MaterialValues with broader criteria, then filter in memory
        var existingValues = await context.MaterialValues
            .Where(mv => ids.Contains(mv.Uid) &&
                         processPropertyIds.Contains(mv.PropertyId) &&
                         dates.Contains(mv.CreatedOn))
            .ToListAsync();

        // Filter to exact matches in memory (using internal ID)
        var materialValuesToProcessSet = resolvedItems
            .Select(i => new { Uid = i.Id, i.PropertyId, i.CreatedOn })
            .ToHashSet();

        existingValues = existingValues
            .Where(ev => materialValuesToProcessSet.Contains(new { Uid = ev.Uid, ev.PropertyId, ev.CreatedOn }))
            .ToList();

        var newMaterialValues = new List<MaterialValue>();

        foreach (var (id, propertyId, createdOn, value) in resolvedItems)
        {
            var existingValue = existingValues.FirstOrDefault(ev =>
                ev.Uid == id &&
                ev.PropertyId == propertyId &&
                ev.CreatedOn == createdOn);

            if (existingValue != null)
            {
                if (decimal.TryParse(value, out var parsedDecimal))
                {
                    existingValue.ValueDecimal = parsedDecimal;
                    existingValue.ValueStr = null;
                }
                else
                {
                    existingValue.ValueStr = value;
                    existingValue.ValueDecimal = null;
                }
            }
            else
            {
                var materialValue = new MaterialValue(id, propertyId, createdOn);

                if (decimal.TryParse(value, out var parsedDecimal))
                    materialValue.ValueDecimal = parsedDecimal;
                else
                    materialValue.ValueStr = value;

                newMaterialValues.Add(materialValue);
            }
        }

        if (newMaterialValues.Count > 0)
        {
            await context.MaterialValues.AddRangeAsync(newMaterialValues);
        }

        await context.SaveChangesAsync();

        try
        {
            var httpClient = httpClientFactory.CreateClient("DbApi");
            var response = await httpClient.PostAsync("cache/clear", null);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to notify DB API to clear cache");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while notifying DB API to clear cache");
        }
    }

    public async Task AddNewMaterial(NewMaterialRequest newMaterial)
    {
        // Find or create Material
        var material = await context.Materials
            .FirstOrDefaultAsync(m => m.Name == newMaterial.MaterialName);

        if (material == null)
        {
            material = new Material { Name = newMaterial.MaterialName };
            context.Materials.Add(material);
            await context.SaveChangesAsync();
        }

        // Find or create MaterialGroup
        var materialGroup = await context.MaterialGroups
            .FirstOrDefaultAsync(mg => mg.Name == newMaterial.MaterialGroupName);

        if (materialGroup == null)
        {
            materialGroup = new MaterialGroup { Name = newMaterial.MaterialGroupName };
            context.MaterialGroups.Add(materialGroup);
            await context.SaveChangesAsync();
        }

        // Find or create Source
        var source = await context.Sources
            .FirstOrDefaultAsync(s => s.Name == newMaterial.MaterialSource);

        if (source == null)
        {
            source = new Source { Name = newMaterial.MaterialSource, Url = newMaterial.MaterialSource };
            context.Sources.Add(source);
            await context.SaveChangesAsync();
        }

        // Find or create DeliveryType
        var deliveryType = await context.DeliveryTypes
            .FirstOrDefaultAsync(dt => dt.Name == newMaterial.DeliveryTypeName);

        if (deliveryType == null)
        {
            deliveryType = new DeliveryType { Name = newMaterial.DeliveryTypeName };
            context.DeliveryTypes.Add(deliveryType);
            await context.SaveChangesAsync();
        }

        // Find or create Unit
        var unit = await context.Units
            .FirstOrDefaultAsync(u => u.Name == newMaterial.UnitName);

        if (unit == null)
        {
            unit = new Unit { Name = newMaterial.UnitName };
            context.Units.Add(unit);
            await context.SaveChangesAsync();
        }

        // Check if MaterialSource already exists
        var existingMaterialSource = await context.MaterialSources
            .FirstOrDefaultAsync(ms =>
                ms.MaterialId == material.Id &&
                ms.SourceId == source.Id &&
                ms.TargetMarket == newMaterial.TargetMarket &&
                ms.UnitId == unit.Id &&
                ms.DeliveryTypeId == deliveryType.Id &&
                ms.MaterialGroupId == materialGroup.Id);

        if (existingMaterialSource != null)
        {
            logger.LogWarning("MaterialSource already exists with Uid={Uid}", existingMaterialSource.Uid);
            throw new InvalidOperationException("MaterialSource with provided parameters already exists.");
        }

        // Create new MaterialSource
        var materialSource = new MaterialSource
        {
            MaterialId = material.Id,
            SourceId = source.Id,
            TargetMarket = newMaterial.TargetMarket,
            UnitId = unit.Id,
            DeliveryTypeId = deliveryType.Id,
            MaterialGroupId = materialGroup.Id
        };

        context.MaterialSources.Add(materialSource);
        await context.SaveChangesAsync();

        logger.LogInformation("Created new MaterialSource with Uid={Uid} for Material={MaterialName}",
            materialSource.Uid, newMaterial.MaterialName);
    }
}