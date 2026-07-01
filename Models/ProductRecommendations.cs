using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechStore.Models
{
    public class ProductRecommendation
    {
        [Key]
        public int RecommendID { get; set; }

        public int ProductID_A { get; set; } // Khách xem/mua sản phẩm A
        
        public int ProductID_B { get; set; } // Sẽ gợi ý sản phẩm B
        
        public double Confidence { get; set; } // Tỷ lệ mua chung (ví dụ 0.8 = 80%)

        [ForeignKey("ProductID_A")]
        public virtual Products ProductA { get; set; }

        [ForeignKey("ProductID_B")]
        public virtual Products ProductB { get; set; }
    }
}