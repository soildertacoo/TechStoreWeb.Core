using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;
using System.Linq;
using System.Threading.Tasks;

namespace TechStore.Controllers
{
    public class AdvertisingController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public AdvertisingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- Banners ---
        public async Task<IActionResult> Banners()
        {
            try
            {
                return View(await _context.Banners.OrderBy(b => b.DisplayOrder).ToListAsync());
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi khi tải danh sách Banner: " + ex.Message;
                return View("Error");
            }
        }

        public async Task<IActionResult> BannerDetails(int? id)
        {
            if (id == null) return NotFound();
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();
            return View(banner);
        }

        public IActionResult CreateBanner() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBanner(Banner banner)
        {
            if (ModelState.IsValid)
            {
                _context.Add(banner);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Banners));
            }
            return View(banner);
        }

        public async Task<IActionResult> EditBanner(int? id)
        {
            if (id == null) return NotFound();
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();
            return View(banner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBanner(int id, Banner banner)
        {
            if (id != banner.BannerID) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(banner);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Banners));
            }
            return View(banner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBanner(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();

            _context.Banners.Remove(banner);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Banners));
        }

        // --- Promotions ---
        public async Task<IActionResult> Promotions()
        {
            try
            {
                return View(await _context.Promotions.ToListAsync());
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi khi tải danh sách Khuyến mãi: " + ex.Message;
                return View("Error");
            }
        }

        public async Task<IActionResult> PromotionDetails(int? id)
        {
            if (id == null) return NotFound();
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null) return NotFound();
            return View(promotion);
        }

        public IActionResult CreatePromotion() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePromotion(Promotion promotion)
        {
            if (ModelState.IsValid)
            {
                _context.Add(promotion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Promotions));
            }
            return View(promotion);
        }

        public async Task<IActionResult> EditPromotion(int? id)
        {
            if (id == null) return NotFound();
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null) return NotFound();
            return View(promotion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPromotion(int id, Promotion promotion)
        {
            if (id != promotion.PromotionID) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(promotion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Promotions));
            }
            return View(promotion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null) return NotFound();

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Promotions));
        }
    }
}
