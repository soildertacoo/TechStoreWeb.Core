using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Bắt buộc cho SelectList
using TechStore.Models;

namespace TechStore.Controllers
{
    public class OrderDetailsController : Controller
    {
        private readonly DBTechStoreEntities db;

        // Tiêm DbContext thông qua Constructor
        public OrderDetailsController(DBTechStoreEntities dbContext)
        {
            db = dbContext;
        }

        // GET: OrderDetails
        public ActionResult Index()
        {
            var OrderDetails = db.OrderDetails.Include(o => o.OrderPro).Include(o => o.Products);
            return View(OrderDetails.ToList());
        }

        // GET: OrderDetails/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return BadRequest(); // Chuẩn .NET Core
            
            OrderDetails? OrderDetails = db.OrderDetails.Find(id);
            if (OrderDetails == null) return NotFound(); // Chuẩn .NET Core
            
            return View(OrderDetails);
        }

        // GET: OrderDetails/Create
        public ActionResult Create()
        {
            ViewBag.IDOrder = new SelectList(db.OrderPro, "ID", "AddressDeliverry"); // Lưu ý: Thuộc tính trong DB của bạn là AddressDeliverry (2 chữ r)
            ViewBag.IDProduct = new SelectList(db.Products, "ProductID", "NamePro");
            return View();
        }

        // POST: OrderDetails/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind("ID,IDProduct,IDOrder,Quantity,UnitPrice,Discount,Subtotal,Note")] OrderDetails OrderDetails)
        {
            if (ModelState.IsValid)
            {
                db.OrderDetails.Add(OrderDetails);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.IDOrder = new SelectList(db.OrderPro, "ID", "AddressDeliverry", OrderDetails.IDOrder);
            ViewBag.IDProduct = new SelectList(db.Products, "ProductID", "NamePro", OrderDetails.IDProduct);
            return View(OrderDetails);
        }

        // GET: OrderDetails/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null) return BadRequest();
            
            OrderDetails OrderDetails = db.OrderDetails.Find(id);
            if (OrderDetails == null) return NotFound();
            
            ViewBag.IDOrder = new SelectList(db.OrderPro, "ID", "AddressDeliverry", OrderDetails.IDOrder);
            ViewBag.IDProduct = new SelectList(db.Products, "ProductID", "NamePro", OrderDetails.IDProduct);
            return View(OrderDetails);
        }

        // POST: OrderDetails/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind("ID,IDProduct,IDOrder,Quantity,UnitPrice,Discount,Subtotal,Note")] OrderDetails OrderDetails)
        {
            if (ModelState.IsValid)
            {
                db.Entry(OrderDetails).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.IDOrder = new SelectList(db.OrderPro, "ID", "AddressDeliverry", OrderDetails.IDOrder);
            ViewBag.IDProduct = new SelectList(db.Products, "ProductID", "NamePro", OrderDetails.IDProduct);
            return View(OrderDetails);
        }

        // GET: OrderDetails/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null) return BadRequest();
            
            OrderDetails?      OrderDetails = db.OrderDetails.Find(id);
            if (OrderDetails == null) return NotFound();
            
            return View(OrderDetails);
        }

        // POST: OrderDetails/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            OrderDetails OrderDetails = db.OrderDetails.Find(id);
            if (OrderDetails != null)
            {
                db.OrderDetails.Remove(OrderDetails);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}