using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechStore.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    public class OTPModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdOTP { get; set; }
        public string? Email { get; set; }
        public string? NameCus { get; set; }
        public string? OtpCode { get; set; }
        public DateTime sendedTime {get;set;}
        public DateTime ExpirationTime { get; set; }
        public int typeOTP { get; set; } //0 là OTP email, 1 là OTP SMS
    }
}