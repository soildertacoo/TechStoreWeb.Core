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
    }
}