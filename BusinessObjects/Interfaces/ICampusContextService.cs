namespace BusinessObjects.Interfaces;

public interface ICampusContextService
{
    /// <summary>
    /// Returns the CampusId of the currently logged-in user.
    /// Returns null if the user is a Super Admin (can view all campuses).
    /// </summary>
    int? GetCurrentCampusId();
}
