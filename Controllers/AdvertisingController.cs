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
            // Chỉ được chọn 1 trong 2
            if (banner.ProductID.HasValue && !string.IsNullOrEmpty(banner.CategoryID))
            {
                ModelState.AddModelError("", "Chỉ được chọn Danh mục hoặc Sản phẩm.");
            }

            if (!banner.ProductID.HasValue && string.IsNullOrEmpty(banner.CategoryID))
            {
                ModelState.AddModelError("", "Phải chọn Danh mục hoặc Sản phẩm.");
            }

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

            // Chỉ được chọn 1 trong 2
            if (banner.ProductID.HasValue && !string.IsNullOrEmpty(banner.CategoryID))
            {
                ModelState.AddModelError("", "Chỉ được chọn Danh mục hoặc Sản phẩm.");
            }

            if (!banner.ProductID.HasValue && string.IsNullOrEmpty(banner.CategoryID))
            {
                ModelState.AddModelError("", "Phải chọn Danh mục hoặc Sản phẩm.");
            }

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
                return View(await _context.Promotions.Include(p => p.Products).Include(c => c.Category1).ToListAsync());
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi khi tải danh sách Khuyến mãi: " + ex.Message;
                System.Diagnostics.Debug.WriteLine("0323" + ex.Message);
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

        public IActionResult CreatePromotion() {
            ViewBag.ProductID = _context.Products.ToList();
        
            ViewBag.CategoryID = _context.Category.ToList();
            ViewData["Category"] = new SelectList(_context.Category, "IDCate", "NameCate");
            return View();
        }
        public class PrivateTypeVoucher {
            int? Id {get;set;}
            string? NameType {get;set;}
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePromotion(Promotion promotion)
        {
            ModelState.Remove("UsedPromotions"); //Đây là bảng trung gian chỉ dùng để lưu lịch sử, không có liên quan gì hết
            if (ModelState.IsValid)
            {
                try {
                    promotion.RemainLength = promotion.VoucherLength;
                    promotion.ApplyCategory = string.IsNullOrEmpty(promotion.ApplyCategory) ? "ALL" : promotion.ApplyCategory;
                    promotion.PriorityLength = 4;
                    _context.Add(promotion);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Promotions");
                }
                catch (Exception ex) {
                    string errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;                   
                     ViewBag.LoiTaoVoucher = "Lỗi khi tạo khuyến mãi, có lỗi với SQL. Vui lòng kiểm tra lại dữ liệu nhập." + errorMessage;
                    ViewBag.ProductID = _context.Products.ToList();
                    ViewBag.CategoryID = _context.Category.ToList();
                    ViewData["Category"] = new SelectList(_context.Category, "IDCate", "NameCate");
                    return View();
                }
            }
            if (!ModelState.IsValid)
            {
                var printErrors = "";
                // Bóc tách toàn bộ thông báo lỗi từ ModelState
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                            .Select(e => e.ErrorMessage)
                                            .ToList();

                // In từng lỗi ra cửa sổ Output (Tab Debug) của Visual Studio
                System.Diagnostics.Debug.WriteLine("=== DANH SÁCH LỖI MODELSTATE ===");
                foreach (var error in errors)
                {
                    printErrors += error + "\n";
                }
                System.Diagnostics.Debug.WriteLine("================================");
                ViewBag.LoiTaoVoucher = "Dữ liệu nhập không hợp lệ. Vui lòng kiểm tra lại.\n" + printErrors;
            }
            // ViewBag.LoiTaoVoucher = "Dữ liệu nhập không hợp lệ. Vui lòng kiểm tra lại.";
            ViewBag.ProductID = _context.Products.ToList();                    
            ViewBag.CategoryID = _context.Category.ToList();
            ViewData["Category"] = new SelectList(_context.Category, "IDCate", "NameCate");
            return View();

        }

        public async Task<IActionResult> EditPromotion(int? id)
        {
            if (id == null) return NotFound();
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null) return NotFound();
            ViewBag.ProductID = _context.Products.ToList();                    
            ViewBag.CategoryID = _context.Category.ToList();
            ViewData["Category"] = new SelectList(_context.Category, "IDCate", "NameCate");
            return View(promotion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPromotion(int id, Promotion promotion)
        {
            if (id != promotion.PromotionID) return NotFound();
            ModelState.Remove("UsedPromotions"); //Đây là bảng trung gian chỉ dùng để lưu lịch sử, không có liên quan gì hết
            if (ModelState.IsValid)
            {
                try
                {
                    promotion.ApplyCategory = string.IsNullOrEmpty(promotion.ApplyCategory) ? "ALL" : promotion.ApplyCategory;
                    _context.Update(promotion);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    string errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    ViewBag.LoiChinhVoucher = "Lỗi khi chỉnh sửa khuyến mãi, có lỗi với SQL. Vui lòng kiểm tra lại dữ liệu nhập." + errorMessage;
                    ViewBag.ProductID = _context.Products.ToList();
                    ViewBag.CategoryID = _context.Category.ToList();
                    ViewData["Category"] = new SelectList(_context.Category, "IDCate", "NameCate");
                    return View(promotion);
                }
                return RedirectToAction(nameof(Promotions));
            }
            if (!ModelState.IsValid)
            {
                var printErrors = "";
                // Bóc tách toàn bộ thông báo lỗi từ ModelState
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                            .Select(e => e.ErrorMessage)
                                            .ToList();

                // In từng lỗi ra cửa sổ Output (Tab Debug) của Visual Studio
                System.Diagnostics.Debug.WriteLine("=== DANH SÁCH LỖI MODELSTATE ===");
                foreach (var error in errors)
                {
                    printErrors += error + "\n";
                }
                System.Diagnostics.Debug.WriteLine("================================");
                ViewBag.LoiChinhVoucher = "Dữ liệu nhập không hợp lệ. Vui lòng kiểm tra lại.\n" + printErrors;
            }
            ViewBag.ProductID = _context.Products.ToList();
            ViewBag.CategoryID = _context.Category.ToList();
            ViewData["Category"] = new SelectList(_context.Category, "IDCate", "NameCate");
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
