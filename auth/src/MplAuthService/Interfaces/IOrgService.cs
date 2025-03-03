using MplAuthService.Models;

namespace MplAuthService.Interfaces
{
    public interface IOrgService
    {
        Task<Organization?> GetOrganization(int id);
        Task<List<Organization>> GetOrganizations();
    }
}