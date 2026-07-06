using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace TechStore.Controllers
{
    public class AdvertisingController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly DBTechStoreEntities dbO;

        public AdvertisingController(ApplicationDbContext context, DBTechStoreEntities  dbContext)
        {
            _context = context;
            dbO = dbContext;
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

        public IActionResult CreateBanner()
        {
            LoadProducts();
            LoadCategories();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBanner(Banner banner)
        {
            if (ModelState.IsValid)
            {
                banner.CreatedDate = DateTime.Now;

                //Nếu admin không nhập ngày bắt đầu thì lấy thời điểm hiện tại
                if (banner.StartDate == null)
                {
                    banner.StartDate = DateTime.Now;
                }
                _context.Add(banner);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Banner đã được tạo thành công!";
                return RedirectToAction(nameof(Banners));
            }
            LoadProducts();
            return View(banner);
        }

        private void LoadCategories()
        {
            ViewBag.Categories = new SelectList(
                dbO.Category.OrderBy(x => x.NameCate).ToList(),
                "IDCate",
                "NameCate");
        }

        public async Task<IActionResult> EditBanner(int? id)
        {
            if (id == null) return NotFound();
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();
            LoadProducts();
            LoadCategories();
            return View(banner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBanner(int id, Banner banner)
        {
            Console.WriteLine($"CategoryID = {banner.CategoryID}");
            Console.WriteLine($"ProductID = {banner.ProductID}");
            Console.WriteLine($"LinkUrl = {banner.LinkUrl}");
            Console.WriteLine($"Title = {banner.Title}");

            if (id != banner.BannerID) return NotFound();

            ModelState.Remove(nameof(Banner.LinkUrl));

            if (!ModelState.IsValid)
            {
                LoadCategories();
                LoadProducts();

                foreach (var item in ModelState)
                {
                    foreach (var error in item.Value.Errors)
                    {
                        // Log lỗi hoặc xử lý theo nhu cầu của bạn
                        Console.WriteLine($"Field = {item.Key}");
                        Console.WriteLine($"Error = {error.ErrorMessage}");                    }
                }        

                Console.WriteLine(ModelState.IsValid);

                
                return View(banner);
            }

            var oldBanner = await _context.Banners.FindAsync(id);
            
            if (oldBanner == null)
               return NotFound();

            oldBanner.Title = banner.Title;
            oldBanner.ImageUrl = banner.ImageUrl;
            //oldBanner.LinkUrl = banner.LinkUrl;
            oldBanner.StartDate = banner.StartDate;
            oldBanner.EndDate = banner.EndDate;
            oldBanner.DisplayOrder = banner.DisplayOrder;
            oldBanner.IsActive = banner.IsActive;
            oldBanner.CategoryID = banner.CategoryID;
            oldBanner.ProductID = banner.ProductID;

            Console.WriteLine("===== Before Save =====");
            Console.WriteLine(oldBanner.Title);
            Console.WriteLine(oldBanner.ImageUrl);
            Console.WriteLine(oldBanner.ProductID);
            Console.WriteLine(oldBanner.CategoryID);

            var result = await _context.SaveChangesAsync();

            Console.WriteLine($"Rows affected = {result}");

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật thành công";

            return RedirectToAction(nameof(Banners));
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

        private void LoadProducts()
        {
            ViewBag.Products = new SelectList(
                dbO.Products.OrderBy(x => x.NamePro).ToList(),
                "ProductID",
                "NamePro");

            ViewBag.Categories = new SelectList(
                dbO.Category.OrderBy(x => x.NameCate).ToList(),
                "IDCate",
                "NameCate");
        }

        public IActionResult BannerProducts(int id)
        {
            var banner = _context.Banners.FirstOrDefault(x => x.BannerID == id);
           

            if (banner == null)
            {
                return RedirectToAction("Index", "Home");
            }

             // Lấy danh sách sản phẩm liên quan đến banner này
            IQueryable<Products> products = dbO.Products;

            //Nếu chọn 1 sản phẩm
            if (banner.ProductID.HasValue)
            {
                var p = dbO.Products.FirstOrDefault(
                    x => x.ProductID == banner.ProductID.Value);
                
                if (p != null)
                {
                    products = products
                    .Where(x => x.Category == p.Category);
                }
            }

            //Nếu chọn cả danh mục
            else if (!string.IsNullOrEmpty(banner.CategoryID))
            {
                products = products
                    .Where(x => x.Category == banner.CategoryID);
            }

           ViewBag.BannerTitle = banner.Title;

           return View(products.ToList());
        }

        public IActionResult BannerRedirect(int id)
        {
            var banner = _context.Banners.FirstOrDefault(x => x.BannerID == id);

            if (banner == null)
                return RedirectToAction("Index", "Home");

           return RedirectToAction(nameof(BannerProducts), new { id });
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
