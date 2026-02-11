namespace Farm_Management.Models.ViewModels
{
    public class AttendanceCalendarViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int DaysInMonth { get; set; }
        public List<Worker> Workers { get; set; } = new();
        public Dictionary<(int WorkerId, int Day), AttendanceLog> Logs { get; set; } = new();

        // Helper method để lấy giờ làm của 1 worker trong 1 ngày
        public decimal GetHours(int workerId, int day)
        {
            return Logs.TryGetValue((workerId, day), out var log) ? log.HoursWorked : 0;
        }

        // Helper để lấy WorkType
        public string GetWorkType(int workerId, int day)
        {
            return Logs.TryGetValue((workerId, day), out var log) ? log.WorkType : "";
        }

        // Tính tổng công của 1 worker trong tháng
        public (decimal TotalDays, decimal OddHours) GetWorkerSummary(int workerId)
        {
            var workerLogs = Logs.Where(l => l.Key.WorkerId == workerId).Select(l => l.Value);

            var fullDays = workerLogs.Count(l => l.WorkType == "FullDay");
            var halfDays = workerLogs.Count(l => l.WorkType == "HalfDay");
            var oddHours = workerLogs.Where(l => l.WorkType == "OddHours").Sum(l => l.HoursWorked);

            var totalDays = fullDays + (halfDays * 0.5m);

            return (totalDays, oddHours);
        }
    }
}