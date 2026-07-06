using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using TechStore.Models;
using TechStoreWeb.Core.AI;
using static TechStoreWeb.Core.AI.geminiGen;

namespace TechStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly DBTechStoreEntities dbO;
        private readonly ApplicationDbContext _context;

    // 2. Tạo hàm khởi tạo (Constructor) và yêu cầu hệ thống "tiêm" DbContext vào
    public HomeController(DBTechStoreEntities dbContext, ApplicationDbContext appContext)
    {
        dbO = dbContext;
        _context = appContext;
    }
        public ActionResult Index()
        {
            using (var db = dbO)
            {
                var model = new indexProducts();

                // Danh sách sản phẩm
                model.products = db.Products.ToList();

                var today = DateTime.Now;

                model.banners = _context.Banners
                      .Where(x => 
                      x.IsActive && 
                      (!x.StartDate.HasValue || x.StartDate <= today) && 
                      (!x.EndDate.HasValue || x.EndDate >= today)
                      )
                      .OrderBy(x => x.DisplayOrder)
                      .ToList();

                // Lấy tất cả sản phẩm
                var proList = db.Products.ToList();
                // Tính toán số lượng đã bán cho mỗi sản phẩm
                var soldQuantities = new Dictionary<int, int>();

                //Lấy điểm của sản phẩm và số lượng đánh giá 
                // Query để lấy số lượng đã bán từ OrderDetailss
                if(proList.Count > 0)
                {
                    // 1. Tính tổng số lượng bán và lấy ra Top 3 ProductID bán chạy nhất
                    var top3SellerInfo = dbO.OrderDetails
                        .Join(dbO.OrderPro, 
                            od => od.IDOrder, 
                            op => op.ID,
                            (od, op) => new { od.IDProduct, od.Quantity })
                        .GroupBy(x => x.IDProduct)
                        .Select(g => new { 
                            ProductID = g.Key, 
                            TotalSold = g.Sum(x => x.Quantity) 
                        })
                        .Where(x => x.ProductID > 0)
                        .OrderByDescending(x => x.TotalSold) // Sắp xếp từ bán nhiều nhất đến ít nhất
                        .Take(3) // Cắt lấy đúng 3 ông đứng đầu
                        .ToList();

                    // 2. Lấy danh sách ID của 3 ông top này
                    var top3ProductIds = top3SellerInfo.Select(x => x.ProductID).ToList();

                    // 3. Truy vấn bảng Products để lấy ra thông tin đầy đủ của 3 sản phẩm này
                    var top3BestSellers = dbO.Products
                        .Where(p => top3ProductIds.Contains(p.ProductID))
                        .AsEnumerable() // Chuyển xử lý về RAM để giữ đúng thứ tự xếp hạng
                        .OrderBy(p => top3ProductIds.IndexOf(p.ProductID)) // Giữ đúng thứ tự Top 1, 2, 3
                        .ToList();

                    var soldItems = db.OrderDetails
                        .Join(db.OrderPro, 
                            od => od.IDOrder, 
                            op => op.ID,
                            (od, op) => new { od.IDProduct, od.Quantity })
                        .GroupBy(x => x.IDProduct)
                        .Select(g => new { 
                            ProductID = g.Key, 
                            TotalSold = g.Sum(x => x.Quantity) 
                        }).Where(x => x.ProductID > 0)
                        .ToDictionary(x => x.ProductID, x => x.TotalSold);

                    //Lay diem trung binh moi san pham
                    var scoreProducts = db.Reviews.Join(
                        db.Products, rv => rv.ProductID , 
                        pro => pro.ProductID,
                        (rv,pro) => new {pro.ProductID, rv.Rating}
                    ).GroupBy(x => x.ProductID)
                    .Select(x => new
                    {
                        ProductID = x.Key,
                        midScores = x.Average(item => item.Rating),
                        scoreNumbers = x.Count()
                    }).ToDictionary(x => x.ProductID, x => (x.midScores , x.scoreNumbers))
                    ;
                    
                    // Truyền dữ liệu số lượng đã bán, điểm  
                    var indexpros = new indexProducts
                    {
                        products = proList,
                        soldQuantities = soldItems,
                        scoreProducts = scoreProducts,
                        banners = model.banners,
                        bestSeller = top3BestSellers
                    };
                    return View(indexpros);
                }
            }
            return View(new indexProducts());
        }
        [HttpPost]
        public ActionResult LogToOutput(string message)
        {
            // Log the message to the output (e.g., console, debug output, etc.)
            System.Diagnostics.Debug.WriteLine(message);
            return new EmptyResult(); // Return an empty result since this is a logging action
        }
     // 1. VIẾT MỘT HÀM PHỤ ĐỂ LỘT SẠCH DẤU TIẾNG VIỆT
        // Khách gõ "Mắc búc" hay "mác búc" thì nó đều biến thành "mac buc"
        private string RemoveVietnameseAccents(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            
            string[] arr1 = new string[] { "á", "à", "ả", "ã", "ạ", "â", "ấ", "ầ", "ẩ", "ẫ", "ậ", "ă", "ắ", "ằ", "ẳ", "ẵ", "ặ",
            "đ", "é","è","ẻ","ẽ","ẹ","ê","ế","ề","ể","ễ","ệ", "í","ì","ỉ","ĩ","ị",
            "ó","ò","ỏ","õ","ọ","ô","ố","ồ","ổ","ỗ","ộ","ơ","ớ","ờ","ở","ỡ","ợ",
            "ú","ù","ủ","ũ","ụ","ư","ứ","ừ","ử","ữ","ự", "ý","ỳ","ỷ","ỹ","ỵ" };
            
            string[] arr2 = new string[] { "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a",
            "d", "e","e","e","e","e","e","e","e","e","e","e", "i","i","i","i","i",
            "o","o","o","o","o","o","o","o","o","o","o","o","o","o","o","o","o",
            "u","u","u","u","u","u","u","u","u","u","u", "y","y","y","y","y" };
            
            text = text.ToLower().Trim();
            for (int i = 0; i < arr1.Length; i++)
            {
                text = text.Replace(arr1[i], arr2[i]);
            }
            return text;
        }

        // 2. HÀM TÌM KIẾM CHÍNH THỨC
        [HttpPost]
        public ActionResult Search(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return View(new List<Products>());
            }

            try
            {
                // Bước 1: Cắt khoảng trắng dư và loại bỏ toàn bộ dấu tiếng Việt
                string originalKeyword = keyword.Trim().ToLower();
                string cleanKeyword = RemoveVietnameseAccents(originalKeyword);

                // Bước 2: Chuẩn hóa "Từ lóng không dấu" thành tên chuẩn
                // Vì đã bỏ dấu rồi nên bạn không cần bắt trường hợp "mắc búc" hay "mác búc" nữa,
                // Chỉ cần bắt mỗi chữ "mac buc" là ôm trọn mọi thể loại sai dấu!
                string searchKeyword = cleanKeyword
                    .Replace("ai bat", "ipad")
                    .Replace("ai pat", "ipad")
                    .Replace("ai phon", "iphone")
                    .Replace("ip phon", "iphone")
                    .Replace("phon", "phone")
                    .Replace("mac buc", "macbook")
                    .Replace("mac bôk", "macbook")
                    .Replace("sam sung", "samsung")
                    .Replace("xam xung", "samsung")
                    .Replace("deo", "dell")
                    .Replace("a xut", "asus")
                    .Replace("dong ho", "watch")
                    .Replace("lap top", "laptop")
                    .Replace("pe ce", "pc")
                    .Replace("pi xi", "pc")
                    .Replace("a co", "aker")
                    .Replace("ai co", "aker");

                System.Diagnostics.Debug.WriteLine($"[ĐÃ DỊCH TỪ KHÓA]: {keyword} ---> {searchKeyword}");

                // Bước 3: Đem cả từ khóa đã lột xác VÀ từ khóa gốc đi quét trong Database
                // Dùng thêm ToLower() cho NamePro để chắc chắn không phân biệt hoa thường
                var Products = dbO.Products
                    .Where(p => p.NamePro.ToLower().Contains(searchKeyword) || 
                                p.NamePro.ToLower().Contains(originalKeyword))
                    .ToList();

                // Ném kết quả ra View
                ViewBag.KeywordUsed = searchKeyword;

                return View(Products);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LỖI TÌM KIẾM]: {ex.Message}");
                ViewBag.Error = "Đã xảy ra sự cố trong quá trình tìm kiếm. Vui lòng thử lại!";
                return View(new List<Products>()); 
            }
        }

        // [HttpGet]
        // public ActionResult CatergoryPartial(String catergory)
        // {
        //         var cate = dbO.Category.Where(s => s.NameCate == catergory).FirstOrDefault();
        //         if (cate != null)
        //         {
        //             var pro = dbO.Products.Where(p => p.Category == cate.IDCate).ToList();
        //             if (pro.Count() < 1)  return View(new List<Products>()); 
        //             return View(pro);
        //         }
        //         return View(new List<Products>());       
        // }

        [HttpGet]
        public ActionResult CatergoryPartial(string catergory, decimal? minPrice, decimal? maxPrice, string sortOrder, int? rating)
        {
            var cate = dbO.Category.FirstOrDefault(s => s.NameCate == catergory);
            if (cate != null)
            {
                var proQuery = dbO.Products.Where(p => p.Category == cate.IDCate).AsQueryable();

                // 1. TÍNH TOÁN ĐIỂM ĐÁNH GIÁ
                var scoreProducts = dbO.Reviews.Join(
                    dbO.Products, rv => rv.ProductID, 
                    pro => pro.ProductID,
                    (rv, pro) => new { pro.ProductID, rv.Rating }
                ).GroupBy(x => x.ProductID)
                .Select(x => new
                {
                    ProductID = x.Key,
                    midScores = x.Average(item => item.Rating),
                    numberReviews = x.Count()
                }).ToDictionary(x => x.ProductID, x => (x.midScores, x.numberReviews));

                // 2. TÍNH TOÁN SỐ LƯỢNG ĐÃ BÁN
                var soldItems = dbO.OrderDetails
                    .Join(dbO.OrderPro, 
                        od => od.IDOrder, 
                        op => op.ID,
                        (od, op) => new { od.IDProduct, od.Quantity })
                    .GroupBy(x => x.IDProduct)
                    .Select(g => new { 
                        ProductID = g.Key, 
                        TotalSold = g.Sum(x => x.Quantity) 
                    }).Where(x => x.ProductID > 0)
                    .ToDictionary(x => x.ProductID, x => x.TotalSold);

               
                // 4. BỘ LỌC GIÁ VÀ SẮP XẾP
                if (minPrice.HasValue) proQuery = proQuery.Where(p => p.Price >= minPrice.Value);
                if (maxPrice.HasValue) proQuery = proQuery.Where(p => p.Price <= maxPrice.Value);

                switch (sortOrder)
                {
                    case "price_asc": proQuery = proQuery.OrderBy(p => p.Price); break;
                    case "price_desc": proQuery = proQuery.OrderByDescending(p => p.Price); break;
                    case "name_asc": proQuery = proQuery.OrderBy(p => p.NamePro); break;
                    case "name_desc": proQuery = proQuery.OrderByDescending(p => p.NamePro); break;
                    default: proQuery = proQuery.OrderByDescending(p => p.ProductID); break;
                }

                var proList = proQuery.ToList();

                // 5. LỌC THEO ĐÁNH GIÁ
                if (rating.HasValue)
                {
                    proList = proList.Where(p => scoreProducts.ContainsKey(p.ProductID) && scoreProducts[p.ProductID].midScores >= rating.Value).ToList();
                }

                // Vẫn giữ ViewBag cho các tham số form (giữ trạng thái khi người dùng bấm Lọc)
                ViewBag.CurrentCategory = catergory;
                ViewBag.CurrentSort = sortOrder;
                ViewBag.MinPrice = minPrice;
                ViewBag.MaxPrice = maxPrice;
                ViewBag.CurrentRating = rating;

                // ĐÓNG GÓI CHUẨN VÀO VIEWMODEL (TUYỆT ĐỐI KHÔNG DÙNG VIEWBAG ĐỂ CHỨA DICTIONARY)
                var indexpros = new indexProducts // Đảm bảo class này có sẵn trong Models
                {
                    products = proList,
                    soldQuantities = soldItems,
                    scoreProducts = scoreProducts,
                };

                return View(indexpros);
            }
            
            // Nếu không tìm thấy danh mục, trả về ViewModel rỗng
            return View(new indexProducts { products = new List<Products>() });       
        }
        [HttpGet]
        public JsonResult GetWishlist()
        {
            string? user_name = HttpContext.Session.GetString("DaDangNhap");
            using (var db = dbO)
            {
                var wishlist = db.LoveProducts
                    .Where(x => x.CustomerName == user_name)
                    .Select(x => new { x.ProductID})
                    .ToList();
                return Json(wishlist);
            }
        }
        [HttpPost]
        public ActionResult AddToWishlist(int ProductID, string ProductsName)
        {
            // Lấy userId từ session hoặc context (giả sử đã đăng nhập)
            string? user_name = HttpContext.Session.GetString("DaDangNhap");
            using (var db = dbO)
            {
                // Kiểm tra đã có chưa
                var exist = db.LoveProducts.FirstOrDefault(x => x.ProductID == ProductID && x.CustomerName == user_name);
                if (exist == null)
                {
                    //Lấy thông tin khách hàng 
                    var cus = db.Customers.FirstOrDefault(x => x.NameCus == user_name);
                    var love = new LoveProducts
                    {
                        ProductID = ProductID,
                        CustomerID = cus.IDCus,
                        CustomerName = cus.NameCus
                        // nếu có cột này
                    };
                    db.LoveProducts.Add(love);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Đã thêm vào yêu thích!" });
                }
                else
                {
                    return Json(new { success = false, message = "Sản phẩm đã có trong yêu thích!" });
                }
            }
        }
        // Xóa khỏi wishlist
        [HttpPost]
        public JsonResult RemoveFromWishlist(int ProductID)

        {
            using (var db = dbO)
            {
                var item = db.LoveProducts.FirstOrDefault(x => x.ProductID == ProductID);
               try
                {
                    if (item != null)
                    {
                        db.LoveProducts.Remove(item);
                        db.SaveChanges();
                        return Json(new { success = true, message = "Đã bỏ tim!" });
                    }
                }
                catch
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong yêu thích!" });
                }
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong yêu thích!" });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Có thể ghi log lỗi ra file ở đây nếu muốn
            return View(); // Trả về trang báo lỗi chung
        }

        // Xử lý các lỗi HTTP thông thường (404, 403...)
        [Route("/Home/ErrorCode")]
        public IActionResult ErrorCode(int statusCode)
        {
            switch (statusCode)
            {
                case 404:
                    ViewBag.ErrorMessage = "Ôi hỏng! Trang bạn tìm kiếm không tồn tại hoặc đã bị xóa.";
                    break;
                case 403:
                    ViewBag.ErrorMessage = "Bạn không có quyền truy cập vào khu vực này.";
                    break;
                default:
                    ViewBag.ErrorMessage = "Đã xảy ra lỗi hệ thống (" + statusCode + "). Vui lòng thử lại sau.";
                    break;
            }
            
            // Dùng chung 1 view báo lỗi cho đỡ tốn công thiết kế nhiều trang
            return View("Error"); 
        }
        }
}