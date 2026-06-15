using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Bắt buộc phải có cho SelectList
using TechStore.Models;

namespace TechStore.Controllers
{
    public class ReviewsController : Controller
    {
        // 1. Chỉ khai báo biến, KHÔNG dùng "new"
        private readonly DBTechStoreEntities db;

        // 2. Tiêm DbContext qua Constructor
        public ReviewsController(DBTechStoreEntities dbContext)
        {
            db = dbContext;
        }

        // GET: Reviews
        public ActionResult Index()
        {
            var reviews = from review in db.Reviews
                          join customer in db.Customers on review.CustomerID equals customer.IDCus
                          join Products in db.Products on review.ProductID equals Products.ProductID
                          select new ReviewViewModel
                          {
                              ReviewID = review.ReviewID,
                              ProductID = Products.ProductID,
                              ProductsName = Products.NamePro,
                              CustomerID = customer.IDCus,
                              CustomerName = customer.NameCus,
                              Rating = review.Rating,
                              ReviewContent = review.ReviewContent,
                              ReviewDate = review.ReviewDate,
                              IsHidden = review.IsHidden ?? false // Default to false if IsHidden is null
                          };

            return View(reviews.ToList());
        }

        // GET: Reviews/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return BadRequest(); // Chuẩn .NET Core
            
            Review review = db.Reviews.Find(id);
            if (review == null) return NotFound(); // Chuẩn .NET Core
            
            return View(review);
        }

        // GET: Reviews/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null) return BadRequest();
            
            Review review = db.Reviews.Find(id);
            if (review == null) return NotFound();
            
            ViewBag.CustomerID = new SelectList(db.Customers, "IDCus", "NameCus", review.CustomerID);
            ViewBag.ProductID = new SelectList(db.Products, "ProductID", "NamePro", review.ProductID);
            return View(review);
        }

        // POST: Reviews/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind("ReviewID,ProductID,CustomerID,Rating,ReviewContent,ReviewDate")] Review review)
        {
            if (ModelState.IsValid)
            {
                db.Entry(review).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CustomerID = new SelectList(db.Customers, "IDCus", "NameCus", review.CustomerID);
            ViewBag.ProductID = new SelectList(db.Products, "ProductID", "NamePro", review.ProductID);
            return View(review);
        }

        // GET: Reviews/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null) return BadRequest();
            
            Review review = db.Reviews.Find(id);
            if (review == null) return NotFound();
            
            return View(review);
        }

        // POST: Reviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult HiddenConfirmed(int id)
        {
            if (ModelState.IsValid)
            {
                var review = db.Reviews.Find(id);
                if (review != null)
                {
                    // xem ẩn thay vì xóa
                    review.IsHidden = true; 
                    db.Entry(review).State = EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    return NotFound();
                }
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult ShowReview(int id)
        {
            if (ModelState.IsValid)
            {
                var review = db.Reviews.Find(id);
                if (review != null)
                {
                    // xem ẩn thay vì xóa
                    review.IsHidden = false;
                    db.Entry(review).State = EntityState.Modified;
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false });
                }
            }
            return Json(new { success = false }); // Đã bỏ phần Redirect không cần thiết phía sau
        }
        // Đã xóa hàm Dispose()
        // =========================================================
/// =========================================================
        // =========================================================
        // PHẦN API DÀNH CHO KHÁCH HÀNG (SỬA / XÓA REVIEW CỦA CHÍNH HỌ)
        // =========================================================

        [HttpPost]
        public ActionResult EditReview([FromBody] EditReviewRequest req)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) 
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            string currentUserName = User.Identity.Name;
            var customer = db.Customers.FirstOrDefault(c => c.NameCus == currentUserName);
            if (customer == null) 
                return Json(new { success = false, message = "Lỗi xác thực người dùng!" });

            var review = db.Reviews.FirstOrDefault(r => r.ReviewID == req.reviewId && r.CustomerID == customer.IDCus);
            if (review == null) 
                return Json(new { success = false, message = "Không tìm thấy hoặc bạn không có quyền sửa đánh giá này!" });

            review.Rating = req.score;
            review.ReviewContent = req.content;
            review.ReviewDate = System.DateTime.Now; // Cập nhật lại ngày giờ sửa

            db.Entry(review).State = EntityState.Modified;
            db.SaveChanges();
            return Json(new { success = true, message = "Cập nhật đánh giá thành công!" });
        }

        [HttpPost]
        public ActionResult DeleteCustomerReview([FromBody] DeleteReviewRequest req)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) 
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            string currentUserName = User.Identity.Name;
            var customer = db.Customers.FirstOrDefault(c => c.NameCus == currentUserName);
            if (customer == null) 
                return Json(new { success = false, message = "Lỗi xác thực người dùng!" });

            var review = db.Reviews.FirstOrDefault(r => r.ReviewID == req.reviewId && r.CustomerID == customer.IDCus);
            if (review == null) 
                return Json(new { success = false, message = "Không tìm thấy hoặc bạn không có quyền xóa đánh giá này!" });

            db.Reviews.Remove(review);
            db.SaveChanges();
            return Json(new { success = true, message = "Đã xóa đánh giá thành công!" });
        }

        // =========================================================
        // 2 CLASS PHỤ TRỢ (ĐỪNG XÓA MẤT 2 BẠN NÀY NHÉ)
        // =========================================================
        public class EditReviewRequest 
        { 
            public int reviewId { get; set; } 
            public int score { get; set; } 
            public string content { get; set; } 
            public int proID { get; set; } 
        }

        public class DeleteReviewRequest
        {
            public int reviewId { get; set; }
        }
         // Đã xóa hàm Dispose()
       

       

    
    }
}
    