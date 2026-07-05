using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechStore.Models
{
    [Table("Banner")]
    public class Banner
    {
        [Key]
        public int BannerID { get; set; }

        [Required]
        [Display(Name = "Tiêu đề quảng cáo")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Đường dẫn hình ảnh")]
        public string ImageUrl { get; set; }

        [Display(Name = "Đường dẫn liên kết")]
        public string LinkUrl { get; set; }

        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.DateTime)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.DateTime)]
        public DateTime? EndDate { get; set; }

        public string ButtonText { get; set; } = "Mua ngay";

        public string ButtonColor { get; set; } = "btn-primary";

        public string TextColor { get; set; } = "#ffffff";

        public bool ShowButton { get; set; } = true;


        public string Description { get; set; }

        public bool IsPopup { get; set; }

        public bool IsHomeSlider { get; set; }

        public bool IsMiddleBanner { get; set; }
    }
}
