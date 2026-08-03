using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechStore.Models 
{
    [Table("UsedPromotion")]
    public class UsedPromotion
    {
        [Required]
        public int PromotionID { get; set; }
        
        [Required]
        public int IDCus { get; set; }
        
        [Required]
        public DateTime UsedDate { get; set; }

        // Thuộc tính điều hướng
        public virtual Customer Customer { get; set; }
        public virtual Promotion Promotion { get; set; }
    }
}