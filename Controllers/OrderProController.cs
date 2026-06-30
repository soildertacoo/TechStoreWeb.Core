using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Bắt buộc phải có cho Session
using TechStore.Models;
using ClosedXML.Excel;
using System.Data;
using  System.Globalization;
using System.Text;
using System.Text.Json;
using TechStoreWeb.Core.PayModel;
using TechStoreWeb.Core.ShippingServices;
namespace TechStore.Controllers
{
    public class OrderProController : Controller
    {
        private readonly DBTechStoreEntities db;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;//Lấy them số từ appSetting.json


        // Constructor "tiêm" DbContext
        public OrderProController(DBTechStoreEntities dbContext, ApplicationDbContext appContext, IConfiguration configuration)
        {
            db = dbContext;
            _context = appContext;
            _configuration = configuration;
        }

        // GET: OrderPro
        [HttpGet]
        public ActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("admin")))
            {
                return RedirectToAction("Login", "Admins");
            }
            // Ưu tiên dùng Lambda expression cho Include để tránh gõ sai tên bảng
            var list = db.OrderPro.Include(o => o.Customer).ToList();
            //ViewBag.Error = (string)TempData["Error"];
            return View(list);
        }

        [HttpGet]
        public ActionResult Details(int? id)
        {
            if (id == null) return BadRequest(); 
            
            var order = db.OrderPro
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Products).AsEnumerable().OrderByDescending(o => o.DateOrder)
                .FirstOrDefault(o => o.ID == id);

            if (order == null) return NotFound(); 
            
            OrderDetails_model OrderDetails_Model = new OrderDetails_model
            {
                OrderDetails = order.OrderDetails.ToList(),
                OrderPro = order
            };
            return View(OrderDetails_Model);
        }

        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (id == null) return BadRequest();
            
            var orderPro = db.OrderPro.FirstOrDefault(s => s.ID == id);
            if (orderPro == null) return NotFound();
            
            return View(orderPro);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(OrderPro orderPro)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingOrder = db.OrderPro.FirstOrDefault(o => o.ID == orderPro.ID);
                    if (existingOrder == null) return NotFound();
                    
                    existingOrder.Status = orderPro.Status;
                    db.Entry(existingOrder).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index", "OrderPro");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Lỗi khi cập nhật: " + ex.Message);
                    TempData["Error"] = "Lỗi khi cập nhật đơn hàng: " + ex.Message;
                }
            }
            return RedirectToAction("Index", "OrderPro");
        }

        [HttpGet]
        public ActionResult Delete(int? id)
        {
            // if (id == null)
            // {
            //     TempData["Error"] = "Không thể hủy đơn được";
            //     return RedirectToAction("Index");
            // }
            var item = db.OrderPro.Find(id);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult ActionDelete(int id, string action)
        {
            // var item = db.OrderPro.FirstOrDefault(s => s.ID == id);
            
            // if (item != null)
            // {
            //     if (item.Status != "Đang giao" && item.Status != "Đã giao")
            //     {
            //         // Lấy TOÀN BỘ chi tiết đơn hàng (không dùng FirstOrDefault)
            //         var OrderDetails = db.OrderDetails.Where(s => s.IDOrder == item.ID).ToList();
                    
            //         db.OrderDetails.RemoveRange(OrderDetails); 
            //         db.OrderPro.Remove(item);
            //         db.SaveChanges();
            //     }
            //     else
            //     {
            //         TempData["Error"] = "Không hủy được do đang giao hay đã giao";
            //         return RedirectToAction("Details", "OrderPro", new { id = id });
            //     }
            // }
            // else
            // {
            //     if (!string.IsNullOrEmpty(action))
            //     {
            //         TempData["Error"] = "Lỗi ko xác định";
            //         return RedirectToAction(action, "OrderPro", new { id = id });
            //     }
            //     TempData["Error"] = "Lỗi ko xác định";
            //     return RedirectToAction("Details", "OrderPro", new { id = id });
            // }

            return RedirectToAction("Index", "OrderPro");
        }

        [HttpGet]
        public ActionResult Index_KH(int id)
        {
            if (TempData["Error"] != null)
            {
                ViewBag.Error = (string)TempData["Error"];
            }
            var list = db.OrderPro.Include(o => o.Customer).Where(s => s.IDCus == id).OrderByDescending(o => o.ID).ToList();
            return View(list);
        }
        public class JSONOrder()
        {
            public string? id {get; set;} 
            public string? nameCus {get;set;} 
            public string? fromDateInput{get; set;}
            public string? toDateInput{get; set;}
            public string? status{get; set;}
            public string? paymentStatus{get; set;}
        }
        [HttpPost] 
        /*public async Task<IActionResult> Delete_KH([FromBody] JSONOrder data)
        {
            var item = db.OrderPro
            .Include(o => o.OrderDetails) // Bao gồm chi tiết đơn hàng để có thể xóa chúng nếu cần
            .FirstOrDefault(s => s.TrackingNumber == data.id);
            if (item == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }
            else if (item != null)
            {
                if ( item.Status.Trim() != "Đã giao") 
                {
                    item.Status ="Hủy đơn";
                    db.Entry(item).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    return Json(new { success = true });
                }
                else if ( item.Status.Trim() != "Đang xử lý")
                {
                    item.Status ="Hủy đơn";
                    db.Entry(item).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    return Json(new { success = true });
                }
                // Không cho hủy nếu đã giao hoặc đang giao
                else
                {
                    return Json(new { success = false, message = "Không hủy đơn được do đang giao hay đã giao" });
                }
            }
            
            return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
        }*/

        public async Task<IActionResult> Delete_KH([FromBody] JSONOrder data)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var item = db.OrderPro
                    .Include(o => o.OrderDetails) // Bao gồm chi tiết đơn hàng để có thể xóa chúng nếu cần
                    .FirstOrDefault(s => s.TrackingNumber == data.id);
                
                    if (item == null)
                    {
                        throw new Exception("Không tìm thấy đơn hàng.");
                    }
                    // Không cho hủy nếu đã giao hoặc đang giao
                    else if (item.Status.Trim().ToLower() == "đang giao" || item.Status.Trim().ToLower() == "đã giao")
                    {
                        throw new Exception("Không hủy đơn được do đang giao hay đã giao" );
                    }
                    // Tránh cộng kho nhiều lần
                    else if (item.Status.Trim().ToLower() == "hủy đơn")
                    {
                        throw new Exception("Đơn hàng đã được hủy trước đó.");
                    }
                    // Trả hàng về kho
                    foreach (var orderDetail in item.OrderDetails)
                    {
                        var inventory = db.Inventories
                        .FirstOrDefault(i => i.ProductID == orderDetail.IDProduct);
                        if (inventory != null)
                        {
                            inventory.StockQuantity += orderDetail.Quantity ?? 0; // Cộng số lượng trả về kho
                            inventory.LastUpdated = DateTime.Now; // Cập nhật thời gian chỉnh sửa
                        }
                    }
                    item.Status = "Hủy đơn";
                    db.Entry(item).State = EntityState.Modified;
                    //Huy don tren GHN truc tiep hoac la don vi gi khac trong tuong lai                    
                    GhnShippingService ghn = new GhnShippingService(new HttpClient());
                    var provider = await db.ShippingProviders.FirstOrDefaultAsync(ship => ship.ProviderCode.Trim().ToLower() == "ghn");
                    await ghn.cancelDVVC(item.ShippingCode, provider);
                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch(Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine("36636_Hủy đơn ko thành công do " + ex.Message);
                    return Json(new { success = false, message = ex.Message });
                }
            }
            return Json(new { success = true });
        }   
        [HttpPost]
        //Nếu FormData trong view chuyền thì FromForm ngược lại thì FromBody
        public async Task<IActionResult> exportExcel([FromForm] JSONOrder data)
        {
            string [] format = { "dd/MM/yyyy", "dd-MM-yyyy","yyyy-MM-dd"}; 
            var query = db.OrderPro.Include(c=> c.Customer).AsQueryable();
            if (!string.IsNullOrEmpty(data.nameCus))
                query = query.Where(x => x.Customer.NameCus == data.nameCus);
            if (!string.IsNullOrEmpty(data.status))
                query = query.Where(x => x.Status == data.status);

            if (!string.IsNullOrEmpty(data.paymentStatus))
                query = query.Where(x => x.PaymentStatus == data.paymentStatus);

            if (!string.IsNullOrEmpty(data.fromDateInput)){
                
                DateTime dateConverted = DateTime.ParseExact(data.fromDateInput, format, CultureInfo.InvariantCulture);
                query = query.Where(x => x.DateOrder >= dateConverted);
            }

            if (!string.IsNullOrEmpty(data.toDateInput)){
                DateTime dateConverted = DateTime.ParseExact(data.toDateInput, format, CultureInfo.InvariantCulture);
                query = query.Where(x => x.DateOrder <= dateConverted);
            }
            var orderProList = (await query.ToListAsync()).Select(s=> new
            {
                ShippingNumber = s.TrackingNumber,
                NameCus = s.Customer.NameCus ?? "Không có tên",
                AddressDelivery = s.AddressDeliverry,
                ShippingStatus = s.Status,
                paymentMethod = s.PaymentMethod,
                paymentStatus = s.PaymentStatus,
                dateOrder = s.DateOrder,
                shippingDate = s.DeliveryDate,
                Amount = s.TotalAmount,
                shippingCost = s.ShippingCost
            }).ToList();
            

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet();
            ws.Cell(1,1).InsertTable(orderProList); //Insert dữ liệu table vào bảng 
            using (var stream = new MemoryStream())
            {
                //Gọi trình duyệt tải về file excel về
                wb.SaveAs(stream);
                var content = stream.ToArray();
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheet.sheet";
                //Tải file về 
                string from = data.fromDateInput ?? DateTime.Now.ToString();
                string? to = data.toDateInput;
                string? cus = data.nameCus;
                // 2. Nối chuỗi tên file
                string fileName = $"BaoCaoDonHang";
                if (!string.IsNullOrEmpty(from))
                {
                    fileName += $"_{from}";
                }
                if (!string.IsNullOrEmpty(to))
                {
                    fileName += $"_Den_{to}";
                }
                 if (!string.IsNullOrEmpty(cus))
                {
                    fileName += $"_{cus}";
                }
                return File(content, contentType, $"{fileName}.xlsx");
            }
        }
        [HttpGet]
        public ActionResult Details_KH(int? id)
        {
            if (id == null) return BadRequest();
            
            var orderPro = db.OrderPro.Include(o => o.Customer).FirstOrDefault(s => s.ID == id);
            //Include bằng join bảng
            var OrderDetails = db.OrderDetails.Include(o => o.Products).Where(s => s.IDOrder == id).ToList();
            
            if (orderPro == null || OrderDetails == null) return NotFound();
            
            string? refundStatus = db.SupportRequests
                .Where(s => s.IdRequest == orderPro.TrackingNumber)
                .Select(s => s.Status)
                .FirstOrDefault();
            string? description = db.SupportRequests
                .Where(s => s.IdRequest == orderPro.TrackingNumber)
                .Select(s => s.Description)
                .FirstOrDefault();
            ViewBag.RefundStatus = $"{refundStatus}|Lý do bạn yêu cầu hoàn tiền là: {description}";            
            OrderDetails_model OrderDetails_Model = new OrderDetails_model 
            { 
                OrderDetails = OrderDetails ?? new List<OrderDetails>(),
                OrderPro = orderPro ?? new OrderPro()
            };
            return View(OrderDetails_Model);
        }
        public class RefundRequestModel
        {
            public string? RefundCategory { get; set; } // "02" (Hoàn toàn phần) hoặc "03" (Hoàn một phần)
            public long Amount { get; set; }
            public string? OrderId { get; set; }
            public string? PayDate { get; set; } // Định dạng yyyyMMddHHmmss
            public string? User { get; set; }
        }
        
    }
}