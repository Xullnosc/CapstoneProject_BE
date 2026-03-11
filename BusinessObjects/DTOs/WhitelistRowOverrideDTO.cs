namespace BusinessObjects.DTOs
{
    /// <summary>
    /// Carries field-level corrections that the HOD supplies for a specific
    /// row in the whitelist import file, e.g. to fix an email or student code
    /// that caused a role-conflict during the preview phase.
    /// </summary>
    public class WhitelistRowOverrideDTO
    {
        /// <summary>1-based row number matching the original Excel row.</summary>
        public int RowNumber { get; set; }

        /// <summary>Replacement email address (optional).</summary>
        public string? Email { get; set; }

        /// <summary>Replacement full name (optional).</summary>
        public string? FullName { get; set; }

        /// <summary>Replacement student code (optional).</summary>
        public string? StudentCode { get; set; }
    }
}
