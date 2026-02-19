using System.Collections.Generic;
using Health_Tracker.Models;

namespace Health_Tracker.Models.ViewModels
{
    public class DashboardViewModel
    {
        public List<WellnessEntry> Entries { get; set; } = new();

        public int TotalEntries { get; set; }
        public double AvgSleep { get; set; }
        public double AvgSteps { get; set; }
        public double AvgWater { get; set; }
        public double AvgBMI { get; set; }

        public int WellnessScore { get; set; }

        public Dictionary<string, int> MoodDistribution { get; set; } = new();
    }
}
