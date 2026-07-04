using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechStore.Models
{
    [Table("InventoryBatch")]
    public class InventoryBatch
    {
        [Key]
        public int BatchID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [ForeignKey("ProductID")]
        public virtual Products? Product { get; set; }

        [Display(Name = "Số lượng trong lô")]
        public int Quantity { get; set; }

        [Display(Name = "Số lượng còn lại")]
        public int RemainingQuantity { get; set; }

        [Display(Name = "Giá nhập/đơn giá")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitCost { get; set; }

        [Display(Name = "Ngày nhập lô")]
        public DateTime BatchDate { get; set; } = DateTime.Now;

        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }
    }
}
