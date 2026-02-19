using System.Diagnostics;
using Health_Tracker.Data;
using Health_Tracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Health_Tracker.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var lastEntry = await _context.WellnessEntries
                .Where(e=> e.UserId == userId)
                .OrderByDescending(e => e.Date)
                .FirstOrDefaultAsync();

            string? firstName = null;

            if(User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                firstName = user?.FirstName;
            }

            ViewBag.FirstName = firstName;

            return View(lastEntry);
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                TempData["UploadError"] = "Please select a file before uploading.";
                return RedirectToAction("Profile");
            }

            var user = await _userManager.GetUserAsync(User);

            var fileName = Guid.NewGuid() + Path.GetExtension(photo.FileName);
            var uploadPath = Path.Combine(_env.WebRootPath, "uploads");

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var fullPath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            user.ProfilePhotoPath = "/uploads/" + fileName;
            await _userManager.UpdateAsync(user);

            return RedirectToAction("Profile");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }



    }
}
