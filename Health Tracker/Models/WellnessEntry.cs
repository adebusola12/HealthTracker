using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Intrinsics.X86;

namespace Health_Tracker.Models
{
    public class WellnessEntry
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Range(0, 20)]    
        public int WaterIntakeInLiters { get; set; }

        [Range(0, 100000)]
        public int Steps { get; set; }

        [Range(0, 24)]
        public double SleepHours { get; set; }

        [Required]
        [StringLength(20)]
        public string Mood { get; set; } = string.Empty;
        [Range(20, 300)]
         public double WeightKg { get; set; }

        [Range(0.5, 2.5)]
        public double HeightMeters { get; set; }

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
    }
}
