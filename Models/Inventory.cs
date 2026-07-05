using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechStore.Models
{
    [Table("Inventory")]
    public class Inventory
    {
        [Key]
        public int InventoryID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [ForeignKey("ProductID")]
        public virtual Products? Product { get; set; }

        [Display(Name = "Số lượng tồn kho")]
        public int StockQuantity { get; set; }

        [Display(Name = "Ngày cập nhật cuối")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [Display(Name = "Ghi chú")]
        public string Note { get; set; }

        [NotMapped]
        [Display(Name = "Giá nhập (VND)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá nhập phải lớn hơn hoặc bằng 0")]
        public decimal? UnitCost { get; set; }
    }
}
