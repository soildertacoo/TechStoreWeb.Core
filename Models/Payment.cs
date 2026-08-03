using System.Collections.Generic;

namespace TechStore.Models
{
    public class Payment
    {
        public OrderDetails? OrderDetails { get; set; }
        public OrderPro? Order { get; set; }
        public Customer? Customers { get; set; }
        public List<CartItem>? mycart { get; set; }
        public List<ShippingProviders>? Providers {get;set;}
        public List<ShippingMethod>? ShippingMethods {get;set;}
        public string? Category {get;set;}

    }
     public class ShippingMethod {
            public string? MethodName ;
            public Nullable<decimal> ShippingCost;
        }
}