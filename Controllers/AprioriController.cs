using Microsoft.AspNetCore.Mvc;
using TechStore.Models;
using System.Linq;
using System.Collections.Generic;
using System;

namespace TechStore.Controllers
{
    public class AprioriController : Controller
    {
       private readonly ApplicationDbContext _context; 

       public AprioriController(ApplicationDbContext context) => _context = context;

        [HttpPost]
        public IActionResult RunAlgorithm()
        {
            var transactions = _context.OrderDetails
                .GroupBy(od => od.IDOrder)
                .Where(g => g.Count() > 1)
                .Select(g => g.Select(od => od.IDProduct).Distinct().ToList())
                .ToList();

            if (!transactions.Any()) return Json(new { success = false, message = "Không đủ dữ liệu đơn hàng" });

            int totalTransactions = transactions.Count;
            var itemCounts = new Dictionary<int, int>();
            var pairCounts = new Dictionary<string, int>();

            foreach (var transaction in transactions)
            {
                for (int i = 0; i < transaction.Count; i++)
                {
                    int itemA = transaction[i];
                    if (!itemCounts.ContainsKey(itemA)) itemCounts[itemA] = 0;
                    itemCounts[itemA]++;

                    for (int j = i + 1; j < transaction.Count; j++)
                    {
                        int itemB = transaction[j];
                        int first = Math.Min(itemA, itemB);
                        int second = Math.Max(itemA, itemB);
                        string key = $"{first}_{second}";
                        if (!pairCounts.ContainsKey(key)) pairCounts[key] = 0;
                        pairCounts[key]++;
                    }
                }
            }

            _context.ProductRecommendations.RemoveRange(_context.ProductRecommendations);
            foreach (var pair in pairCounts)
            {
                var ids = pair.Key.Split('_');
                int a = int.Parse(ids[0]), b = int.Parse(ids[1]);
                
                double confAB = (double)pair.Value / itemCounts[a];
                double confBA = (double)pair.Value / itemCounts[b];

                if (confAB >= 0.1) _context.ProductRecommendations.Add(new ProductRecommendation { ProductID_A = a, ProductID_B = b, Confidence = confAB });
                if (confBA >= 0.1) _context.ProductRecommendations.Add(new ProductRecommendation { ProductID_A = b, ProductID_B = a, Confidence = confBA });
            }
            _context.SaveChanges();
            return Json(new { success = true, message = "Đã cập nhật thuật toán thành công!" });
        }
    }
}