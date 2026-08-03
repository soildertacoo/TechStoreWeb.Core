using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;
namespace TechStore.Models
{
    public class CartItem
    {
        
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IDCart {get;set;}
        public int ProductID { get; set; }
        public string? NamePro { get; set; }
        public string? ImagePro { get; set; }
        public decimal Price { get; set; }
        public int Number { get; set; }
        public string? Category {get;set;}

        //Tính FinalPrice = Price * Number
        public decimal FinalPrice()
        {
            return Number * Price;
        }
        public string? userLogged{get;set;}
        public CartItem()
        {
            this.Number = 1;
        }
        
    }
}