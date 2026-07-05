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
            // =====================================================================
            // BƯỚC 1: LẤY VÀ LỌC DỮ LIỆU ĐẦU VÀO (TIỀN XỬ LÝ - PREPROCESSING)
            // =====================================================================
            var transactions = _context.OrderDetails
                .GroupBy(od => od.IDOrder)       // Gom nhóm tất cả sản phẩm theo Mã đơn hàng
                .Where(g => g.Count() > 1)       // Chỉ giữ lại những đơn hàng có mua từ 2 món trở lên
                .Select(g => g.Select(od => od.IDProduct).Distinct().ToList()) // Trích xuất ra danh sách ID Sản phẩm (bỏ trùng lặp nếu có)
                .ToList();                       // Kéo toàn bộ dữ liệu hợp lệ này lên RAM để xử lý nhanh

            // Nếu dữ liệu quá ít (không có đơn nào > 1 món), dừng thuật toán để tránh lỗi
            if (!transactions.Any()) return Json(new { success = false, message = "Không đủ dữ liệu đơn hàng" });

            int totalTransactions = transactions.Count;
            
            // =====================================================================
            // BƯỚC 2: KHỞI TẠO TỪ ĐIỂN ĐỂ ĐẾM TẦN SUẤT 
            // =====================================================================
            // itemCounts: Đếm xem mỗi sản phẩm (đứng lẻ) được mua tổng cộng bao nhiêu lần
            var itemCounts = new Dictionary<int, int>();
            
            // pairCounts: Đếm xem các cặp 2 sản phẩm (mua chung) xuất hiện bao nhiêu lần
            var pairCounts = new Dictionary<string, int>();

            // Bắt đầu duyệt qua từng hóa đơn hợp lệ đã lọc ở Bước 1
            foreach (var transaction in transactions)
            {
                // Vòng lặp thứ nhất: Lấy ra sản phẩm A trong hóa đơn
                for (int i = 0; i < transaction.Count; i++)
                {
                    int itemA = transaction[i];
                    
                    // Nếu sản phẩm A chưa có trong từ điển thì thêm vào và bắt đầu đếm
                    if (!itemCounts.ContainsKey(itemA)) itemCounts[itemA] = 0;
                    itemCounts[itemA]++;

                    // =====================================================================
                    // BƯỚC 3: GHÉP CẶP VÀ ĐẾM SỐ LẦN MUA CHUNG
                    // =====================================================================
                    // Vòng lặp thứ hai: Lấy các sản phẩm B (đứng sau A) trong cùng hóa đơn đó
                    for (int j = i + 1; j < transaction.Count; j++)
                    {
                        int itemB = transaction[j];
                        
                        // Luôn sắp xếp ID nhỏ đứng trước, ID lớn đứng sau. 
                        // Mục đích: Tránh việc cặp (1,2) và cặp (2,1) bị đếm tách rời nhau.
                        int first = Math.Min(itemA, itemB);
                        int second = Math.Max(itemA, itemB);
                        string key = $"{first}_{second}"; // Tạo chuỗi định danh, ví dụ: "1_2"
                        
                        // Đếm số lần cặp (A, B) này xuất hiện cùng nhau
                        if (!pairCounts.ContainsKey(key)) pairCounts[key] = 0;
                        pairCounts[key]++;
                    }
                }
            }

            // Xóa sạch các bộ luật cũ trong Database để làm mới lại từ đầu
            _context.ProductRecommendations.RemoveRange(_context.ProductRecommendations);
            
            // =====================================================================
            // BƯỚC 4: TÍNH TOÁN ĐỘ TIN CẬY (CONFIDENCE) VÀ SINH LUẬT
            // =====================================================================
            // Duyệt qua tất cả các cặp sản phẩm đã đếm được
            foreach (var pair in pairCounts)
            {
                // Tách chuỗi "1_2" thành số 1 (sản phẩm a) và số 2 (sản phẩm b)
                var ids = pair.Key.Split('_');
                int a = int.Parse(ids[0]), b = int.Parse(ids[1]);
                
                // Công thức sinh luật: (Số lần mua chung cặp AB) chia cho (Số lần mua lẻ từng món)
                // confAB: Tỷ lệ % người mua A sẽ mua thêm B
                double confAB = (double)pair.Value / itemCounts[a];
                
                // confBA: Tỷ lệ % người mua B sẽ mua thêm A
                double confBA = (double)pair.Value / itemCounts[b];

                // =====================================================================
                // BƯỚC 5: LỌC THEO NGƯỠNG TỐI THIỂU (MIN CONFIDENCE) & LƯU DATABASE
                // =====================================================================
                // Ngưỡng >= 0.1 (tức là 10%). Nếu xác suất mua kèm lớn hơn 10% thì mới đưa vào Database để gợi ý cho khách
                if (confAB >= 0.1) _context.ProductRecommendations.Add(new ProductRecommendation { ProductID_A = a, ProductID_B = b, Confidence = confAB });
                if (confBA >= 0.1) _context.ProductRecommendations.Add(new ProductRecommendation { ProductID_A = b, ProductID_B = a, Confidence = confBA });
            }
            
            // Chạy lệnh lưu tất cả những luật thỏa mãn xuống SQL Server
            _context.SaveChanges();
            
            // Trả về thông báo thành công cho giao diện Admin
            return Json(new { success = true, message = "Đã cập nhật thuật toán thành công!" });
        }
    }
}