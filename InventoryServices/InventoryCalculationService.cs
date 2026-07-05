using Microsoft.EntityFrameworkCore;
using TechStore.Models;

namespace TechStoreWeb.Core.InventoryServices
{
    public class InventoryCalculationService : IInventoryCalculationService
    {
        private readonly ApplicationDbContext _context;
        private readonly DBTechStoreEntities _dbTechStoreEntities;

        public InventoryCalculationService(ApplicationDbContext context, DBTechStoreEntities dbTechStoreEntities)
        {
            _context = context;
            _dbTechStoreEntities = dbTechStoreEntities;
        }

        public async Task<List<InventoryBatch>> GetInventoryBatchesAsync(int productId)
        {
            return await _context.InventoryBatches
                .Where(b => b.ProductID == productId && b.RemainingQuantity > 0)
                .OrderBy(b => b.BatchDate)
                .ToListAsync();
        }

        public async Task<InventoryBatch> AddInventoryBatchAsync(
            int productId, 
            int quantity, 
            decimal unitCost, 
            string? note)
        {
            var productExists = await _dbTechStoreEntities.Products.AnyAsync(p => p.ProductID == productId);
            if (!productExists)
                throw new InvalidOperationException("Sản phẩm không tồn tại.");

            var batch = new InventoryBatch
            {
                ProductID = productId,
                Quantity = quantity,
                RemainingQuantity = quantity,
                UnitCost = unitCost,
                BatchDate = DateTime.Now,
                Note = note
            };

            _context.InventoryBatches.Add(batch);
            await _context.SaveChangesAsync();
            return batch;
        }

        public async Task<InventoryCalculationResult> CalculateInventoryValueAsync(
            int productId, 
            InventoryCostingMethod method = InventoryCostingMethod.FIFO)
        {
            var product = await _dbTechStoreEntities.Products.FindAsync(productId);
            if (product == null)
                throw new InvalidOperationException("Sản phẩm không tồn tại.");

            var batches = await GetInventoryBatchesAsync(productId);
            var result = new InventoryCalculationResult
            {
                ProductID = productId,
                ProductName = product.NamePro ?? string.Empty,
                CostingMethod = method
            };

            if (method == InventoryCostingMethod.WeightedAverage)
            {
                return CalculateWeightedAverage(batches, result);
            }

            return method == InventoryCostingMethod.FIFO 
                ? CalculateFIFO(batches, result) 
                : CalculateLIFO(batches, result);
        }

        public async Task<InventoryCalculationResult> CalculateCostOfGoodsSoldAsync(
            int productId, 
            int quantitySold, 
            InventoryCostingMethod method = InventoryCostingMethod.FIFO)
        {
            var product = await _dbTechStoreEntities.Products.FindAsync(productId);
            if (product == null)
                throw new InvalidOperationException("Sản phẩm không tồn tại.");

            var batches = await GetInventoryBatchesAsync(productId);
            var result = new InventoryCalculationResult
            {
                ProductID = productId,
                ProductName = product.NamePro ?? string.Empty,
                CostingMethod = method
            };

            int remainingToSell = quantitySold;
            decimal totalCost = 0;

            if (method == InventoryCostingMethod.FIFO)
            {
                foreach (var batch in batches.OrderBy(b => b.BatchDate))
                {
                    if (remainingToSell <= 0) break;

                    int takeFromBatch = Math.Min(batch.RemainingQuantity, remainingToSell);
                    totalCost += takeFromBatch * batch.UnitCost;
                    result.TotalQuantity += takeFromBatch;
                    result.UsedBatches.Add(batch);
                    remainingToSell -= takeFromBatch;
                }
            }
            else if (method == InventoryCostingMethod.LIFO)
            {
                foreach (var batch in batches.OrderByDescending(b => b.BatchDate))
                {
                    if (remainingToSell <= 0) break;

                    int takeFromBatch = Math.Min(batch.RemainingQuantity, remainingToSell);
                    totalCost += takeFromBatch * batch.UnitCost;
                    result.TotalQuantity += takeFromBatch;
                    result.UsedBatches.Add(batch);
                    remainingToSell -= takeFromBatch;
                }
            }
            else
            {
                var avgResult = CalculateWeightedAverage(batches, result);
                totalCost = quantitySold * avgResult.AverageCostPerUnit;
                result.TotalQuantity = quantitySold;
            }

            if (remainingToSell > 0)
                throw new InvalidOperationException($"Không đủ hàng tồn kho. Cần {quantitySold}, chỉ có {result.TotalQuantity}.");

            result.TotalCost = totalCost;
            result.AverageCostPerUnit = result.TotalQuantity > 0 ? totalCost / result.TotalQuantity : 0;
            return result;
        }

        public async Task<ReorderPointResult> CalculateReorderPointAsync(
            int productId, 
            int averageDailyUsage, 
            int leadTimeDays, 
            int safetyStock = 0)
        {
            var product = await _dbTechStoreEntities.Products.FindAsync(productId);
            var inventory = await _dbTechStoreEntities.Inventories
                .FirstOrDefaultAsync(i => i.ProductID == productId);

            if (product == null)
                throw new InvalidOperationException("Sản phẩm không tồn tại.");

            int reorderPoint = (averageDailyUsage * leadTimeDays) + safetyStock;
            int currentStock = inventory?.StockQuantity ?? 0;

            return new ReorderPointResult
            {
                ProductID = productId,
                ProductName = product.NamePro ?? string.Empty,
                AverageDailyUsage = averageDailyUsage,
                LeadTimeDays = leadTimeDays,
                SafetyStock = safetyStock,
                ReorderPoint = reorderPoint,
                CurrentStock = currentStock,
                ShouldReorder = currentStock <= reorderPoint
            };
        }

        public async Task<EOQResult> CalculateEOQAsync(
            int productId, 
            int annualDemand, 
            decimal orderingCost, 
            decimal holdingCostPerUnit)
        {
            var product = await _dbTechStoreEntities.Products.FindAsync(productId);
            if (product == null)
                throw new InvalidOperationException("Sản phẩm không tồn tại.");

            double eoq = Math.Sqrt((2 * annualDemand * (double)orderingCost) / (double)holdingCostPerUnit);
            int economicOrderQuantity = (int)Math.Round(eoq);
            int numberOfOrders = annualDemand > 0 ? (int)Math.Ceiling((double)annualDemand / economicOrderQuantity) : 0;
            decimal totalAnnualCost = (annualDemand / (decimal)economicOrderQuantity) * orderingCost + 
                                     (economicOrderQuantity / 2m) * holdingCostPerUnit;

            return new EOQResult
            {
                ProductID = productId,
                ProductName = product.NamePro ?? string.Empty,
                AnnualDemand = annualDemand,
                OrderingCost = orderingCost,
                HoldingCostPerUnit = holdingCostPerUnit,
                EconomicOrderQuantity = economicOrderQuantity,
                NumberOfOrdersPerYear = numberOfOrders,
                TotalAnnualCost = totalAnnualCost
            };
        }

        public Task<int> CalculateSafetyStockAsync(
            int averageDailyUsage, 
            int leadTimeDays, 
            int serviceLevelFactor = 2)
        {
            int safetyStock = serviceLevelFactor * averageDailyUsage * leadTimeDays;
            return Task.FromResult(safetyStock);
        }

        private static InventoryCalculationResult CalculateFIFO(
            List<InventoryBatch> batches, 
            InventoryCalculationResult result)
        {
            decimal totalCost = 0;
            int totalQuantity = 0;

            foreach (var batch in batches.OrderBy(b => b.BatchDate))
            {
                totalCost += batch.RemainingQuantity * batch.UnitCost;
                totalQuantity += batch.RemainingQuantity;
                result.UsedBatches.Add(batch);
            }

            result.TotalCost = totalCost;
            result.TotalQuantity = totalQuantity;
            result.AverageCostPerUnit = totalQuantity > 0 ? totalCost / totalQuantity : 0;
            return result;
        }

        private static InventoryCalculationResult CalculateLIFO(
            List<InventoryBatch> batches, 
            InventoryCalculationResult result)
        {
            decimal totalCost = 0;
            int totalQuantity = 0;

            foreach (var batch in batches.OrderByDescending(b => b.BatchDate))
            {
                totalCost += batch.RemainingQuantity * batch.UnitCost;
                totalQuantity += batch.RemainingQuantity;
                result.UsedBatches.Add(batch);
            }

            result.TotalCost = totalCost;
            result.TotalQuantity = totalQuantity;
            result.AverageCostPerUnit = totalQuantity > 0 ? totalCost / totalQuantity : 0;
            return result;
        }

        private static InventoryCalculationResult CalculateWeightedAverage(
            List<InventoryBatch> batches, 
            InventoryCalculationResult result)
        {
            decimal totalCost = 0;
            int totalQuantity = 0;

            foreach (var batch in batches)
            {
                totalCost += batch.RemainingQuantity * batch.UnitCost;
                totalQuantity += batch.RemainingQuantity;
                result.UsedBatches.Add(batch);
            }

            result.TotalCost = totalCost;
            result.TotalQuantity = totalQuantity;
            result.AverageCostPerUnit = totalQuantity > 0 ? totalCost / totalQuantity : 0;
            return result;
        }

        public async Task<ProfitLossResult> CalculateProductProfitLossAsync(
            int productId, 
            InventoryCostingMethod method = InventoryCostingMethod.FIFO)
        {
            var product = await _dbTechStoreEntities.Products.FindAsync(productId);
            if (product == null)
                throw new InvalidOperationException("Sản phẩm không tồn tại.");

            var orderDetails = await _dbTechStoreEntities.OrderDetails
                .Where(od => od.IDProduct == productId)
                .ToListAsync();

            int totalUnitsSold = orderDetails.Sum(od => od.Quantity ?? 0);
            decimal totalRevenue = orderDetails.Sum(od => (decimal?)(od.Subtotal ?? 0) ?? 0);

            decimal totalCostOfGoodsSold = 0;
            if (totalUnitsSold > 0)
            {
                try
                {
                    var cogsResult = await CalculateCostOfGoodsSoldAsync(productId, totalUnitsSold, method);
                    totalCostOfGoodsSold = cogsResult.TotalCost;
                }
                catch
                {
                    var inventoryValue = await CalculateInventoryValueAsync(productId, method);
                    decimal avgCost = inventoryValue.AverageCostPerUnit;
                    totalCostOfGoodsSold = totalUnitsSold * avgCost;
                }
            }

            decimal grossProfit = totalRevenue - totalCostOfGoodsSold;
            decimal profitMargin = totalRevenue > 0 ? (grossProfit / totalRevenue) * 100 : 0;

            return new ProfitLossResult
            {
                ProductID = productId,
                ProductName = product.NamePro ?? string.Empty,
                TotalUnitsSold = totalUnitsSold,
                TotalRevenue = totalRevenue,
                TotalCostOfGoodsSold = totalCostOfGoodsSold,
                GrossProfit = grossProfit,
                ProfitMarginPercentage = profitMargin,
                CostingMethodUsed = method
            };
        }

        public async Task<ProfitLossResult> CalculateProductProfitLossByDateRangeAsync(
            int productId, 
            DateTime startDate, 
            DateTime endDate, 
            InventoryCostingMethod method = InventoryCostingMethod.FIFO)
        {
            var product = await _dbTechStoreEntities.Products.FindAsync(productId);
            if (product == null)
                throw new InvalidOperationException("Sản phẩm không tồn tại.");

            var orderDetails = await _dbTechStoreEntities.OrderDetails
                .Include(od => od.OrderPro)
                .Where(od => od.IDProduct == productId 
                    && od.OrderPro != null 
                    && od.OrderPro.DateOrder >= startDate 
                    && od.OrderPro.DateOrder <= endDate)
                .ToListAsync();

            int totalUnitsSold = orderDetails.Sum(od => od.Quantity ?? 0);
            decimal totalRevenue = orderDetails.Sum(od => (decimal?)(od.Subtotal ?? 0) ?? 0);

            decimal totalCostOfGoodsSold = 0;
            if (totalUnitsSold > 0)
            {
                try
                {
                    var cogsResult = await CalculateCostOfGoodsSoldAsync(productId, totalUnitsSold, method);
                    totalCostOfGoodsSold = cogsResult.TotalCost;
                }
                catch
                {
                    var batches = await GetInventoryBatchesAsync(productId);
                    if (batches.Any())
                    {
                        var avgCost = batches.Average(b => b.UnitCost);
                        totalCostOfGoodsSold = totalUnitsSold * avgCost;
                    }
                    else
                    {
                        var inventoryValue = await CalculateInventoryValueAsync(productId, method);
                        decimal avgCost = inventoryValue.AverageCostPerUnit;
                        totalCostOfGoodsSold = totalUnitsSold * avgCost;
                    }
                }
            }

            decimal grossProfit = totalRevenue - totalCostOfGoodsSold;
            decimal profitMargin = totalRevenue > 0 ? (grossProfit / totalRevenue) * 100 : 0;

            return new ProfitLossResult
            {
                ProductID = productId,
                ProductName = product.NamePro ?? string.Empty,
                TotalUnitsSold = totalUnitsSold,
                TotalRevenue = totalRevenue,
                TotalCostOfGoodsSold = totalCostOfGoodsSold,
                GrossProfit = grossProfit,
                ProfitMarginPercentage = profitMargin,
                CostingMethodUsed = method
            };
        }

        public async Task<PeriodicProfitLossResult> CalculatePeriodicProfitLossAsync(
            DateTime startDate, 
            DateTime endDate, 
            InventoryCostingMethod method = InventoryCostingMethod.FIFO)
        {
            var productIds = await _dbTechStoreEntities.OrderDetails
                .Include(od => od.OrderPro)
                .Where(od => od.OrderPro != null 
                    && od.OrderPro.DateOrder >= startDate 
                    && od.OrderPro.DateOrder <= endDate)
                .Select(od => od.IDProduct)
                .Distinct()
                .ToListAsync();

            var result = new PeriodicProfitLossResult
            {
                StartDate = startDate,
                EndDate = endDate
            };

            foreach (var productId in productIds)
            {
                try
                {
                    var productProfit = await CalculateProductProfitLossByDateRangeAsync(
                        productId, startDate, endDate, method);
                    result.ProductProfits.Add(productProfit);
                    result.TotalRevenue += productProfit.TotalRevenue;
                    result.TotalCostOfGoodsSold += productProfit.TotalCostOfGoodsSold;
                }
                catch
                {
                    continue;
                }
            }

            result.TotalGrossProfit = result.TotalRevenue - result.TotalCostOfGoodsSold;
            return result;
        }
    }
}
