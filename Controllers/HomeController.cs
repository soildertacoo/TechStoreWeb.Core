using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using TechStore.Models;

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
                // Lấy tất cả sản phẩm
                var proList = db.Products.ToList();
                // Tính toán số lượng đã bán cho mỗi sản phẩm
                var soldQuantities = new Dictionary<int, int>();
                //Lấy điểm của sản phẩm và số lượng đánh giá 
                // Query để lấy số lượng đã bán từ OrderDetailss
                if(proList.Count > 0)
                {
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
                        scoreProducts = scoreProducts
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
        [HttpPost]
        public ActionResult Search(string keyword)
        {
            try
            {
                var Products = dbO.Products.Where(p => p.NamePro.Contains(keyword)).ToList();
                return View(Products);
            }
            catch
            {
                ViewBag.Error = "Không tìm thấy sản phẩm";
                return new NotFoundResult();
            }
        }

        [HttpGet]
        public ActionResult CatergoryPartial(String catergory)
        {
                var cate = dbO.Category.Where(s => s.NameCate == catergory).FirstOrDefault();
                if (cate != null)
                {
                    var pro = dbO.Products.Where(p => p.Category == cate.IDCate).ToList();
                    if (pro.Count() < 1)  return View(new List<Products>()); 
                    return View(pro);
                }
                return View(new List<Products>());       
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