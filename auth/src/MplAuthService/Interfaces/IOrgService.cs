using MplAuthService.Models;
using MplAuthService.Models.Dtos;

namespace MplAuthService.Interfaces
{
    public interface IOrgService
    {
        Task<Organization?> GetOrganization(int id);
        Task<List<Organization>> GetOrganizations();
        Task<Organization> CreateOrganization(OrganizationDto orgDto);
    }
}