using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using HealthTracker.Data;
using HealthTracker.Models;
using HealthTracker.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace Health_Tracker.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _env;
        private readonly Cloudinary _cloudinary;


        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,SignInManager<ApplicationUser> signInmanager,
            IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInmanager;
            _env = env;

            var account = new Account(
        config["Cloudinary:CloudName"],
        config["Cloudinary:ApiKey"],
        config["Cloudinary:ApiSecret"]
           );

            _cloudinary = new Cloudinary(account);
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

            using var stream = photo.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(photo.FileName, stream),
                Transformation = new Transformation()
                    .Width(300)
                    .Height(300)
                    .Crop("fill")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                TempData["UploadError"] = uploadResult.Error.Message;
                return RedirectToAction("Profile");
            }

            user.ProfilePhotoPath = uploadResult.SecureUrl.ToString();
            await _userManager.UpdateAsync(user);

            return RedirectToAction("Profile");
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ApplicationUser model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Profile updated successfully.";

            return RedirectToAction("Profile");
        }

        public IActionResult ChangePassword()
        {
                       return View();

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Password changed successfully.";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
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
