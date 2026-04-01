namespace HealthTracker.ViewModels
{
    public class WeeklySummaryViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public double AvgSleep { get; set; }
        public double AvgWater { get; set; }
        public int AvgSteps { get; set; }
        public string MostCommonMood { get; set; } = string.Empty;
        public string SleepRecommendation { get; set; } = string.Empty;
        public string WaterRecommendation { get; set; } = string.Empty;
        public string StepsRecommendation { get; set; } = string.Empty;
        public string MoodRecommendation { get; set; } = string.Empty;
        public int TotalEntries { get; set; }
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
    }
}