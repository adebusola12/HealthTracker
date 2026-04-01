using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace HealthTracker.Models
{
    public class WellnessEntry
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Column(TypeName = "timestamp without time zone")]
        [Display(Name = "Entry Date")]
        public DateTime Date { get; set; }

        [Range(0, 20)]
        [Display(Name = "Water Intake (Liters)")]
        public int WaterIntakeInLiters { get; set; }

        [Range(0, 100000)]
        [Display(Name = "Steps")]
        public int Steps { get; set; }

        [Range(0, 24)]
        [Display(Name = "Sleep Hours")]
        public double SleepHours { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Mood")]
        public string Mood { get; set; } = string.Empty;

        [Range(20, 300)]
        [Display(Name = "Weight (Kg)")]
         public double WeightKg { get; set; }

        [Range(0.5, 2.5)]
        [Display(Name = "Height (Meters)")]
        public double HeightMeters { get; set; }

        [NotMapped]
        public bool ShowRecommendations { get; set; }

        public double BMI
        {
            get
            {
                if (HeightMeters <=0) return 0;
                return Math.Round(WeightKg / (HeightMeters * HeightMeters), 2);
            }
        }


        [NotMapped]
        public string BMICategory
        {
            get
            {
                if (BMI == 0) return "N/A";
                if (BMI < 18.5) return "Underweight";
                if (BMI < 25) return "Normal";
                if (BMI < 30) return "Overweight";
                return "Obese";
            }
        }

        [NotMapped]
        public string BMIBadgeClass
        {
            get
            {
                return BMICategory switch
                {
                    "Normal" => "bg-success",
                    "Underweight" => "bg-warning text-dark",
                    "Overweight" => "bg-warning text-dark",
                    "Obese" => "bg-danger",
                    _ => "bg-secondary"
                };
            }
        }

        [NotMapped]
        public string BMIFeedback
        {
            get
            {
                return BMICategory switch
                {
                    "Underweight" => "You may need to improve your nutrition.",
                    "Normal" => "Great job! Keep maintaining a healthy lifestyle.",
                    "Overweight" => "Consider more physical activity and balanced meals.",
                    "Obese" => "It may help to consult a health professional.",
                    _ => ""
                };
            }
        }


        [StringLength(250)]
        public string? Notes { get; set; }

        public ApplicationUser? User {get; set;}


        [NotMapped]
        public string WellnessStatus
        {
            get
            {
                int score = 0;

                if (SleepHours >= 7) score++;
                if (WaterIntakeInLiters >= 2) score++;
                if (Steps >= 7000) score++;
                if (Mood == "Great" || Mood == "Good") score++;
                if (BMI >= 18.5 && BMI <= 24.9) score++;

                if (score >= 4)
                    return "Good";
                else if (score >= 2)
                    return "Fair";
                else
                    return "Needs Attention";
            }
        }

        [NotMapped]
        public string HealthInsight
        {
            get
            {
                bool goodSleep = SleepHours >= 7;
                bool active = Steps >= 7000;
                bool hydrated = WaterIntakeInLiters >= 2;
                bool healthyBMI = BMICategory == "Normal";



                int goodHabitsCount = 0;

                if (goodSleep) goodHabitsCount++;
                if (active) goodHabitsCount++;
                if (hydrated) goodHabitsCount++;
                if (healthyBMI) goodHabitsCount++;

                if (goodHabitsCount == 4)
                {
                    return "Excellent! Your sleep, activity, hydration, and BMI all indicate a very healthy lifestyle.";
                }

                if (goodHabitsCount == 3)
                {
                    return "You're doing well overall. One small improvement—like better sleep, hydration, or activity—could make a big difference.";
                }

                if (goodHabitsCount == 2)
                {
                    return "Your habits are mixed. Try focusing on hydration, regular activity, and sleep consistency to improve your wellness.";
                }

                return "Your wellness indicators suggest that several lifestyle changes—especially hydration, movement, and sleep—could significantly improve your health.";
            }

       
        }


        [NotMapped]
        public List<(string Icon, string Color, string Message)> Recommendations
        {
            get
            {
                var list = new List<(string, string, string)>();

                // Water
                if (WaterIntakeInLiters < 2)
                    list.Add(("💧", "danger", $"You logged {WaterIntakeInLiters}L of water today. The recommended daily intake is at least 2-3 liters. Try carrying a water bottle with you!"));
                else if (WaterIntakeInLiters < 3)
                    list.Add(("💧", "warning", $"You logged {WaterIntakeInLiters}L today. You're close to the recommended 3L daily intake. Keep it up!"));
                else
                    list.Add(("💧", "success", $"Great job! You logged {WaterIntakeInLiters}L of water today. You're well hydrated!"));

                // Sleep
                if (SleepHours < 6)
                    list.Add(("😴", "danger", $"You only got {SleepHours} hours of sleep. Adults need 7-9 hours for optimal health. Try going to bed earlier tonight."));
                else if (SleepHours < 7)
                    list.Add(("😴", "warning", $"You got {SleepHours} hours of sleep. You're slightly below the recommended 7-9 hours. Try to get a bit more rest."));
                else
                    list.Add(("😴", "success", $"Excellent! You got {SleepHours} hours of sleep. That's within the healthy range of 7-9 hours!"));

                // Steps
                if (Steps < 5000)
                    list.Add(("👟", "danger", $"You walked {Steps:N0} steps today. The recommended goal is 10,000 steps. Try taking short walks during the day!"));
                else if (Steps < 10000)
                    list.Add(("👟", "warning", $"You walked {Steps:N0} steps today. You're on your way to the 10,000 step goal. Keep moving!"));
                else
                    list.Add(("👟", "success", $"Amazing! You walked {Steps:N0} steps today. You've hit the recommended 10,000 step goal!"));

                // Mood
                if (Mood == "Low" || Mood == "Stressed")
                    list.Add(("😔", "danger", $"You logged a {Mood.ToLower()} mood today. Consider taking a short walk, meditating, or talking to someone you trust."));
                else if (Mood == "Okay")
                    list.Add(("🙂", "warning", "Your mood is okay today. Try doing something you enjoy to lift your spirits!"));
                else
                    list.Add(("😊", "success", $"You're feeling {Mood.ToLower()} today. Keep doing what's working for you!"));

                return list;
            }
        }

    }

}
