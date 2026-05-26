namespace FirebaseWorkout.Model
{
    public class WorkPlaceReport
    {
        public string WorkPlaceId { get; set; } = string.Empty;
        public string WorkPlaceName { get; set; } = string.Empty;
        public string ReportedByUserId { get; set; } = string.Empty;
        public string ReporterName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string ReportDate { get; set; } = string.Empty;
    }

    public class ReportedJobSummary
    {
        public string WorkPlaceId { get; set; } = string.Empty;
        public string WorkPlaceName { get; set; } = string.Empty;
        public string CreatedByUserName { get; set; } = string.Empty;
        public int ReportCount { get; set; }
        public List<WorkPlaceReport> Reports { get; set; } = new();
        public string ReportCountText => $"{ReportCount} report{(ReportCount == 1 ? "" : "s")}";
    }
}
