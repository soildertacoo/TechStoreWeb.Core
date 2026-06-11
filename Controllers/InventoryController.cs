using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TechStore.Controllers
{
    public class InventoryController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Inventory
        public async Task<IActionResult> Index()
        {
            try
            {
                var inventories = await _context.Inventories
                    .Include(i => i.Product)
                    .ToListAsync();
                return View(inventories);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi khi tải danh sách tồn kho: " + ex.Message;
                return View("Error");
            }
        }

        // GET: Inventory/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(m => m.InventoryID == id);

            if (inventory == null)
            {
                return NotFound();
            }

            // Lấy danh sách sản phẩm cùng loại
            if (inventory.Product != null)
            {
                ViewBag.SameCategoryProducts = await _context.Products
                    .Where(p => p.Category == inventory.Product.Category)
                    .Include(p => p.Category1)
                    .ToListAsync();
                
                // Lấy thông tin tồn kho của các sản phẩm này
                var productIds = ((List<Products>)ViewBag.SameCategoryProducts).Select(p => p.ProductID).ToList();
                ViewBag.StockInfo = await _context.Inventories
                    .Where(i => productIds.Contains(i.ProductID))
                    .ToDictionaryAsync(i => i.ProductID, i => i.StockQuantity);
            }

            return View(inventory);
        }

        // GET: Inventory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(m => m.InventoryID == id);

            if (inventory == null)
            {
                // Nếu chưa có bản ghi tồn kho cho sản phẩm này, có thể tạo mới
                // Nhưng ở đây ta giả định là quản lý theo ID tồn kho
                return NotFound();
            }
            return View(inventory);
        }

        // POST: Inventory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("InventoryID,ProductID,StockQuantity,Note")] Inventory inventory)
        {
            if (id != inventory.InventoryID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    inventory.LastUpdated = DateTime.Now;
                    _context.Update(inventory);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Không thể lưu thay đổi: " + ex.Message);
                }
            }
            return View(inventory);
        }

        // GET: Inventory/Create
        public IActionResult Create()
        {
            try
            {
                ViewBag.ProductID = _context.Products.ToList();
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi khi tải trang nhập kho: " + ex.Message;
                return View("Error");
            }
        }

        // POST: Inventory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductID,StockQuantity,Note")] Inventory inventory)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    inventory.LastUpdated = DateTime.Now;
                    _context.Add(inventory);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Bị lỗi khi nhập kho: " + ex.Message);
                }
            }
            ViewBag.ProductID = _context.Products.ToList();
            return View(inventory);
        }

        // POST: Inventory/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var inventory = await _context.Inventories.FindAsync(id);
            if (inventory == null)
            {
                return NotFound();
            }

            _context.Inventories.Remove(inventory);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool InventoryExists(int id)
        {
            return _context.Inventories.Any(e => e.InventoryID == id);
        }
    }
}
