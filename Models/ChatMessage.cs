using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// SỬA DÒNG NÀY CHO KHỚP VỚI APPLICATIONDBCONTEXT
namespace TechStore.Models 
{
    public class ChatMessage
    {
        [Key]
        public int MessageId { get; set; }

        public int? IDCus { get; set; } 
        [ForeignKey("IDCus")]
        public virtual Customer? Customer { get; set; }

        public int? AdminID { get; set; } 
        [ForeignKey("AdminID")]
        public virtual AdminUsers? AdminUser { get; set; }

        [Required]
        [MaxLength(100)]
        public string RoomId { get; set; }

        [Required]
        public string Content { get; set; }

        public bool IsFromSupport { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}