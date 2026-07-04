using Microsoft.EntityFrameworkCore;
using TechStore.Models;

namespace TechStoreWeb.Core.InventoryServices
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly DBTechStoreEntities _dbTechStoreEntities;
        private readonly IInventoryCalculationService _calculationService;

        public InventoryService(
            ApplicationDbContext context,
            DBTechStoreEntities dbTechStoreEntities,
            IInventoryCalculationService calculationService)
        {
            _context = context;
            _dbTechStoreEntities = dbTechStoreEntities;
            _calculationService = calculationService;
        }

        public async Task<List<Inventory>> GetAllInventoriesAsync()
        {
            return await _dbTechStoreEntities.Inventories
                .Include(i => i.Product)
                .OrderByDescending(i => i.LastUpdated)
                .ToListAsync();
        }

        public async Task<Inventory?> GetInventoryByIdAsync(int inventoryId)
        {
            return await _dbTechStoreEntities.Inventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.InventoryID == inventoryId);
        }

        public async Task<Inventory?> GetInventoryByProductIdAsync(int productId)
        {
            return await _dbTechStoreEntities.Inventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.ProductID == productId);
        }

        public async Task<Dictionary<int, int>> GetStockByProductIdsAsync(IEnumerable<int> productIds)
        {
            var ids = productIds.Distinct().ToList();
            return await _dbTechStoreEntities.Inventories
                .Where(i => ids.Contains(i.ProductID))
                .ToDictionaryAsync(i => i.ProductID, i => i.StockQuantity);
        }

        public StockStatus GetStockStatus(int quantity)
        {
            if (quantity <= 0) return StockStatus.OutOfStock;
            if (quantity < InventoryConstants.LowStockThreshold) return StockStatus.LowStock;
            return StockStatus.InStock;
        }

        public string GetStockStatusLabel(StockStatus status) => status switch
        {
            StockStatus.OutOfStock => "Hết hàng",
            StockStatus.LowStock => "Sắp hết",
            StockStatus.InStock => "Sẵn sàng",
            _ => "Không xác định"
        };

        public async Task<InventoryStockSummary?> GetStockSummaryAsync(int productId)
        {
            // Lấy inventory (nếu có) từ DBTechStoreEntities
            var inventory = await GetInventoryByProductIdAsync(productId);

            // Lấy thông tin sản phẩm từ DBTechStoreEntities
            var product = await _dbTechStoreEntities.Products.FindAsync(productId);

            // Lấy số lượng đã bán từ DBTechStoreEntities
            var unitsSold = await _dbTechStoreEntities.OrderDetails
                .Where(od => od.IDProduct == productId)
                .SumAsync(od => od.Quantity ?? 0);

            // Lấy lịch sử nhập xuất từ ApplicationDbContext (vì bảng này mới thêm)
            var recentMovements = await _context.InventoryMovements
                .Where(m => m.ProductID == productId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(20)
                .ToListAsync();

            // Tính trạng thái tồn kho
            int currentStock = inventory?.StockQuantity ?? 0;
            var status = GetStockStatus(currentStock);

            // Tính lời lỗ
            ProfitLossResult? profitLossInfo = null;
            try
            {
                profitLossInfo = await _calculationService.CalculateProductProfitLossAsync(productId);
            }
            catch
            {
                // Ignore errors when calculating profit/loss
            }

            return new InventoryStockSummary
            {
                ProductID = productId,
                ProductName = product?.NamePro ?? inventory?.Product?.NamePro ?? string.Empty,
                CurrentStock = currentStock,
                UnitsSold = unitsSold,
                Status = status,
                StatusLabel = GetStockStatusLabel(status),
                LastUpdated = inventory?.LastUpdated,
                RecentMovements = recentMovements,
                ProfitLossInfo = profitLossInfo
            };
        }

        public async Task<bool> ProductHasInventoryAsync(int productId)
        {
            return await _dbTechStoreEntities.Inventories.AnyAsync(i => i.ProductID == productId);
        }

        public async Task<(Inventory inventory, InventoryMovement movement)> CreateInventoryAsync(
            int productId, int initialQuantity, string? note)
        {
            if (initialQuantity < 0)
                throw new InvalidOperationException("Số lượng tồn kho ban đầu không được âm.");

            if (await ProductHasInventoryAsync(productId))
                throw new InvalidOperationException("Sản phẩm này đã có bản ghi tồn kho. Vui lòng điều chỉnh thay vì tạo mới.");

            var productExists = await _dbTechStoreEntities.Products.AnyAsync(p => p.ProductID == productId);
            if (!productExists)
                throw new InvalidOperationException("Sản phẩm không tồn tại.");

            var inventory = new Inventory
            {
                ProductID = productId,
                StockQuantity = initialQuantity,
                LastUpdated = DateTime.Now,
                Note = note ?? string.Empty
            };

            _dbTechStoreEntities.Inventories.Add(inventory);
            await _dbTechStoreEntities.SaveChangesAsync();

            var movement = await RecordMovementAsync(
                productId,
                InventoryMovementType.Initial,
                initialQuantity,
                0,
                initialQuantity,
                note ?? "Khởi tạo tồn kho");

            return (inventory, movement);
        }

        public async Task<(Inventory inventory, InventoryMovement movement)> ImportStockAsync(
            int productId, int quantity, string? note)
        {
            ValidatePositiveQuantity(quantity, "nhập kho");

            var inventory = await GetOrThrowInventoryAsync(productId);
            var stockBefore = inventory.StockQuantity;
            var stockAfter = CalculateNewStock(stockBefore, quantity);

            inventory.StockQuantity = stockAfter;
            inventory.LastUpdated = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(note))
                inventory.Note = note;

            var movement = await RecordMovementAsync(
                productId,
                InventoryMovementType.Import,
                quantity,
                stockBefore,
                stockAfter,
                note ?? $"Nhập kho +{quantity}");

            await _dbTechStoreEntities.SaveChangesAsync();
            return (inventory, movement);
        }

        public async Task<(Inventory inventory, InventoryMovement movement)> ExportStockAsync(
            int productId, int quantity, string? note)
        {
            ValidatePositiveQuantity(quantity, "xuất kho");

            var inventory = await GetOrThrowInventoryAsync(productId);
            var stockBefore = inventory.StockQuantity;

            EnsureSufficientStock(stockBefore, quantity);

            var stockAfter = CalculateNewStock(stockBefore, -quantity);
            inventory.StockQuantity = stockAfter;
            inventory.LastUpdated = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(note))
                inventory.Note = note;

            var movement = await RecordMovementAsync(
                productId,
                InventoryMovementType.Export,
                -quantity,
                stockBefore,
                stockAfter,
                note ?? $"Xuất kho -{quantity}");

            await _dbTechStoreEntities.SaveChangesAsync();
            return (inventory, movement);
        }

        public async Task<(Inventory inventory, InventoryMovement movement)> SetStockAsync(
            int inventoryId, int newQuantity, string? note)
        {
            if (newQuantity < 0)
                throw new InvalidOperationException("Số lượng tồn kho không được âm.");

            var inventory = await _dbTechStoreEntities.Inventories.FindAsync(inventoryId)
                ?? throw new InvalidOperationException("Không tìm thấy bản ghi tồn kho.");

            var stockBefore = inventory.StockQuantity;
            if (stockBefore == newQuantity)
                throw new InvalidOperationException("Số lượng mới trùng với tồn kho hiện tại.");

            var delta = newQuantity - stockBefore;
            inventory.StockQuantity = newQuantity;
            inventory.LastUpdated = DateTime.Now;
            inventory.Note = note ?? inventory.Note;

            var movement = await RecordMovementAsync(
                inventory.ProductID,
                InventoryMovementType.Adjustment,
                delta,
                stockBefore,
                newQuantity,
                note ?? $"Điều chỉnh tồn kho: {stockBefore} → {newQuantity}");

            await _dbTechStoreEntities.SaveChangesAsync();
            return (inventory, movement);
        }

        public async Task<(Inventory inventory, InventoryMovement movement)> AdjustStockAsync(
            int inventoryId, InventoryMovementType adjustmentType, int quantity, string? note)
        {
            var inventory = await _context.Inventories.FindAsync(inventoryId)
                ?? throw new InvalidOperationException("Không tìm thấy bản ghi tồn kho.");

            return adjustmentType switch
            {
                InventoryMovementType.Import => await ImportStockAsync(inventory.ProductID, quantity, note),
                InventoryMovementType.Export => await ExportStockAsync(inventory.ProductID, quantity, note),
                _ => throw new InvalidOperationException("Loại điều chỉnh không hợp lệ. Chỉ hỗ trợ Nhập hoặc Xuất.")
            };
        }

        public async Task DeleteInventoryAsync(int inventoryId)
        {
            var inventory = await _dbTechStoreEntities.Inventories.FindAsync(inventoryId)
                ?? throw new InvalidOperationException("Không tìm thấy bản ghi tồn kho.");

            _dbTechStoreEntities.Inventories.Remove(inventory);
            await _dbTechStoreEntities.SaveChangesAsync();
        }

        /// <summary>
        /// Thuật toán tính tồn kho: Tồn mới = Tồn cũ + Delta.
        /// Delta dương khi nhập, âm khi xuất.
        /// </summary>
        internal static int CalculateNewStock(int currentStock, int delta)
        {
            var newStock = currentStock + delta;
            if (newStock < 0)
                throw new InvalidOperationException($"Không đủ tồn kho. Hiện có {currentStock}, yêu cầu xuất {Math.Abs(delta)}.");
            return newStock;
        }

        private async Task<Inventory> GetOrThrowInventoryAsync(int productId)
        {
            return await _dbTechStoreEntities.Inventories.FirstOrDefaultAsync(i => i.ProductID == productId)
                ?? throw new InvalidOperationException("Sản phẩm chưa có bản ghi tồn kho.");
        }

        private static void ValidatePositiveQuantity(int quantity, string action)
        {
            if (quantity <= 0)
                throw new InvalidOperationException($"Số lượng {action} phải lớn hơn 0.");
        }

        private static void EnsureSufficientStock(int currentStock, int exportQuantity)
        {
            if (currentStock < exportQuantity)
                throw new InvalidOperationException(
                    $"Không đủ tồn kho để xuất. Hiện có {currentStock}, yêu cầu xuất {exportQuantity}.");
        }

        private async Task<InventoryMovement> RecordMovementAsync(
            int productId,
            InventoryMovementType movementType,
            int quantityChange,
            int stockBefore,
            int stockAfter,
            string? note)
        {
            var movement = new InventoryMovement
            {
                ProductID = productId,
                MovementType = movementType,
                QuantityChange = quantityChange,
                StockBefore = stockBefore,
                StockAfter = stockAfter,
                CreatedAt = DateTime.Now,
                Note = note
            };

            _context.InventoryMovements.Add(movement);
            await _context.SaveChangesAsync();
            return movement;
        }
    }
}
