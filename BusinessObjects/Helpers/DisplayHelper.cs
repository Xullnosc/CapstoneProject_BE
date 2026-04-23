namespace BusinessObjects.Helpers
{
    public static class DisplayHelper
    {
        public static string FormatTeamCode(string? dbTeamCode)
        {
            if (string.IsNullOrEmpty(dbTeamCode)) return string.Empty;
            
            // Expected database format: [SEMESTER]_SE_XX (e.g., SP26_SE_01)
            // Desired display: SE_01, SE_02
            // logic: strip everything before "_SE_" and include "SE_" prefix
            
            int index = dbTeamCode.IndexOf("_SE_");
            if (index != -1)
            {
                // Result after index+1 would be "SE_01"
                return dbTeamCode.Substring(index + 1);
            }
            
            return dbTeamCode;
        }
    }
}
