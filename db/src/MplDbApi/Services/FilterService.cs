using Microsoft.EntityFrameworkCore;
using MplDbApi.Data;
using MplDbApi.Models.Dtos;
using MplDbApi.Models.Filters;

namespace MplDbApi.Services
{
    public class FilterService(FilterContext context, ILogger<FilterService> logger)
    {
        public async Task ModifyFilter(FilterCreateReqDto input)
        {
            try
            {
                if (string.IsNullOrEmpty(input.AffectedRole))
                {
                    throw new ArgumentException("AffectedRole cannot be null or empty", nameof(input));
                }

                var existingFilter = await context.Filters
                    .FirstOrDefaultAsync(f => f.AffectedRole == input.AffectedRole);

                if (existingFilter != null)
                {
                    // Update existing filter
                    existingFilter.Groups = input.Groups;
                    existingFilter.Sources = input.Sources;
                    existingFilter.Units = input.Units;
                    existingFilter.MaterialIds = input.MaterialIds;
                    existingFilter.Properties = input.Properties;
                    context.Filters.Update(existingFilter);
                }
                else
                {
                    var filter = new DataFilter
                    {
                        AffectedRole = input.AffectedRole,
                        Groups = input.Groups,
                        Sources = input.Sources,
                        Units = input.Units,
                        MaterialIds = input.MaterialIds,
                        Properties = input.Properties
                    };
                    context.Filters.Add(filter);
                }
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding filter");
                throw new InvalidOperationException("Failed to add filter");
            }
        }
        public async Task<DataFilter> GetFilterByRole(string? role)
        {
            //If role is not present, treat is an admin request and don't apply any filters
            if (string.IsNullOrEmpty(role))
            {
                return new DataFilter
                {
                    AffectedRole = "Default",
                    Groups = null,
                    Sources = null,
                    Units = null,
                    MaterialIds = null,
                    Properties = null
                };
            }

            var filter = await context.Filters
                .FirstOrDefaultAsync(f => f.AffectedRole == role) ?? throw new KeyNotFoundException($"No filter found for role: {role}");
            return filter;
        }

        public async Task<List<DataFilter>> GetAllFilters()
        {
            try
            {
                return await context.Filters.ToListAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving all filters");
                throw new InvalidOperationException("Failed to retrieve filters");
            }
        }

    }
}