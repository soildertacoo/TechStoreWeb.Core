using TechStore.Models;

namespace TechStoreWeb.Core.InventoryServices
{
    public interface IInventoryCalculationService
    {
        Task<InventoryCalculationResult> CalculateInventoryValueAsync(
            int productId, 
            InventoryCostingMethod method = InventoryCostingMethod.FIFO);

        Task<InventoryCalculationResult> CalculateCostOfGoodsSoldAsync(
            int productId, 
            int quantitySold, 
            InventoryCostingMethod method = InventoryCostingMethod.FIFO);

        Task<ReorderPointResult> CalculateReorderPointAsync(
            int productId, 
            int averageDailyUsage, 
            int leadTimeDays, 
            int safetyStock = 0);

        Task<EOQResult> CalculateEOQAsync(
            int productId, 
            int annualDemand, 
            decimal orderingCost, 
            decimal holdingCostPerUnit);

        Task<int> CalculateSafetyStockAsync(
            int averageDailyUsage, 
            int leadTimeDays, 
            int serviceLevelFactor = 2);

        Task<List<InventoryBatch>> GetInventoryBatchesAsync(int productId);

        Task<InventoryBatch> AddInventoryBatchAsync(
            int productId, 
            int quantity, 
            decimal unitCost, 
            string? note);

        Task<ProfitLossResult> CalculateProductProfitLossAsync(
            int productId, 
            InventoryCostingMethod method = InventoryCostingMethod.FIFO);

        Task<ProfitLossResult> CalculateProductProfitLossByDateRangeAsync(
            int productId, 
            DateTime startDate, 
            DateTime endDate, 
            InventoryCostingMethod method = InventoryCostingMethod.FIFO);

        Task<PeriodicProfitLossResult> CalculatePeriodicProfitLossAsync(
            DateTime startDate, 
            DateTime endDate, 
            InventoryCostingMethod method = InventoryCostingMethod.FIFO);
    }
}
