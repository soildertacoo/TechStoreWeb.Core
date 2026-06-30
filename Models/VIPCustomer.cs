using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechStore.Models
{
    [Table("VIPCustomer")]
    public class VIPCustomer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] 
        public int IDCus { get; set; }

        [Required(ErrorMessage = "Tên khách hàng không được để trống")]
        [StringLength(100)]
        public string? NameCus { get; set; }

        [StringLength(50)]
        public string VipTier { get; set; } = "Thành viên";

        public bool isActived { get; set; } = false;

        public DateTime? ExpireVIPDate { get; set; }
    }
}