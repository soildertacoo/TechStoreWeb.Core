using Newtonsoft.Json;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TechStore.Models;
using System;
using System.Collections.Generic;
using TechStoreWeb.Core.Helpers;
using Microsoft.EntityFrameworkCore.Storage.Json;
using System.Data.Common;
using TechStoreWeb.Core.InventoryServices;

namespace TechStore.Controllers
{
    public class AdminsController : Controller
    {
        // GET: Admins
        private readonly DBTechStoreEntities dBO;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IInventoryCalculationService _inventoryCalculationService;

        // 2. Tạo hàm khởi tạo (Constructor) và yêu cầu hệ thống "tiêm" DbContext vào
        public AdminsController(DBTechStoreEntities dbContext, ApplicationDbContext appContext, 
        IWebHostEnvironment env, IInventoryCalculationService inventoryCalculationService)
        {
            dBO = dbContext;
            _context = appContext;
            _env = env;
            _inventoryCalculationService = inventoryCalculationService;
        }        
        public ActionResult Index()
        {
            if (HttpContext.Session.GetString("admin")== null)
            {
                return RedirectToAction("Login", "Admins");
            }
            string? admin = HttpContext.Session.GetString("admin");//Hiện tên admin
            ViewBag.Admin = admin;
            return View();
        }
        [HttpGet]
        public ActionResult shippingManagement()
        {
            var provider = dBO.ShippingProviders.ToList();
            if (provider == null) return NotFound();
            return View(provider);
        }
        [HttpGet]
        public ActionResult createProvider()
        {
            return View("~/Views/Admins/Shipping/createProvider.cshtml");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Tham số nhận vào chính là Model ShippingProviders
        public ActionResult createProvider(ShippingProviders provider)
        {
            try
            {
                // 1. Kiểm tra xem các trường bắt buộc (Required) đã có đủ chưa
                if (ModelState.IsValid)
                {
                    // 2. Kiểm tra xem Mã Code (Khóa chính) đã bị trùng trong CSDL chưa
                    // Lưu ý: Đổi chữ 'dbO' thành tên biến DbContext thực tế của bạn
                    var checkExist = dBO.ShippingProviders.FirstOrDefault(p => p.ProviderCode == provider.ProviderCode);
                    
                    if (checkExist != null)
                    {
                        // Nếu trùng mã, báo lỗi và trả lại y nguyên trang Create kèm dữ liệu cũ đang nhập dở
                        ViewBag.ErrorCreate = "Mã Đơn vị vận chuyển này đã tồn tại trong hệ thống!";
                        return View("~/Views/Admins/Shipping/createProvider.cshtml", provider);
                    }

                    // 3. Setup một vài giá trị mặc định (Ví dụ: Vừa tạo xong thì cho trạng thái Kích hoạt luôn)
                    provider.IsActive = true; 

                    // 4. Thêm đối tượng vào Bảng và Lưu lại
                    dBO.ShippingProviders.Add(provider);
                    dBO.SaveChanges();

                    // 5. 🌟 Áp dụng bài học lúc nãy: Lưu XONG thì dùng RedirectToAction 
                    // để bắt hệ thống chạy lại hàm hiển thị danh sách (giả sử là shippingManagement)
                    return RedirectToAction("shippingManagement", "Admins"); 
                }
                
                // Nếu Validation thất bại (Ví dụ: nhập chữ vào ô số), trả lại trang Create
                return View("~/Views/Admins/Shipping/createProvider.cshtml", provider);
            }
            catch (Exception ex)
            {
                // Bắt lỗi hệ thống (như sập Database, sai chuỗi kết nối...)
                ViewBag.ErrorCreate = "Lỗi hệ thống khi lưu CSDL: " + ex.Message;
                return View("~/Views/Admins/Shipping/createProvider.cshtml", provider);
            }
        }
        [HttpGet]
        public ActionResult editProvider(string code)
        {
            var thisProvider = dBO.ShippingProviders.FirstOrDefault(provider => provider.ProviderCode == code);
            if (thisProvider == null) return RedirectToAction("shippingManagement","Admins");
            return View("~/Views/Admins/Shipping/editProvider.cshtml",thisProvider);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult editProvider(ShippingProviders provider)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Tìm dòng dữ liệu cũ trong Database dựa theo Mã Code
                    var existingProvider = dBO.ShippingProviders.FirstOrDefault(p => p.ProviderCode == provider.ProviderCode);
                    
                    if (existingProvider == null)
                    {
                        throw new Exception("Ko có dữ liệu");
                    }

                    // Ghi đè các thông tin mới từ form lên dữ liệu cũ
                    existingProvider.ProviderName = provider.ProviderName;
                    existingProvider.ApiToken = provider.ApiToken;
                    existingProvider.ApiCreateOrder = provider.ApiCreateOrder;
                    existingProvider.ApiCancelOrder = provider.ApiCancelOrder;
                    existingProvider.ApiCheckStatus = provider.ApiCheckStatus;
                    
                    existingProvider.SupportStandard = provider.SupportStandard;
                    existingProvider.SupportFast = provider.SupportFast;
                    existingProvider.SupportExpress = provider.SupportExpress;
                    
                    existingProvider.PriceStandard = provider.PriceStandard;
                    existingProvider.PriceFast = provider.PriceFast;
                    existingProvider.PriceExpress = provider.PriceExpress;

                    // Lưu thay đổi vào CSDL
                    dBO.SaveChanges();

                    // Sửa xong thì quay lại trang danh sách
                    return RedirectToAction("shippingManagement", "Admins");
                }

                return View("~/Views/Admins/Shipping/editProvider.cshtml", provider);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorEdit = "Lỗi hệ thống khi cập nhật: " + ex.Message;
                return View("~/Views/Admins/Shipping/editProvider.cshtml", provider);
            }
        }
        [HttpPost]
        public async Task<IActionResult> providerDeleteData ([FromBody]JSONProvider data)
        {
            try
            {
                var thisProvider = dBO.ShippingProviders.FirstOrDefault(id => id.ProviderCode == data.ProviderCode);
                if (thisProvider == null) throw new Exception ("Lỗi khi xử lý dữ liệu");
                //Xóa mã đơn
                dBO.ShippingProviders.Remove(thisProvider);
                await dBO.SaveChangesAsync();
                //Reload du lieu
                var provider = dBO.ShippingProviders.ToList();
                return Json(new {success = true, providerData = provider ,message = "Xoa thanh cong"});

            }
            catch (Exception e)
            {
                return Json(new {success = false, message = "Co loi khi xoa du lieu" + e});
            }
        }
        
        public ActionResult Statistics(
            string type="day",
            DateTime? date=null,
            int? month=null,
            int? quarter=null,
            int? year=null,
            DateTime? fromDate=null,
            DateTime? toDate=null
        )
    {
        var revenueQuery = dBO.OrderDetails
            .Join(dBO.OrderPro,
                od => od.IDOrder,
                o => o.ID,
                (od, o) => new
                {
                    Subtotal = od.Subtotal ?? 0,
                    o.DateOrder,
                    od.IDProduct,
                    Quantity = od.Quantity ?? 0
                })
            .Where(x => x.DateOrder != null);
            switch(type)
            {
                case "day":
                    if(date.HasValue)
                        revenueQuery=revenueQuery.Where(x=>x.DateOrder.Value.Date==date.Value.Date);
                    break;

                case "month":
                    if (month.HasValue && year.HasValue)
                    revenueQuery=revenueQuery.Where(x=>x.DateOrder.Value.Month==month &&
                             x.DateOrder.Value.Year==year);
                    break;
                
                case "quarter":
                    if (quarter.HasValue && year.HasValue)
                    {
                        int start=(quarter.Value-1)*3+1;
                        int end = start + 2;
                        revenueQuery=revenueQuery.Where(x=>
                             x.DateOrder.Value.Year==year &&
                             x.DateOrder.Value.Month>=start &&
                             x.DateOrder.Value.Month<=end);
                    }
                    break;

                case "range":
                    revenueQuery=revenueQuery.Where(x=>
                     x.DateOrder.Value>=fromDate &&
                     x.DateOrder.Value<=toDate);
                break;
            }


        // Doanh thu theo ngày
        var revenueOverTime = revenueQuery
            .GroupBy(x => x.DateOrder.Value.Date) 
            .Select(g => new 
            {
                Date = g.Key,
                TotalRevenue = g.Sum(x => x.Subtotal),
                Products = g.GroupBy(x => x.IDProduct)
                    .Select(pg => new { ProductID = pg.Key, QuantitySold = pg.Sum(x => x.Quantity) })
                    .ToList()
            })
            .OrderBy(x => x.Date)
            .ToList();

        ViewBag.Labels = revenueOverTime.Select(d => d.Date.ToString("yyyy-MM-dd")).ToList();
        ViewBag.DataRevenue = revenueOverTime.Select(d => d.TotalRevenue).ToList();

        
        // Tính toán lời lỗ theo ngày
            var profitLossLabels = new List<string>();
            var profitLossRevenueData = new List<decimal>();
            var profitLossCostData = new List<decimal>();
            var profitLossProfitData = new List<decimal>();
            decimal totalProfit = 0;
            decimal totalCostAll = 0;

            foreach (var day in revenueOverTime)
            {
                decimal totalCogs = 0;
                foreach (var product in day.Products)
                {
                    try
                    {
                        // Lấy giá vốn trung bình của sản phẩm
                        var batches = _context.InventoryBatches
                            .Where(b => b.ProductID == product.ProductID && b.RemainingQuantity > 0)
                            .ToList();

                        if (batches.Any())
                        {
                            decimal totalValue = batches.Sum(b => b.RemainingQuantity * b.UnitCost);
                            int totalQuantity = batches.Sum(b => b.RemainingQuantity);
                            decimal avgCost = totalQuantity > 0 ? totalValue / totalQuantity : 0;
                            totalCogs += avgCost * product.QuantitySold;
                        }
                        else
                        {
                            // Nếu không có batch nào, lấy giá từ Products
                            var productInfo = dBO.Products.FirstOrDefault(p => p.ProductID == product.ProductID);
                            if (productInfo != null)
                            {
                                totalCogs += productInfo.Price * product.QuantitySold;
                            }
                        }
                    }
                    catch
                    {
                        // Bỏ qua nếu có lỗi
                    }
                }

                decimal revenue = (decimal)day.TotalRevenue;
                decimal profit = revenue - totalCogs;
                
                profitLossLabels.Add(day.Date.ToString("yyyy-MM-dd"));
                profitLossRevenueData.Add(revenue);
                profitLossCostData.Add(totalCogs);
                profitLossProfitData.Add(profit);
                
                totalProfit += profit;
                totalCostAll += totalCogs;
            }

            ViewBag.ProfitLossLabels = profitLossLabels;
            ViewBag.ProfitLossRevenueData = profitLossRevenueData;
            ViewBag.ProfitLossCostData = profitLossCostData;
            ViewBag.ProfitLossProfitData = profitLossProfitData;

            // Tổng hợp lợi nhuận
            ViewBag.TotalProfit = totalProfit;
            ViewBag.TotalCost = totalCostAll;

            // Đánh giá - áp dụng filter theo thời gian
            var reviewQuery = dBO.Reviews.AsQueryable();
            switch(type)
            {
                case "day":
                    if(date.HasValue)
                        reviewQuery=reviewQuery.Where(r=>r.ReviewDate.HasValue && r.ReviewDate.Value.Date==date.Value.Date);
                    break;

                case "month":
                    if (month.HasValue && year.HasValue)
                        reviewQuery=reviewQuery.Where(r=>r.ReviewDate.HasValue && r.ReviewDate.Value.Month==month &&
                                 r.ReviewDate.Value.Year==year);
                    break;
                
                case "quarter":
                    if (quarter.HasValue && year.HasValue)
                    {
                        int start=(quarter.Value-1)*3+1;
                        int end = start + 2;
                        reviewQuery=reviewQuery.Where(r=>r.ReviewDate.HasValue &&
                                 r.ReviewDate.Value.Year==year &&
                                 r.ReviewDate.Value.Month>=start &&
                                 r.ReviewDate.Value.Month<=end);
                    }
                    break;

                case "range":
                    reviewQuery=reviewQuery.Where(r=>r.ReviewDate.HasValue &&
                     r.ReviewDate.Value>=fromDate &&
                     r.ReviewDate.Value<=toDate);
                break;
            }

            var reviewData = reviewQuery
                .GroupBy(r => r.ProductID)
                .Select(g => new
                {
                    ProductsName = dBO.Products
                        .Where(p => p.ProductID == g.Key)
                        .Select(p => p.NamePro)
                        .FirstOrDefault(),
                    ReviewCount = g.Count(),
                    ReviewAverage = g.Average(r => r.Rating > 5 ? 5 : r.Rating)
                })
                .Where(x => x.ProductsName != null)
                .ToList();

            ViewBag.ReviewLabels = reviewData.Select(r => r.ProductsName).ToList();
            ViewBag.ReviewData = reviewData.Select(r => r.ReviewCount).ToList();
            ViewBag.AverageRating = reviewData.Select(r => r.ReviewAverage).ToList();

            // Bán chạy - áp dụng filter theo thời gian (sử dụng revenueQuery đã filter)
            var bestSelling = revenueQuery
                .GroupBy(od => od.IDProduct)
                .Select(g => new {
                    ProductsName = dBO.Products
                        .Where(p => p.ProductID == g.Key)
                        .Select(p => p.NamePro)
                        .FirstOrDefault(),
                    QuantitySold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(g => g.QuantitySold)
                .Take(5)
                .ToList();

            ViewBag.BestSellingLabels = bestSelling.Select(x => x.ProductsName).ToList();
            ViewBag.BestSellingData = bestSelling.Select(x => x.QuantitySold).ToList();

            return View();
        }


        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }
        [HttpPost, ActionName("Login")]
        public async Task<IActionResult> LoginConfirm(AdminUsers admin)
        {
            var adminUser = dBO.AdminUsers.FirstOrDefault(c => c.NameUser == admin.NameUser);

                // Nếu không tìm thấy User trong DB -> Chặn luôn
                if (adminUser == null)
                {
                    ViewBag.ThongBao = "Tài khoản không tồn tại!";
                    return View();
                }

                // BƯỚC 2: Kiểm tra xem tài khoản có đang bị khóa hay không?
                if (adminUser.IsBanned == true)
                {
                    // Kiểm tra xem đã hết thời hạn 30 phút chưa
                    if (adminUser.BannedUntil != null && adminUser.BannedUntil > DateTime.Now)
                    {
                        ViewBag.ThongBao = $"Tài khoản của bạn đã bị khóa đến {adminUser.BannedUntil:HH:mm}. Lý do: {adminUser.ReasonBanned}";
                        return View();
                    }
                    else // Nếu đã hết thời hạn khóa thì tự động mở khóa cho người dùng
                     {
                        adminUser.IsBanned = false;
                        adminUser.ReasonBanned = null;
                        adminUser.BannedUntil = null;
                        adminUser.FailedLoginAttempts = 0; // Reset lại số lần thử sau khi mở khóa
                        await dBO.SaveChangesAsync();
                    }
                   
                }

                
                bool isPasswordCorrect = (adminUser.PasswordUser.Trim() == admin.PasswordUser.Trim()) ? true : false; 

                if (!isPasswordCorrect)
                {
                    //Thêm số lần thử vào database để tránh trường hợp tấn công bằng cách gửi nhiều request
                    adminUser.FailedLoginAttempts = (adminUser.FailedLoginAttempts ?? 0) + 1;
                    await dBO.SaveChangesAsync();

                    if (adminUser.FailedLoginAttempts >= 5)
                    {
                        // 🚨 QUAN TRỌNG: Cập nhật CẢ 3 trường dữ liệu
                        adminUser.IsBanned = true;
                        adminUser.ReasonBanned = "Quá nhiều lần đăng nhập thất bại";
                        adminUser.BannedUntil = DateTime.Now.AddMinutes(30); // Thiết lập thời gian khóa 30 phút
                        
                        await dBO.SaveChangesAsync();
                        
                        ViewBag.ThongBao = "Không đăng nhập thành công vì bạn đã nhập sai quá 5 lần. Tài khoản sẽ bị khóa 30 phút.";
                    } else ViewBag.ThongBao = $"Không đăng nhập thành công vì bạn đã nhập sai, bạn còn {5 - adminUser.FailedLoginAttempts} lần thử.";
                    
                    return View();
                }

                
                ViewBag.ThongBao = "Chúc mừng đăng nhập thành công ";
                adminUser.FailedLoginAttempts = 0; // Reset lại số lần thử sau khi mở khóa
                await dBO.SaveChangesAsync();
                HttpContext.Session.SetString("admin", admin.NameUser);
                return RedirectToAction("Statistics", "Admins");
        }
        private AdminUsers? ValidateUser(string username, string password)
        {
            return dBO.AdminUsers.FirstOrDefault(s => s.NameUser == username && s.PasswordUser == password);
        }
        public ActionResult ProductsManament()
        {

            return View();
        }

        public ActionResult CateManament()
        {
            return View();
        }

        public ActionResult OderManagement()
        {
            return View();
        }

        // Support Request Management Actions
        public ActionResult SupportRequestManagement(string filter = "all", string search = "")
        {
            if (HttpContext == null)
            {
                return RedirectToAction("Login", "Admins");
            }

            var requests = dBO.SupportRequests.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                 requests = requests.Where(r => r.IdRequest.Contains(search) ||
                                             r.CustomerName.Contains(search) ||
                                             r.Email.Contains(search) ||
                                             r.OrderNumber.Contains(search));
            }

            // Apply status filter
            switch (filter.ToLower())
            {
                case "refund":
                    requests = requests.Where(r => r.RequestType == "Refund");
                    break;
                case "warranty":
                    requests = requests.Where(r => r.RequestType == "Warranty");
                    break;
                case "recent":
                    DateTime sevenDay_ago = DateTime.Now.AddDays(-7);
                    requests = requests.Where(r => r.RequestDate >= sevenDay_ago);
                    break;
                default:
                    // Show all
                    break;
            }

            var supportRequests = requests.OrderByDescending(r => r.RequestDate).ToList();
            
            ViewBag.Filter = filter;
            ViewBag.Search = search;
            ViewBag.TotalRequests = dBO.SupportRequests.Count();
            ViewBag.RefundRequests = dBO.SupportRequests.Count(r => r.RequestType == "Refund");
            ViewBag.WarrantyRequests = dBO.SupportRequests.Count(r => r.RequestType == "Warranty");
            //Thêm biến datetime 
            DateTime sevenDaysAgo = DateTime.Now.AddDays(-7);
            var recentRequests = dBO.SupportRequests
                .Where(r => r.RequestDate >= sevenDaysAgo)
                .ToList();
            ViewBag.RecentRequests = recentRequests.Count() ;

            return View(supportRequests);
        }

        public ActionResult SupportRequestDetails(string id)
        {
            if (HttpContext.Session.GetString("admin") == null)
            {
                return RedirectToAction("Login", "Admins");
            }

            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("SupportRequestManagement");
            }

            var request = dBO.SupportRequests.FirstOrDefault(r => r.IdRequest == id);
            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu hỗ trợ.";
                return RedirectToAction("SupportRequestManagement");
            }

            return View(request);
        }
        public class JSONOrder
        {
            public string? requestId {get;set;}
            public string? action {get;set;}
            public string? reason {get;set;}


        }
        [HttpPost]
        public ActionResult ProcessSupportRequest([FromForm] JSONOrder data)
        {
            if (HttpContext.Session.GetString("admin") == null)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                //Lấy request được chọn
                var request = dBO.SupportRequests.FirstOrDefault(r => r.IdRequest == data.requestId);
                if (request == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy yêu cầu." });
                }

                string? adminName = HttpContext.Session.GetString("admin");
                string statusMessage = "";
                string Status = "";
                if (data.action == "approve")
                {
                    statusMessage = $"Yêu cầu đã được phê duyệt bởi {adminName} vào {DateTime.Now:dd/MM/yyyy HH:mm}";
                    Status = "Accepted";
                    if (!string.IsNullOrEmpty(data.reason))
                    {
                        statusMessage += $". Ghi chú: {data.reason}";
                    }
                }
                else if (data.action == "reject")
                { 
                    Status = "Rejected";
                    statusMessage = $"Yêu cầu đã bị từ chối bởi {adminName} vào {DateTime.Now:dd/MM/yyyy HH:mm}";
                    if (!string.IsNullOrEmpty(data.reason))
                    {
                        statusMessage += $". Lý do: {data.reason}";
                    }
                }

                // Update the request description to include status
                request.Status = Status;
                request.Description = statusMessage ;
                dBO.Entry(request).State = Microsoft.EntityFrameworkCore.EntityState.Modified;                
                dBO.SaveChanges();

                return Json(new { 
                    success = true, 
                    message = data.action == "approve" ? "Đã phê duyệt yêu cầu thành công!" : "Đã từ chối yêu cầu thành công!",
                    action = data.action,
                    processedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    processedBy = adminName
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult DeleteSupportRequest(string requestId)
        {
            if (HttpContext.Session.GetString("admin") == null)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var request = dBO.SupportRequests.FirstOrDefault(r => r.IdRequest == requestId);
                if (request == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy yêu cầu." });
                }

                dBO.SupportRequests.Remove(request);
                dBO.SaveChanges();

                return Json(new { success = true, message = "Đã xóa yêu cầu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
        // =======================================================
        // API: XÓA CUỘC TRÒ CHUYỆN CHAT VỚI KHÁCH HÀNG
        // =======================================================
        [HttpPost]
        public ActionResult DeleteChatRoom(string roomName)
        {
            if (HttpContext.Session.GetString("admin") == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });
            }

            try
            {
                // Sửa thành ChatMessages (có s)
                var messages = _context.ChatMessages.Where(m => m.RoomId == roomName).ToList();
                if (messages.Any())
                {
                    // Sửa thành ChatMessages (có s)
                    _context.ChatMessages.RemoveRange(messages);
                    _context.SaveChanges();
                }

                return Json(new { success = true, message = "Đã xóa cuộc trò chuyện!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }


        }
        // =======================================================
        // API: TẢI DANH SÁCH PHÒNG CHAT & KIỂM TRA TIN NHẮN OFFLINE
        // =======================================================
        [HttpGet]
        public ActionResult GetChatRoomsSummary()
        {
            if (HttpContext.Session.GetString("admin") == null)
            {
                return Json(new { success = false });
            }

            try
            {
                // Sửa thành ChatMessages (có s)
                var rooms = _context.ChatMessages.Select(m => m.RoomId).Distinct().ToList();
                var result = new List<object>();

                foreach (var room in rooms)
                {
                    // Lấy tin nhắn mới nhất của phòng này
                    // Sửa thành ChatMessages (có s)
                    var lastMsg = _context.ChatMessages
                        .Where(m => m.RoomId == room)
                        .OrderByDescending(m => m.SentAt)
                        .FirstOrDefault();

                    // LOGIC: Nếu tin nhắn cuối cùng là của Khách (IsFromSupport = false) -> Admin chưa trả lời -> Là tin nhắn MỚI (Unread)
                    bool isUnread = lastMsg != null && lastMsg.IsFromSupport == false;

                    // Lấy Avatar
                    var customer = dBO.Customers.FirstOrDefault(c => c.NameCus == room);
                    string avatar = customer != null && !string.IsNullOrEmpty(customer.ImagePro) ? customer.ImagePro : "";

                    result.Add(new { 
                        room = room, 
                        lastMessage = lastMsg != null ? lastMsg.Content : "Nhấn để xem tin nhắn",
                        isUnread = isUnread,
                        avatar = avatar
                    });
                }

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }   
}