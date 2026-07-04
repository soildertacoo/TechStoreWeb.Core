using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TechStore.Models
{
    public enum StockStatus
    {
        OutOfStock,
        LowStock,
        InStock
    }

    public enum InventoryCostingMethod
    {
        FIFO,
        LIFO,
        WeightedAverage
    }

    public class InventoryStockSummary
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int UnitsSold { get; set; }
        public StockStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public DateTime? LastUpdated { get; set; }
        public List<InventoryMovement> RecentMovements { get; set; } = new();
        public ProfitLossResult? ProfitLossInfo { get; set; }
    }

    public class StockAdjustmentViewModel
    {
        public int InventoryID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }

        [Display(Name = "Loại điều chỉnh")]
        public InventoryMovementType AdjustmentType { get; set; } = InventoryMovementType.Import;

        [Display(Name = "Số lượng")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        [Display(Name = "Giá nhập (VND)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá nhập phải lớn hơn hoặc bằng 0")]
        public decimal? UnitPrice { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }
    }
}
