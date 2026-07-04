using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TechStore.Models
{
    public class StatisticModel
    {
        public class RevenueStatistics
        {
            public List<string> Labels { get; set; }
            public List<decimal> Data { get; set; }
        }

        public class ReviewStatistics
        {
            public List<string> Labels { get; set; }
            public List<int> Data { get; set; }
            public List<decimal> AverageRatings { get; set; }
        }

        public class BestSellingStatistics
        {
            public List<string> Labels { get; set; }
            public List<int> Data { get; set; }
        }

        public class SummaryStatistics
        {
            public decimal TotalRevenue { get; set; }
            public int TotalOrders { get; set; }
            public decimal AverageRating { get; set; }
            public int TotalCustomers { get; set; }
        }

        public class ProfitLossStatistics
        {
            public List<string> Labels { get; set; }
            public List<decimal> RevenueData { get; set; }
            public List<decimal> CostData { get; set; }
            public List<decimal> ProfitData { get; set; }
        }
    }
}