using System;
using System.ComponentModel.DataAnnotations;

namespace TechStore.Models
{
   

    public class SupportRequest
    {
        [Key]
        public string IdRequest { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string OrderNumber { get; set; }
        public  String PurchaseDate { get; set; }
        public string ProductsName { get; set; }
        public string RequestType { get; set; }
        public string Description { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.Now;
        public string? Status {get ;set;}
        public string ? message2Buyer {get ;set;}
    }
}   