using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using TechStore.Models;

namespace TechStore.Controllers
{
    public class ContactController : Controller
    {
        // GET: Contact
        private readonly DBTechStoreEntities dB;
        public ContactController(DBTechStoreEntities dbContext)
        {
            dB = dbContext;
        }
        [HttpGet]
        public ActionResult IndexSupport(int idOrder, String namecus, String email, String phone,String trackingNumber)
        {
            var date = dB.OrderPro.Where(op => op.ID == idOrder).Select(op => op.DateOrder).FirstOrDefault();//Lấy ngày từ OrderPro
            //Lấy support Request hiện tại nếu đã có ko thì tạo mới 
            var existRequest = dB.SupportRequests.Where(re => re.IdRequest == trackingNumber && re.CustomerName == namecus).FirstOrDefault();
            var supportRequest = existRequest ?? new SupportRequest
            {
                IdRequest = trackingNumber,
                CustomerName = namecus,
                Email = email,
                PhoneNumber = phone,
                OrderNumber = trackingNumber,
                PurchaseDate = date != null ? Convert.ToDateTime(date).ToString("dd-MM-yyyy") 
                        : "01-01-2000"
            };
            
            ViewData["ProductsNames"] = dB.OrderPro.Where(op => op.ID == idOrder).Join
                (dB.OrderDetails,op => op.ID, od => od.IDOrder, 
                (op, od) => new { od.IDProduct }).
                Select(od => dB.Products.Where(p => p.ProductID == od.IDProduct).Select(p => p.NamePro).FirstOrDefault()).ToList()
                ;
            return View(supportRequest);
        }
        [HttpPost]
        [ValidateAntiForgeryToken] // Nên thêm cái này để bảo mật Form
        public ActionResult SendSupport(SupportRequest support)
        {
            if (ModelState.IsValid)
            {
                try 
                {
                    // BƯỚC 1: Mapping dữ liệu
                    var dbSupportRequest = new SupportRequest
                    {
                        IdRequest = support.IdRequest, 
                        CustomerName = support.CustomerName,
                        Email = support.Email,
                        PhoneNumber = support.PhoneNumber,
                        OrderNumber = support.OrderNumber,
                        PurchaseDate = support.PurchaseDate,
                        ProductsName = support.ProductsName,
                        RequestType = support.RequestType,
                        Description = support.Description, 
                        RequestDate = DateTime.Now,
                        Status = "Sended"
                    };

                    // Thêm vào Database
                    dB.SupportRequests.Add(dbSupportRequest);
                    dB.SaveChanges();
                    
                    // Dùng TempData để giữ thông báo khi chuyển hướng
                    TempData["notify_sendRequest"] = "success|Đã gửi yêu cầu hỗ trợ thành công. Chúng tôi sẽ liên hệ sớm nhất!";
                }
                catch (Exception ex)
                {
                    //nếu thất bại thì trả về view
                   TempData["notify_sendRequest"] = "error|Đã gửi thất bại. Lỗi hệ thống." + ex;
                }
            }
            else
            {
                TempData["notify_sendRequest"] = "error|Đã gửi thất bại. Lỗi hệ thống khi gửi lên server.";
            }
           
            return View(support);
        }
        private string GenerateRequestId()
        {
            //Ngẫu nhiên 5 chữ cái từ A-Z
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var random = new Random();
            var result = new String(Enumerable.Repeat(letters,3).Select(s => s[random.Next(s.Length)]).ToArray());
            // REQ + RandomNumber + Chữ cái + DateTime.Now
            return "REQ" + new Random().Next(1,999).ToString() + result;
        }
    }
}