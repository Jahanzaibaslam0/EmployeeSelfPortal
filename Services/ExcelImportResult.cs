namespace HRMS.Services
{
    public class ExcelImportResult
    {
        public bool Success { get; set; }
        public int Processed { get; set; }
        public string Message { get; set; } = "";
    }
}
