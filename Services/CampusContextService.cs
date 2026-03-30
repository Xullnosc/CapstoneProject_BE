using Microsoft.AspNetCore.Http;
using BusinessObjects.Interfaces;

namespace Services;

public class CampusContextService : ICampusContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CampusContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? GetCurrentCampusId()
    {
        var claim = _httpContextAccessor.HttpContext?
            .User.FindFirst("campus_id");
        
        if (claim == null || !int.TryParse(claim.Value, out var campusId))
            return null; // Super Admin -> no filter
        
        return campusId;
    }
}
