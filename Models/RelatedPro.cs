using System.Collections.Generic;

namespace TechStore.Models
{
    public class RelatedPro
    {
        public Products Products { get; set; }
        //public List<Products> RelatedProducts { get; set; }
        public List<Review> RelatedReviews { get; set; }
        public Review Review { get; set; }
        public Customer Customer { get; set; }
        public Dictionary<int,int> SoldItem { get; set; }

    }
}