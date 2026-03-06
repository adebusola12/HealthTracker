using HealthTracker.Data;
using HealthTracker.Models;
using HealthTracker.Models.ViewModels;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
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

        public WellnessController(ApplicationDbContext context)
        {
            _context = context;
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
                return RedirectToAction("Index");
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

    }
}