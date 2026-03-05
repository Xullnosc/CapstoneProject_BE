using System.Collections.Generic;

namespace Services.Helpers
{
    public class ImportResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public List<ImportError> Errors { get; set; } = new List<ImportError>();
    }
}
