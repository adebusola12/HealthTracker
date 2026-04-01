using HealthTracker.Data;
using HealthTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthTracker.Services
{
    public class WeeklySummaryService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WeeklySummaryService> _logger;

        public WeeklySummaryService(IServiceProvider serviceProvider, ILogger<WeeklySummaryService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextSunday = GetNextSunday(now);
                var delay = nextSunday - now;

                _logger.LogInformation($"Next weekly summary will be sent on {nextSunday}");
                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await SendWeeklySummariesAsync();
                }
            }
        }

        private DateTime GetNextSunday(DateTime from)
        {
            return from.AddMinutes(2); // TESTING: remove this line after testing
        }

        private async Task SendWeeklySummariesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

            var oneWeekAgo = DateTime.Today.AddDays(-7);

            var users = await context.Users.OfType<ApplicationUser>().ToListAsync();

            foreach (var user in users)
            {
                var entries = await context.WellnessEntries
                    .Where(e => e.UserId == user.Id && e.Date >= oneWeekAgo)
                    .ToListAsync();

                if (!entries.Any()) continue;

                var avgSleep = Math.Round(entries.Average(e => e.SleepHours), 1);
                var avgWater = Math.Round(entries.Average(e => e.WaterIntakeInLiters), 1);
                var avgSteps = (int)entries.Average(e => e.Steps);
                var mostCommonMood = entries
                    .GroupBy(e => e.Mood)
                    .OrderByDescending(g => g.Count())
                    .First().Key;

                var sleepRec = avgSleep < 6 ? "⚠️ Your average sleep was below 6 hours. Try to aim for 7-9 hours each night." :
                               avgSleep < 7 ? "🙂 You're close to the recommended 7-9 hours of sleep. A little more rest would help!" :
                               "✅ Great sleep this week! You averaged within the healthy 7-9 hour range.";

                var waterRec = avgWater < 2 ? "⚠️ Your average water intake was below 2L. Try to drink at least 2-3L daily." :
                               avgWater < 3 ? "🙂 You're close to the recommended 3L daily intake. Keep it up!" :
                               "✅ Excellent hydration this week!";

                var stepsRec = avgSteps < 5000 ? "⚠️ Your average steps were below 5,000. Try taking short walks during the day!" :
                               avgSteps < 10000 ? "🙂 You're on your way to the 10,000 step goal. Keep moving!" :
                               "✅ Amazing! You averaged over 10,000 steps this week!";

                var moodRec = mostCommonMood == "Low" || mostCommonMood == "Stressed" ?
                               $"⚠️ Your most common mood this week was {mostCommonMood.ToLower()}. Consider talking to someone or taking time for self-care." :
                               mostCommonMood == "Okay" ? "🙂 Your mood was generally okay this week. Try to do something you enjoy!" :
                               $"✅ Your mood was mostly {mostCommonMood.ToLower()} this week. Keep it up!";

                var emailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: auto;'>
                    <h2 style='color: #4CAF50;'>Your Weekly Wellness Summary 🌿</h2>
                    <p>Hi {user.FirstName}, here's how you did this week!</p>
                    
                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                        <tr style='background-color: #f2f2f2;'>
                            <th style='padding: 10px; text-align: left;'>Metric</th>
                            <th style='padding: 10px; text-align: left;'>Your Average</th>
                            <th style='padding: 10px; text-align: left;'>Goal</th>
                        </tr>
                        <tr>
                            <td style='padding: 10px;'>😴 Sleep</td>
                            <td style='padding: 10px;'>{avgSleep} hours</td>
                            <td style='padding: 10px;'>7-9 hours</td>
                        </tr>
                        <tr style='background-color: #f9f9f9;'>
                            <td style='padding: 10px;'>💧 Water Intake</td>
                            <td style='padding: 10px;'>{avgWater}L</td>
                            <td style='padding: 10px;'>2-3L</td>
                        </tr>
                        <tr>
                            <td style='padding: 10px;'>👟 Steps</td>
                            <td style='padding: 10px;'>{avgSteps:N0}</td>
                            <td style='padding: 10px;'>10,000</td>
                        </tr>
                        <tr style='background-color: #f9f9f9;'>
                            <td style='padding: 10px;'>😊 Most Common Mood</td>
                            <td style='padding: 10px;'>{mostCommonMood}</td>
                            <td style='padding: 10px;'>Great / Good</td>
                        </tr>
                    </table>

                    <h3 style='color: #4CAF50;'>📋 Recommendations</h3>
                    <p>{sleepRec}</p>
                    <p>{waterRec}</p>
                    <p>{stepsRec}</p>
                    <p>{moodRec}</p>

                    <br/>
                    <p>Keep up the great work and stay healthy!</p>
                    <p>regards,<br/><strong>The HealthTracker Team</strong></p>
                </body>
                </html>";

                try
                {
                    await emailService.SendEmailAsync(
                        user.Email,
                        "Your Weekly Wellness Summary 🌿",
                        emailBody);

                    _logger.LogInformation($"Weekly summary sent to {user.Email}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to send weekly summary to {user.Email}: {ex.Message}");
                }
            }
        }
    }
}