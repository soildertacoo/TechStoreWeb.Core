using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Web;

namespace TechStore.Models
{
    public class Face2Factor
    {
        public string Email { get; set; }
        public string SecretKey { get; set; }
        public string QrCodeImage { get; set; }
        public string UserCode { get; set; }
        public bool IsVerified { get; set; }
        
    }
}