using System.Data;
using System.Linq;
using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using TechStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;
using DocumentFormat.OpenXml.EMMA;

namespace TechStore.Controllers
{
    public class CategoryController : Controller
    {
        private readonly DBTechStoreEntities db;
    private readonly ApplicationDbContext _context;

    // 2. Tạo hàm khởi tạo (Constructor) và yêu cầu hệ thống "tiêm" DbContext vào
    public CategoryController(DBTechStoreEntities dbContext, ApplicationDbContext appContext)
    {
        db = dbContext;
        _context = appContext;
    }

        // GET: Category
        public ActionResult Index()
        {
             var category = db.Category.Select(
                c => new
                {
                    Id = c.Id,
                    IDCate = c.IDCate,
                    NameCate = c.NameCate,
                    TotalProduct = c.Products.Count()
                }
            ).ToList();
            return View(category);
        }

        // GET: Category/Details/5
        public ActionResult Details(string? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            var category = db.Category.Where(s => s.IDCate == id).FirstOrDefault();
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // GET: Category/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Category/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind( "Id,IDCate,NameCate")] Category category)
        {
            if (!ModelState.IsValid)  return View(category);

            try
            {
                db.Category.Add(category);
                db.SaveChanges();
                
            }
            catch
            {
                ViewBag.Loi = "Bị lỗi khi tạo danh mục";
                return View(category);
            }

            return RedirectToAction("Index");
        }

        // GET: Category/Edit/5
        [HttpGet]
        public ActionResult Edit(int? Id)
        {
            if (Id == null)
            {
                return BadRequest();
            }
            var category = db.Category.Where(s => s.Id == Id).FirstOrDefault();
            if (category == null)
            {
                return BadRequest();
            }
            return View(category);
        }

        // POST: Category/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost][ValidateAntiForgeryToken]
        public ActionResult Edit([FromForm]Category category)
        {
            if (!ModelState.IsValid)    
            {
                string errorString = string.Join("\n", ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage));
                ViewBag.Loi = "Không thể cập nhật danh mục này vì bị lỗi: " + errorString;
                return View(category);
            }
            var existCate = db.Category.Where(c => c.Id == category.Id).FirstOrDefault();
            if (existCate == null)
            {
                ViewBag.Loi = "Không thể cập nhật danh mục này vì bị lỗi: " + "không tìm thấy dữ liệu phù hợp";
                return View(category);
            }
            try
            {
                existCate?.IDCate = category.IDCate ?? existCate.IDCate; 
                existCate?.NameCate = category.NameCate ?? existCate.NameCate; 
                db.Entry(existCate ?? category).State = EntityState.Modified;
                //Đổi tên category mà sản phẩm được gắn
                db.SaveChanges();
                // db.Products
                // .Where(c => c.Category == category.IDCate)
                // .ExecuteUpdate(s => s.SetProperty(
                //     pro => pro.Category, 
                //     pro => category.IDCate ?? pro.Category
                // ));
            }
            catch(Exception ex)
            {
                ViewBag.Loi = "Không thể cập nhật danh mục này" + ex.Message;
                return View(category);
            }
            return RedirectToAction("Index");

        }

        // GET: Category/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            var category = db.Category.Where(s => s.Id == id).FirstOrDefault();
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Category/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var item = db.Category.Where(s => s.Id == id).FirstOrDefault();

            if (item != null)
            {
                try
                {
                    db.Category.Remove(item);
                    db.SaveChanges();
                }
                catch(Exception ex)
                {
                    ViewBag.Loi = "Không thể xóa danh mục này vì có sản phẩm dùng danh mục này";
                    return View(item);
                }

            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
