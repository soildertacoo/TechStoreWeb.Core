using System;
using System.Collections.Generic;

namespace TechStore.Models
{
    public class InventoryCalculationResult
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        
        public InventoryCostingMethod CostingMethod { get; set; }
        
        public decimal TotalCost { get; set; }
        
        public int TotalQuantity { get; set; }
        
        public decimal AverageCostPerUnit { get; set; }
        
        public List<InventoryBatch> UsedBatches { get; set; } = new();
        
        public DateTime CalculatedAt { get; set; } = DateTime.Now;
    }

    public class ReorderPointResult
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        
        public int AverageDailyUsage { get; set; }
        public int LeadTimeDays { get; set; }
        public int SafetyStock { get; set; }
        public int ReorderPoint { get; set; }
        
        public int CurrentStock { get; set; }
        public bool ShouldReorder { get; set; }
    }

    public class EOQResult
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        
        public int AnnualDemand { get; set; }
        public decimal OrderingCost { get; set; }
        public decimal HoldingCostPerUnit { get; set; }
        
        public int EconomicOrderQuantity { get; set; }
        public int NumberOfOrdersPerYear { get; set; }
        public decimal TotalAnnualCost { get; set; }
    }

    public class ProfitLossResult
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        
        public int TotalUnitsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCostOfGoodsSold { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal ProfitMarginPercentage { get; set; }
        
        public InventoryCostingMethod CostingMethodUsed { get; set; }
        public DateTime CalculatedAt { get; set; } = DateTime.Now;
    }

    public class PeriodicProfitLossResult
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ProfitLossResult> ProductProfits { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public decimal TotalCostOfGoodsSold { get; set; }
        public decimal TotalGrossProfit { get; set; }
    }
}
