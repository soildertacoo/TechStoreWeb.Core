using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TechStore.Models
{
    public class indexProducts
    {
        public List<Products>? products {get;set;} = new List<Products>();
        public Dictionary<int,int?>? soldQuantities {get;set;} = new Dictionary<int, int?>();
        public Dictionary<int, (decimal midScores, int numberReviews)>? scoreProducts {get;set;} = new Dictionary<int, (decimal midScores, int numberReviews)>();
        public List<Banner> banners { get; set; }
    }
}