using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using TechStore.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace TechStore.Controllers
{
    public class CustomerProController : Controller
    {
        private readonly DBTechStoreEntities db;
        private readonly ApplicationDbContext _context;

        public CustomerProController(DBTechStoreEntities dbContext, ApplicationDbContext appContext)
        {
            db = dbContext;
            _context = appContext;
        }

        [HttpGet]
        public ActionResult Details(int id)
        {
            var Products = _context.Products.FirstOrDefault(s => s.ProductID == id);
            var inventory = _context.Inventories.FirstOrDefault(s => s.ProductID == id);
            var reviews = _context.Reviews.Where(s => s.ProductID == id && s.IsHidden == false).ToList();

            // Tính toán số lượng đã bán cho mỗi sản phẩm
            var soldQuantities = new Dictionary<int, int>();

            // Query để lấy số lượng đã bán từ OrderDetails
            var soldItems = db.OrderDetails
                .Join(db.OrderPro,
                    od => od.IDOrder,
                    op => op.ID,
                    (od, op) => new { od.IDProduct, od.Quantity })
                .GroupBy(x => x.IDProduct)
                .Select(g => new {
                    ProductID = g.Key,
                    TotalSold = g.Sum(x => x.Quantity)
                })
                .ToDictionary(x => x.ProductID, x => x.TotalSold);

            // Khởi tạo mặc định số lượng bán = 0 cho tất cả sản phẩm
            foreach (var pro in db.Products)
            {
                soldQuantities[pro.ProductID] = (int)(soldItems.ContainsKey(pro.ProductID)
                    ? soldItems[pro.ProductID]
                    : 0);
            }

            // ==============================================================
            // BẮT ĐẦU: LẤY SẢN PHẨM GỢI Ý TỪ THUẬT TOÁN APRIORI
            // ==============================================================
            var aiProductIds = _context.ProductRecommendations
                .Where(r => r.ProductID_A == id)
                .OrderByDescending(r => r.Confidence) // Ưu tiên tỷ lệ mua chung cao nhất
                .Take(4) // Lấy tối đa 4 sản phẩm
                .Select(r => r.ProductID_B)
                .ToList();

            ViewBag.AI_Recommendations = _context.Products
                .Where(p => aiProductIds.Contains(p.ProductID))
                .ToList();
            // ==============================================================
            // KẾT THÚC: LẤY SẢN PHẨM GỢI Ý
            // ==============================================================

            var relatedPro = new RelatedPro
            {
                Products = Products,
                RelatedReviews = reviews,
                SoldItem = soldQuantities,
                StockQuantity = inventory?.StockQuantity ?? 0
            };
            return View(relatedPro);
        }

        public class ReviewJSON
        {
            public decimal score { get; set; }
            public string? content { get; set; }
            public int proID { get; set; }
        }
        
        [HttpPost]
        public async Task<ActionResult> CreateReview([FromBody] ReviewJSON data)
        {
            // 1. Kiểm tra đăng nhập
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Bạn phải đăng nhập mới đánh giá sản phẩm này được." });
            }

            try 
            {
                var userName = User.Identity.Name;
                
                // Thống nhất dùng 1 biến context (giả sử biến của bạn tên là 'db')
                var cus = db.Customers.FirstOrDefault(s => s.NameCus == userName);
                
                // 2. Bắt lỗi nếu không tìm thấy Customer trong DB
                if (cus == null)
                {
                    return Json(new { success = false, message = "Lỗi: Không tìm thấy thông tin tài khoản của bạn." });
                }

                // 3. Kiểm tra khách hàng đã mua sản phẩm này và đơn đã giao chưa
                var customerHasPurchased = db.OrderPro
                    .Where(op => op.Status == "Đã giao" && op.IDCus == cus.IDCus)
                    .Join(db.OrderDetails,
                        op => op.ID,
                        od => od.IDOrder,
                        (op, od) => od.IDProduct)
                    .Any(ProductID => ProductID == data.proID);

                if (!customerHasPurchased)
                {
                    return Json(new { success = false, message = "Bạn phải hoàn thành đơn hàng mới có thể đánh giá sản phẩm này." });
                }

                // 4. Xử lý logic Thêm/Sửa đánh giá
                var existingReview = db.Reviews.FirstOrDefault(r => r.ProductID == data.proID && r.CustomerID == cus.IDCus);

                if (existingReview == null) // CHƯA CÓ -> TẠO MỚI
                {
                    var re = new Review()
                    {
                        ProductID = data.proID,
                        CustomerID = cus.IDCus,
                        Rating = data.score,
                        ReviewContent = data.content,
                        ReviewDate = DateTime.Now,
                        ReviewerName = cus.NameCus,
                        IsHidden = false // Mặc định hiển thị
                    };
                    db.Reviews.Add(re);
                }
                else // ĐÃ CÓ -> CẬP NHẬT
                {
                    existingReview.ReviewContent = data.content;
                    existingReview.Rating = data.score;
                    existingReview.ReviewDate = DateTime.Now;
                    db.Entry(existingReview).State = EntityState.Modified;
                }

                // 5. Lưu thay đổi với await
                await db.SaveChangesAsync();
                
                return Json(new { success = true, message = "Đánh giá thành công!" });
            }
            catch (Exception ex)
            {
                // Ghi log lỗi để dễ debug trong ASP.NET Framework
                System.Diagnostics.Debug.WriteLine("Bị lỗi khi up review: " + ex.Message);
                return Json(new { success = false, message = "Đã xảy ra lỗi hệ thống khi gửi đánh giá." });
            }
        }
    }
}