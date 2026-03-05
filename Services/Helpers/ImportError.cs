namespace Services.Helpers
{
    public class ImportError
    {
        public int Row { get; set; }
        public string Column { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
