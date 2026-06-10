using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TechStore.Models
{
    public class HoaDonModel
    {
        [Key]
        public int ID { get; set; }
        public string TrackingNumber { get; set; }
        public string CustomerName { get; set; }
        public string AddressDeliverry { get; set; }
        public DateTime ? DateOrder { get; set; }
        public DateTime ? DeliveryDate { get; set; }
        public string Status { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? ShippingCost { get; set; }
        public List<HoaDonProductsModel> Products { get; set; }
    }

    public class HoaDonProductsModel
    {
        public string ProductsName { get; set; }
        public string ImagePro { get; set; }
        public double UnitPrice { get; set; }
        public int Quantity { get; set; }
        public double TotalPrice => UnitPrice * Quantity;
    }
}