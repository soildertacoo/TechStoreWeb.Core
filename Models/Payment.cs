using System.Collections.Generic;

namespace TechStore.Models
{
    public class Payment
    {
        public OrderDetails? OrderDetails { get; set; }
        public OrderPro? Order { get; set; }
        public Customer? Customers { get; set; }
        public List<CartItem>? mycart { get; set; }
        public List<ShippingProvider>? Providers {get;set;}

    }
}