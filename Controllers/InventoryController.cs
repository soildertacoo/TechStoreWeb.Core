using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;
using TechStoreWeb.Core.InventoryServices;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TechStore.Controllers
{
    public class InventoryController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly DBTechStoreEntities _dbTechStoreEntities;
        private readonly IInventoryService _inventoryService;

        public InventoryController(ApplicationDbContext context, DBTechStoreEntities dbTechStoreEntities, IInventoryService inventoryService)
        {
            _context = context;
            _dbTechStoreEntities = dbTechStoreEntities;
            _inventoryService = inventoryService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var inventories = await _inventoryService.GetAllInventoriesAsync();
                ViewBag.LowStockThreshold = InventoryConstants.LowStockThreshold;
                return View(inventories);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi khi tải danh sách tồn kho: " + ex.Message;
                return View("Error");
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _inventoryService.GetInventoryByIdAsync(id.Value);
            if (inventory == null)
            {
                return NotFound();
            }

            ViewBag.StockSummary = await _inventoryService.GetStockSummaryAsync(inventory.ProductID);
            ViewBag.LowStockThreshold = InventoryConstants.LowStockThreshold;

            if (inventory.Product != null)
            {
                ViewBag.SameCategoryProducts = await _dbTechStoreEntities.Products
                    .Where(p => p.Category == inventory.Product.Category)
                    .Include(p => p.Category1)
                    .ToListAsync();

                var productIds = ((List<Products>)ViewBag.SameCategoryProducts).Select(p => p.ProductID).ToList();
                ViewBag.StockInfo = await _inventoryService.GetStockByProductIdsAsync(productIds);
            }

            return View(inventory);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _inventoryService.GetInventoryByIdAsync(id.Value);
            if (inventory == null)
            {
                return NotFound();
            }

            ViewBag.AdjustmentModel = new StockAdjustmentViewModel
            {
                InventoryID = inventory.InventoryID,
                ProductID = inventory.ProductID,
                ProductName = inventory.Product?.NamePro ?? string.Empty,
                CurrentStock = inventory.StockQuantity
            };

            return View(inventory);
        }

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
                    await _inventoryService.SetStockAsync(inventory.InventoryID, inventory.StockQuantity, inventory.Note);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Không thể lưu thay đổi: " + ex.Message);
                }
            }

            inventory = await _inventoryService.GetInventoryByIdAsync(id) ?? inventory;
            ViewBag.AdjustmentModel = new StockAdjustmentViewModel
            {
                InventoryID = inventory.InventoryID,
                ProductID = inventory.ProductID,
                ProductName = inventory.Product?.NamePro ?? string.Empty,
                CurrentStock = inventory.StockQuantity
            };
            return View(inventory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(StockAdjustmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var inventory = await _inventoryService.GetInventoryByIdAsync(model.InventoryID);
                if (inventory == null) return NotFound();

                ViewBag.AdjustmentModel = model;
                return View("Edit", inventory);
            }

            try
            {
                await _inventoryService.AdjustStockAsync(
                    model.InventoryID,
                    model.AdjustmentType,
                    model.Quantity,
                    model.Note,
                    model.UnitPrice);

                return RedirectToAction(nameof(Details), new { id = model.InventoryID });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                var inventory = await _inventoryService.GetInventoryByIdAsync(model.InventoryID);
                if (inventory == null) return NotFound();

                ViewBag.AdjustmentModel = model;
                return View("Edit", inventory);
            }
        }

        public IActionResult Create()
        {
            try
            {
                ViewBag.ProductID = _dbTechStoreEntities.Products.ToList();
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi khi tải trang nhập kho: " + ex.Message;
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductID,StockQuantity,Note,UnitCost")] Inventory inventory)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _inventoryService.CreateInventoryAsync(
                        inventory.ProductID,
                        inventory.StockQuantity,
                        inventory.Note,
                        inventory.UnitCost);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Bị lỗi khi nhập kho: " + ex.Message);
                }
            }
            ViewBag.ProductID = _dbTechStoreEntities.Products.ToList();
            return View(inventory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _inventoryService.DeleteInventoryAsync(id);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
