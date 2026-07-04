using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechStore.Models
{
    [Table("InventoryMovement")]
    public class InventoryMovement
    {
        [Key]
        public int MovementID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [ForeignKey("ProductID")]
        public virtual Products? Product { get; set; }

        public InventoryMovementType MovementType { get; set; }

        /// <summary>Số lượng thay đổi (+ nhập, - xuất).</summary>
        public int QuantityChange { get; set; }

        /// <summary>Tồn kho trước khi thay đổi.</summary>
        public int StockBefore { get; set; }

        /// <summary>Tồn kho sau khi thay đổi.</summary>
        public int StockAfter { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }
    }
}
