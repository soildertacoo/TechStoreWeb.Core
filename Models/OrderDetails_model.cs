using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TechStore.Models
{
    public class OrderDetails_model
    {
        public List<OrderDetails>? OrderDetails { get; set; }
        public  OrderPro? OrderPro { get; set; }
    }
}