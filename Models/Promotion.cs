using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechStore.Models
{
    [Table("Promotion")]
    public class Promotion
    {
        [Key]
        public int PromotionID { get; set; }

        [Required]
        [Display(Name = "Tên chương trình")]
        public string Name { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Display(Name = "Phần trăm giảm giá")]
        [Range(0, 100)]
        public decimal DiscountPercentage { get; set; }

        public decimal? DiscountAmount { get; set; }

        public decimal? MinimumOrder { get; set; }

        public decimal? MaximumDiscount { get; set; }

        public int Priority { get; set; }

        [Display(Name = "Ngày bắt đầu")]
        public DateTime StartDate { get; set; }

        [Display(Name = "Ngày kết thúc")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; } = true;

        // Có thể mở rộng để liên kết với các sản phẩm cụ thể nếu cần
        // public virtual ICollection<Products> Products { get; set; }
    }
}
