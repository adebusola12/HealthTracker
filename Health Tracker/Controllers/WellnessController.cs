using HealthTracker.Data;
using HealthTracker.Models;
using HealthTracker.ViewModels;
using HealthTracker.Services;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Reflection;
using System.Runtime.Intrinsics.X86;
using System.Security.Claims;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Health_Tracker.Controllers
{
    [Authorize]
    public class WellnessController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        public WellnessController(ApplicationDbContext context, EmailService emailService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Get current user's ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Only get entries for the logged-in user
            var entries = await _context.WellnessEntries
                .Where(e => e.UserId == userId)
                .OrderByDescending(w => w.Date)
                .ToListAsync();

            return View(entries);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var entry = new WellnessEntry
            {
                Date = DateTime.Today
            };

            return View(entry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WellnessEntry entry)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Normalize date
            entry.Date = entry.Date.Date;

            // Prevent future dates
            if (entry.Date > DateTime.Today)
            {
                ModelState.AddModelError("Date", "You cannot add entries for future dates.");
                return View(entry);
            }

            var existingEntry = await _context.WellnessEntries
                .FirstOrDefaultAsync(e => e.UserId == userId && e.Date == entry.Date);

            if (existingEntry != null)
            {
                TempData["Error"] = "You already have a wellness entry for this date.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                entry.UserId = userId;
                _context.WellnessEntries.Add(entry);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Entry saved successfully!";

                var displayEntry = new WellnessEntry
                {
                    Date = entry.Date,
                    WaterIntakeInLiters = entry.WaterIntakeInLiters,
                    Steps = entry.Steps,
                    SleepHours = entry.SleepHours,
                    Mood = entry.Mood,
                    WeightKg = entry.WeightKg,
                    HeightMeters = entry.HeightMeters,
                    ShowRecommendations = true
                };

                return View(displayEntry);
            }

            return View(entry);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Make sure the entry belongs to the current user
            var entry = await _context.WellnessEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (entry == null)
            {
                return NotFound();
            }

            return View(entry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WellnessEntry entry)
        {
            if (id != entry.Id)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Verify the entry belongs to the current user
            var existingEntry = await _context.WellnessEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (existingEntry == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update fields but keep the original UserId
                    existingEntry.Date = entry.Date.Date;
                    existingEntry.Mood = entry.Mood;
                    existingEntry.SleepHours = entry.SleepHours;
                    existingEntry.WaterIntakeInLiters = entry.WaterIntakeInLiters;
                    existingEntry.Steps = entry.Steps;
                    existingEntry.WeightKg = entry.WeightKg;
                    existingEntry.HeightMeters = entry.HeightMeters;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.WellnessEntries.Any(e => e.Id == entry.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(entry);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Make sure the entry belongs to the current user
            var entry = await _context.WellnessEntries
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (entry == null)
                return NotFound();

            return View(entry);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Only delete if it belongs to the current user
            var entry = await _context.WellnessEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (entry != null)
            {
                _context.WellnessEntries.Remove(entry);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Make sure the entry belongs to the current user
            var entry = await _context.WellnessEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (entry == null)
                return NotFound();

            return View(entry);
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var entries = await _context.WellnessEntries
                .Where(e => e.UserId == userId)
                .OrderBy(e => e.Date)
                .ToListAsync();

            // ================= STREAK LOGIC =================
            int streak = 0;

            var entryDates = entries
                .Select(e => e.Date.Date)
                .OrderByDescending(d => d)
                .ToList();

            if (entryDates.Any())
            {
                DateTime checkDate = entryDates.First();

                foreach (var date in entryDates)
                {
                    if (date == checkDate)
                    {
                        streak++;
                        checkDate = checkDate.AddDays(-1);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            ViewBag.Streak = streak;

            // ================= MOOD DISTRIBUTION =================
            var moodGroups = entries
                .GroupBy(e => e.Mood)
                .ToDictionary(g => g.Key, g => g.Count());

            // ================= WELLNESS SCORE =================
            int wellnessScore = 0;

            if (entries.Any())
            {
                wellnessScore =
                    (entries.Average(e => e.SleepHours) >= 7 ? 20 : 0) +
                    (entries.Average(e => e.Steps) >= 7000 ? 20 : 0) +
                    (entries.Average(e => e.WaterIntakeInLiters) >= 2 ? 20 : 0) +
                    (entries.Average(e => e.BMI) >= 18.5 && entries.Average(e => e.BMI) <= 24.9 ? 20 : 0) +
                    (entries.Count(e => e.Mood == "Great" || e.Mood == "Good") > entries.Count / 2 ? 20 : 0);
            }

            // ================= BUILD VIEWMODEL =================
            var model = new DashboardViewModel
            {
                Entries = entries,
                TotalEntries = entries.Count,
                AvgSleep = entries.Any() ? entries.Average(e => e.SleepHours) : 0,
                AvgSteps = entries.Any() ? entries.Average(e => e.Steps) : 0,
                AvgWater = entries.Any() ? entries.Average(e => e.WaterIntakeInLiters) : 0,
                AvgBMI = entries.Any() ? entries.Average(e => e.BMI) : 0,
                WellnessScore = wellnessScore,
                MoodDistribution = moodGroups
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SendWeeklySummary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId) as ApplicationUser;

            var oneWeekAgo = DateTime.Today.AddDays(-7);
            var entries = await _context.WellnessEntries
                .Where(e => e.UserId == userId && e.Date >= oneWeekAgo)
                .ToListAsync();

            if (!entries.Any())
            {
                TempData["Error"] = "No wellness entries found for this week. Add some entries first!";
                return RedirectToAction("Index");
            }

            var avgSleep = Math.Round(entries.Average(e => e.SleepHours), 1);
            var avgWater = Math.Round(entries.Average(e => e.WaterIntakeInLiters), 1);
            var avgSteps = (int)entries.Average(e => e.Steps);
            var mostCommonMood = entries
                .GroupBy(e => e.Mood)
                .OrderByDescending(g => g.Count())
                .First().Key;

            var model = new WeeklySummaryViewModel
            {
                FirstName = user.FirstName,
                AvgSleep = avgSleep,
                AvgWater = avgWater,
                AvgSteps = avgSteps,
                MostCommonMood = mostCommonMood,
                TotalEntries = entries.Count,
                WeekStart = oneWeekAgo,
                WeekEnd = DateTime.Today,
                SleepRecommendation = avgSleep < 6 ? "⚠️ Your average sleep was below 6 hours. Try to aim for 7-9 hours each night." :
                                      avgSleep < 7 ? "🙂 You're close to the recommended 7-9 hours. A little more rest would help!" :
                                      "✅ Great sleep this week! You averaged within the healthy 7-9 hour range.",
                WaterRecommendation = avgWater < 2 ? "⚠️ Your average water intake was below 2L. Try to drink at least 2-3L daily." :
                                      avgWater < 3 ? "🙂 You're close to the recommended 3L daily intake. Keep it up!" :
                                      "✅ Excellent hydration this week!",
                StepsRecommendation = avgSteps < 5000 ? "⚠️ Your average steps were below 5,000. Try taking short walks during the day!" :
                                      avgSteps < 10000 ? "🙂 You're on your way to the 10,000 step goal. Keep moving!" :
                                      "✅ Amazing! You averaged over 10,000 steps this week!",
                MoodRecommendation = mostCommonMood == "Low" || mostCommonMood == "Stressed" ?
                                      $"⚠️ Your most common mood was {mostCommonMood.ToLower()}. Consider talking to someone or taking time for self-care." :
                                      mostCommonMood == "Okay" ? "🙂 Your mood was generally okay. Try to do something you enjoy!" :
                                      $"✅ Your mood was mostly {mostCommonMood.ToLower()} this week. Keep it up!"
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SendWeeklySummaryEmail()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId) as ApplicationUser;

            var oneWeekAgo = DateTime.Today.AddDays(-7);
            var entries = await _context.WellnessEntries
                .Where(e => e.UserId == userId && e.Date >= oneWeekAgo)
                .ToListAsync();

            if (!entries.Any())
            {
                TempData["Error"] = "No entries found to send!";
                return RedirectToAction("Index");
            }

            var avgSleep = Math.Round(entries.Average(e => e.SleepHours), 1);
            var avgWater = Math.Round(entries.Average(e => e.WaterIntakeInLiters), 1);
            var avgSteps = (int)entries.Average(e => e.Steps);
            var mostCommonMood = entries
                .GroupBy(e => e.Mood)
                .OrderByDescending(g => g.Count())
                .First().Key;

            var sleepRec = avgSleep < 6 ? "⚠️ Your average sleep was below 6 hours. Try to aim for 7-9 hours each night." :
                           avgSleep < 7 ? "🙂 You're close to the recommended 7-9 hours. A little more rest would help!" :
                           "✅ Great sleep this week!";

            var waterRec = avgWater < 2 ? "⚠️ Your average water intake was below 2L. Try to drink at least 2-3L daily." :
                           avgWater < 3 ? "🙂 You're close to the recommended 3L daily intake. Keep it up!" :
                           "✅ Excellent hydration this week!";

            var stepsRec = avgSteps < 5000 ? "⚠️ Your average steps were below 5,000. Try taking short walks!" :
                           avgSteps < 10000 ? "🙂 You're on your way to the 10,000 step goal. Keep moving!" :
                           "✅ Amazing! You averaged over 10,000 steps this week!";

            var moodRec = mostCommonMood == "Low" || mostCommonMood == "Stressed" ?
                           $"⚠️ Your most common mood was {mostCommonMood.ToLower()}. Consider self-care." :
                           mostCommonMood == "Okay" ? "🙂 Your mood was generally okay. Try to do something you enjoy!" :
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
            <tr><td style='padding: 10px;'>😴 Sleep</td><td style='padding: 10px;'>{avgSleep} hours</td><td style='padding: 10px;'>7-9 hours</td></tr>
            <tr style='background-color: #f9f9f9;'><td style='padding: 10px;'>💧 Water</td><td style='padding: 10px;'>{avgWater}L</td><td style='padding: 10px;'>2-3L</td></tr>
            <tr><td style='padding: 10px;'>👟 Steps</td><td style='padding: 10px;'>{avgSteps:N0}</td><td style='padding: 10px;'>10,000</td></tr>
            <tr style='background-color: #f9f9f9;'><td style='padding: 10px;'>😊 Mood</td><td style='padding: 10px;'>{mostCommonMood}</td><td style='padding: 10px;'>Great / Good</td></tr>
        </table>
        <h3 style='color: #4CAF50;'>📋 Recommendations</h3>
        <p>{sleepRec}</p><p>{waterRec}</p><p>{stepsRec}</p><p>{moodRec}</p>
        <br/><p>Keep up the great work!</p>
        <p>regards,<br/><strong>The HealthTracker Team</strong></p>
    </body>
    </html>";

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Your Weekly Wellness Summary 🌿",
                    emailBody);
                TempData["Success"] = "Your weekly summary has been sent to your email!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to send email: " + ex.Message;
                Console.WriteLine("EMAIL ERROR: " + ex.Message);
            }
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public async Task<IActionResult> SendSleepReminders()
        {
            var users = await _context.Users
        .OfType<ApplicationUser>()
        .Where(u => u.BedTime.HasValue)
        .ToListAsync();

            var now = DateTime.Now.TimeOfDay;

            foreach (var user in users)
            {
                if (user.BedTime.HasValue)
                {
                    var emailBody = $@"
            <html>
            <body style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: auto;'>
                <h2 style='color: #4CAF50;'>😴 Time to Sleep, {user.FirstName}!</h2>
                <p>This is your bedtime reminder. It's time to put down your phone and get some rest!</p>
                <p>Remember, adults need <strong>7-9 hours</strong> of sleep for optimal health.</p>
                <br/>
                <p>Good night! 🌙</p>
                <p>regards,<br/><strong>The HealthTracker Team</strong></p>
            </body>
            </html>";

                    try
                    {
                        await _emailService.SendEmailAsync(
                            user.Email,
                            "😴 Time to Sleep! - HealthTracker Reminder",
                            emailBody);

                        Console.WriteLine($"Sleep reminder sent to {user.Email}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send sleep reminder to {user.Email}: {ex.Message}");
                    }
                }
            }

            return Ok("Sleep reminders sent!");
        }
    }
}