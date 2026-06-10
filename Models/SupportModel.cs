using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TechStore.Models
{
    public class SupportModel
    {
        public OrderDetails_model OrderDetails { get; set; }
        public SupportRequest SupportRequest { get; set; } 
    }
}