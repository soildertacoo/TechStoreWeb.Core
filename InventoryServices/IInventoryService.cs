using TechStore.Models;

namespace TechStoreWeb.Core.InventoryServices
{
    public static class InventoryConstants
    {
        public const int LowStockThreshold = 10;
    }

    public interface IInventoryService
    {
        Task<List<Inventory>> GetAllInventoriesAsync();
        Task<Inventory?> GetInventoryByIdAsync(int inventoryId);
        Task<Inventory?> GetInventoryByProductIdAsync(int productId);
        Task<InventoryStockSummary?> GetStockSummaryAsync(int productId);
        Task<Dictionary<int, int>> GetStockByProductIdsAsync(IEnumerable<int> productIds);

        StockStatus GetStockStatus(int quantity);
        string GetStockStatusLabel(StockStatus status);

        Task<(Inventory inventory, InventoryMovement movement)> CreateInventoryAsync(
            int productId, int initialQuantity, string? note);

        Task<(Inventory inventory, InventoryMovement movement)> ImportStockAsync(
            int productId, int quantity, string? note);

        Task<(Inventory inventory, InventoryMovement movement)> ExportStockAsync(
            int productId, int quantity, string? note);

        Task<(Inventory inventory, InventoryMovement movement)> SetStockAsync(
            int inventoryId, int newQuantity, string? note);

        Task<(Inventory inventory, InventoryMovement movement)> AdjustStockAsync(
            int inventoryId, InventoryMovementType adjustmentType, int quantity, string? note);

        Task DeleteInventoryAsync(int inventoryId);
        Task<bool> ProductHasInventoryAsync(int productId);
    }
}
