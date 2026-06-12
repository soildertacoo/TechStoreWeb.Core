using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Bắt buộc cho Session
using TechStore.Models;
using TechStoreWeb.Core.Helpers;
using TechStoreWeb.Core.PayModel;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.WebUtilities; // Nơi chứa SessionExtensions
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using TechStoreWeb.Core.ShippingServices;
using TechStore.Models.ModelShipping;
using System.Runtime.InteropServices.Marshalling;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office.CustomUI;
namespace TechStore.Controllers
{
    public class CartController : Controller
    {
        private readonly DBTechStoreEntities db;
        private readonly ApplicationDbContext _context;
        private IConfiguration _configuration; //Lấy them số từ appSetting.json

        // Constructor yêu cầu hệ thống "tiêm" DbContext vào
        public CartController(DBTechStoreEntities dbContext, ApplicationDbContext appContext,
        IConfiguration configuration)
        {
            db = dbContext;
            _context = appContext;
            _configuration = configuration;

        }
        public class JsonData 
        {
            public List<CartItem>? MyCart { get; set; }
            public int? productID {get;set;}
        }

        private string CartSessionKey => "GioHang" + (User.Identity?.Name ?? "Demo");

        #region Xử lý Session Giỏ Hàng
        public List<CartItem> GetCart()
        {
            // Lấy từ DB ra + tên người dùng dùng giở hàng đó
            string? userLogged = User.Identity.IsAuthenticated ? User.Identity.Name : "" ;
            return _context.CartItems.Where(x => x.userLogged == userLogged).ToList() ?? new List<CartItem>();
        }

        // Lưu giỏ hàng vào Session sau khi thay đổi
        private async Task DBCart(CartItem cart , string command)
        {
            // Thêm một sản phẩm cartItem vào DBCart 
            if(command == "add") _context.CartItems.Add(cart);
            if (command == "remove") _context.Remove(cart);
            if (command == "update") _context.Entry(cart).State = EntityState.Modified; //cap nhat san pham

            await _context.SaveChangesAsync(); //Lưu dữ liệu lại 
        }

        #endregion

        public ActionResult RebackDetails(int id)
        {
            return RedirectToAction("Details", "CustomerPro", new { id = id });
        }
        [HttpGet]
        public ActionResult CartDetails()
        {
            List<CartItem> myCart = GetCart();
            
            ViewBag.Total = TotalMoney();
            return View(myCart); 
        }
        [HttpGet]
        public async Task<IActionResult> ShowCart()
        {
            List<CartItem> myCart = GetCart();
            
            // Kiểm tra an toàn: Nếu null thì coi như giỏ hàng rỗng
            if (myCart == null) 
            {
                return Json(new { success = false, message = "Giỏ hàng rỗng" });
            }

            // Nhét cả danh sách giỏ hàng (cart) VÀ tổng tiền (total) vào trong JSON
            return Json(new { 
                success = true, 
                cart = myCart, 
                total = TotalMoney() 
            });
        }
        [HttpPost]
        public async Task<IActionResult> AddToShowCart(int id, int quantity)
        {
            var inventory = _context.Inventories.FirstOrDefault(x => x.ProductID == id);

            if (inventory == null)
            {
                TempData["Error"] = "Sản phẩm hiện không có trong kho!";
                return RedirectToAction("Details", "CustomerPro", new { id });
            }

            if (quantity > inventory.StockQuantity)
            {
                TempData["Error"] =
                $"Chỉ còn {inventory.StockQuantity} sản phẩm trong kho!";

                return RedirectToAction("Details", "CustomerPro", new { id });
            }


            if (quantity <= 0 || quantity > 999) return RedirectToAction("Details", "CustomerPro", new { id = id });
            var myCart = GetCart(); var cartitem = new CartItem(); string command = "add";

            CartItem? currentProducts = myCart.FirstOrDefault(p => p.ProductID == id);
            if (currentProducts == null) //Thêm nếu chưa có
            {
                var addPro = db.Products.SingleOrDefault(p=> p.ProductID == id);
                if (addPro == null) return NotFound("notFound_405: Có lỗi khi thêm vào giỏ hàng");
                cartitem = new CartItem() { 
                    IDCart = 0, // Do Idcart dùng cột tự tăng
                    ProductID =  addPro.ProductID,
                    NamePro = addPro.NamePro,
                    ImagePro = addPro.ImagePro,
                    Price = addPro.Price,
                    Number = quantity,
                    userLogged = User.Identity.IsAuthenticated ? User.Identity.Name : "" 
                };
            }
            else //Có thì cập nhật lên 
            {
                currentProducts.Number += quantity; 
                cartitem = currentProducts;
                command = "update";
            }
            
            await DBCart(cartitem, command); // LƯU SAU KHI THÊM Hay Sửa 
            return RedirectToAction("Details", "CustomerPro", new { id = id });
        }
        #region Chức năng Thanh Toán
        [HttpGet]
        public ActionResult PaymentCart()
        {

            string ? usermodel = User?.Identity?.Name?.ToString();
            if (usermodel == null) return RedirectToAction("DangNhap", "User");
            var cus = db.Customers.FirstOrDefault(s => s.NameCus == usermodel);
            if (cus == null) return RedirectToAction("DangNhap", "User");

            List<CartItem> myCart = GetCart();
            if (!myCart.Any()) return RedirectToAction("ShowCart");

            
            //var providers =  db.ShippingProvider.Where(item => item.IsActive).ToList();
            var providers = new List<ShippingProvider>();
            ViewBag.Total = TotalMoney();
            var checkout = new Payment
            {
                mycart = myCart,
                Customers = cus,
                Providers = providers
            };
            return View(checkout);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> PaymentCart(Payment model)
        {
            // 
            List<CartItem> myCart = GetCart();
            if (!myCart.Any()) return RedirectToAction("Index","Home");

            string? usermodel = User?.Identity?.Name;
            if (string.IsNullOrEmpty(usermodel)) 
                return RedirectToAction("DangNhap", "User");

            var cus = db.Customers.FirstOrDefault(s => s.NameCus == usermodel);
            if (cus == null) 
                return RedirectToAction("DangNhap", "User");
            
            bool isValid = true;

            var nameCheck = ValidateName(cus.NameCus);
            if (!nameCheck.IsValid) 
            { 
                ModelState.AddModelError("Customers.NameCus", nameCheck.Error); // Thêm báo lỗi vào validation trong cshtml
                isValid = false; 
            }

            // var phoneCheck = ValidatePhoneNumber(cus.PhoneCus);
            // if (!phoneCheck.IsValid) 
            // { 
            //     ModelState.AddModelError("Customers.PhoneCus", phoneCheck.Error); 
            //     isValid = false; 
            // }

            var emailCheck = ValidateEmail(cus.EmailCus);
            if (!emailCheck.IsValid) 
            { 
                ModelState.AddModelError("Customers.EmailCus", emailCheck.Error); 
                isValid = false; 
            }
            //Join thành một địa chỉ hoàn chỉnh
            string address = string.Join(", ", new[] { cus.StreetAddress, cus.Ward, cus.City }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var addressCheck = ValidateAddress(address);
            if (!addressCheck.IsValid) 
            { 
               
                ModelState.AddModelError("Customers.StreetAddress", addressCheck.Error); 
                isValid = false; 
            }

            if (!isValid) 
            {
                var checkout = new Payment { mycart = myCart, Customers = cus };
                ViewBag.Total = TotalMoney();
                return View(checkout);
            }


            //Xử lý thanh toán tiền mặt, vnPay
            string trackingNumber = GenerateTrackingNumber();
            bool isCardPayment = model.Order?.PaymentMethod == "1";

            // XỬ LÝ LƯU DATABASE VỚI TRANSACTION Nếu là thanh toán bằng tiền mặt 
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    decimal? shippingCost = TotalMoney() * 0.05m;
                    int idCus = cus.IDCus;
                    var order = new OrderPro
                    {
                        IDCus = idCus,
                        DateOrder = DateTime.Now,
                        TotalAmount = TotalMoney() + shippingCost,
                        Status = "Đang xử lý",
                        PaymentMethod = isCardPayment == true ? "Thanh toán qua thẻ ngân hàng" : "Thanh toán khi nhận hàng",
                        TrackingNumber = trackingNumber,
                        AddressDeliverry = address,
                        DeliveryDate = DateTime.Now.AddDays(8),
                        ShippingCost = shippingCost,
                        PaymentStatus =  "Chưa thanh toán"
                    };

                    db.OrderPro.Add(order);
                    db.SaveChanges(); 

                    //Convert nguyên list (lưu có tùy chọn) để lưu thẳng vào sql
                    var orderDetailsList = myCart.Select(item => new OrderDetails
                    {
                        IDOrder = order.ID,
                        IDProduct = item.ProductID,
                        Quantity = item.Number,
                        UnitPrice = (double)item.Price,
                        Note = "Đơn hàng đang chờ xử lý"
                    }).ToList();

                    db.OrderDetails.AddRange(orderDetailsList); //Nếu lưu nguyên list vào sql
                    db.SaveChanges(); 

                    foreach (var item in myCart)
                    {
                        // Giảm tồn kho
                        var inventory = db.Inventories
                        .FirstOrDefault(x => x.ProductID == item.ProductID);

                        if (inventory == null || inventory?.StockQuantity < item.Number )
                        {
                            throw new Exception($"Sản phẩm không đủ để đặt hàng hoặc ko tồn tại.");
                        }

                        inventory?.StockQuantity -= item.Number;
                    }
                    db.CartItems.RemoveRange(myCart);
                    db.SaveChanges();
                    // Xác nhận toàn bộ quá trình thành công
                    transaction.Commit();
                    if (isCardPayment){ 
                        //Chạy thanh toán vnpay
                        return VnPayCheckout(trackingNumber);
                    }

                    // //Khi thanh toán qua nhận hàng COD 
                    // try {
                    //     createOrderProvider(order);
                    // }
                    // catch(Exception ex)
                    // {
                    //     Console.WriteLine("6736: Có lỗi khi gửi từ server đvvc " + ex); 
                    // }
                    return View("PaymentSuccess", new { RspCode = "00", Message = "Confirm Success" }); //Thanh toan bang tien mat
                }
                catch (Exception ex)
                {
                    // Nếu có lỗi ở bất kỳ dòng nào, quay ngược (Rollback) toàn bộ dữ liệu, không tạo Đơn hàng ma
                    transaction.Rollback();
                    Console.WriteLine("DB_ERROR_CART:" + ex);
                    ViewBag.Error = "Đặt hàng không thành công: Vui lòng thử lại sau."; // Ẩn ex.Message với người dùng thực tế
                    model.mycart = myCart;
                    model.Customers = cus;
                    ViewBag.Total = TotalMoney();
                    return View(model);
                }
            }
        }
        #region Thanh toán VNPAY
        [HttpGet]
        public async Task<IActionResult> PaymentSuccess()
        {
            //Lấy sau dấu ? của đường link có giá trị được gắn sẵn đưa vào trong hàm
            //Gọi IPN giả lập, dùng Task để gọi song song và chạy thêm hàm 

            if (Request.Query.Count > 0)
            {
                string queryString = Request.QueryString.ToString(); // ?giastri=bien&&giatri2=bien => queryString
                string ipnUrl = $"{Request.Scheme}://{Request.Host}/Cart/VnPayIPN{queryString}";
                
                _= Task.Run( async () => {
                    using(var httpClient = new HttpClient())
                    {
                        try {await httpClient.GetAsync(ipnUrl);
                        Console.WriteLine("Đang chạy IPN");}
                        catch
                        {
                            Console.WriteLine("Bị lỗi khi gọi IPN"); return;
                        }
                    }
                });
                
                // 1. Lấy Secret Key từ appsettings.json
                string? vnp_HashSecret = _configuration["PayAPI:vnPay:vnp_HashSecret"] ?? "";
                
                var vnpayData = Request.Query;
                VnPayLibrary vnpay = new VnPayLibrary();

                // Nạp data vào thư viện
                foreach (var (key, value) in vnpayData)
                {
                    if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(key, value.ToString());
                    }
                }

                // 2. Trích xuất dữ liệu
                string orderId = vnpay.GetResponseData("vnp_TxnRef");
                long vnp_Amount = Convert.ToInt64(vnpay.GetResponseData("vnp_Amount")); 
                long vnpayTranId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                string vnp_SecureHash = Request.Query["vnp_SecureHash"].ToString();

                // 3. Kiểm tra chữ ký
                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                if (checkSignature)
                {
                    // 4. Lấy đơn hàng thật từ Database của bạn
                    var order = db.OrderPro.FirstOrDefault(o => o.TrackingNumber == orderId);
                    
                    if (order != null)
                    {
                        // Kiểm tra số tiền có khớp không
                        // Ép kiểu thành long(Convert.ToInt64 * 100) nếu TotalAmount của bạn là decimal/double
                        long amount = Convert.ToInt64((order.TotalAmount - order.ShippingCost) * 100);
                        if (amount == vnp_Amount) 
                        {
                            // Kiểm tra trạng thái đơn hàng (Chỉ cập nhật nếu đơn đang là "Chưa thanh toán")
                            if (order.PaymentStatus == "Chưa thanh toán")
                            {
                                string rspCode = "";
                                string message = "";
                                if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                                {
                                    // Thanh toán thành công
                                    order.PaymentStatus = "Đã thanh toán";
                                    Console.WriteLine($"[IPN] Thanh toán thành công đơn {orderId}");
                                    rspCode = "00"; message="";
                                    // //Tao don GHN , GHTK , ...
                                    // try {
                                    //     createOrderProvider(order);
                                    // }
                                    // catch(Exception ex)
                                    // {
                                    //     Console.WriteLine("3005: Có lỗi khi gửi từ server đvvc " + ex); 
                                    // }

                                }
                                else
                                {
                                    // Thanh toán thất bại
                                    order.PaymentStatus = "Chưa thanh toán";
                                    Console.WriteLine($"[IPN] Thanh toán lỗi đơn {orderId}. Mã: {vnp_ResponseCode}");
                                    rspCode = "99"; message="Checkout failed";
                                }

                                // LƯU DATABASE
                                db.Entry(order).State = EntityState.Modified;
                                db.SaveChanges();

                                // trả về view
                                return View(new { RspCode = rspCode, Message = message });
                            }
                            else
                            {
                                return View(new { RspCode = "02", Message = "Order already confirmed" });
                            }
                        }
                        else
                        {
                            Console.WriteLine("Gía tiền có vấn đề : " + amount);
                            return View(new { RspCode = "04", Message = "Invalid amount" });
                        }
                    }
                    else
                    {
                        return View(new { RspCode = "01", Message = "Order not found" });
                    }
                }
                else
                {
                    Console.WriteLine($"[IPN] Lỗi sai chữ ký: {Request.Path}{Request.QueryString}");
                    return View(new { RspCode = "97", Message = "Invalid signature" });
                }
            }

            return View(new { RspCode = "99", Message = "Thanh toán chưa được thực hiện" });
           
        }
        public async void createOrderProvider(OrderPro order)
        {
            using (var httpClient = new HttpClient())
            {
                string shipping = "";
                switch (shipping)
                {
                    case "GHN": 
                        var ghnHelper = new GhnShippingService(httpClient);
                        var provider = db.ShippingProvider.FirstOrDefault(provider => provider.ProviderCode == shipping);
                        //Tao don ghn gui len server 
                        order.ShippingCode = await ghnHelper.CreateGHN(order, provider) ?? "";
                        break;
                    case "GHTK": 
                        break;
                    default : break;
                }
            }
        }
        private IActionResult VnPayCheckout(string trackingNumber)
        {
            //Get Config Info
            //Dùng QueryHelper để ghép link truy cập tới controller trong trình duyệt một cách chính xác
            // var param = new Dictionary<string, string>
            // {
            //     {"trackingNumber", trackingNumber}
            // };
            // string? postURL = _configuration["PayAPI:vnPay:vnp_Returnurl"] ?? "";
            string? vnp_Returnurl = _configuration["PayAPI:vnPay:vnp_Returnurl"] ?? ""; //URL nhan ket qua tra ve 
            string? vnp_Url = _configuration["PayAPI:vnPay:vnp_Url"]; //URL thanh toan cua VNPAY 
            string? vnp_TmnCode = _configuration["PayAPI:vnPay:vnp_TmnCode"]; //Ma định danh merchant kết nối (Terminal Id)
            string? vnp_HashSecret = _configuration["PayAPI:vnPay:vnp_HashSecret"]; //Secret Key

            //Build URL for VNPAY
            VnPayLibrary vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode ?? "");
            vnpay.AddRequestData("vnp_Amount", Convert.ToInt64(TotalMoney() * 100).ToString()); //Số tiền thanh toán. Số tiền không mang các ký tự phân tách thập phân, phần nghìn, ký tự tiền tệ. Để gửi số tiền thanh toán là 100,000 VND (một trăm nghìn VNĐ) thì merchant cần chuyển số tiền sang định dạng long và nhân nó thêm 100 lần (khử phần thập phân), sau đó gửi sang VNPAY là: 10000000
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(HttpContext));
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang co ma don hang la {trackingNumber}");
            vnpay.AddRequestData("vnp_OrderType", "other"); //default value: other

            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl ?? "");
            vnpay.AddRequestData("vnp_TxnRef", trackingNumber); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày

            //Add Params of 2.1.0 Version
            //Billing
            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url ?? "", vnp_HashSecret ?? "");
            Console.WriteLine($"VNPAY URL: {paymentUrl}");
            return Redirect(paymentUrl);
        }
        //Hàm IPN để xử lý dữ liệu tra rveef 
       
        [HttpGet]
        public async Task<IActionResult> VnPayIPN()
        {
            if (Request.Query.Count > 0)
            {
                // 1. Lấy Secret Key từ appsettings.json
                string? vnp_HashSecret = _configuration["VnpayConfig:vnp_HashSecret"];
                
                var vnpayData = Request.Query;
                VnPayLibrary vnpay = new VnPayLibrary();

                // Nạp data vào thư viện
                foreach (var (key, value) in vnpayData)
                {
                    if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(key, value.ToString());
                    }
                }

                // 2. Trích xuất dữ liệu
                string orderId = vnpay.GetResponseData("vnp_TxnRef");
                long vnp_Amount = Convert.ToInt64(vnpay.GetResponseData("vnp_Amount")); 
                long vnpayTranId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                string vnp_SecureHash = Request.Query["vnp_SecureHash"].ToString();

                // 3. Kiểm tra chữ ký
                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                if (checkSignature)
                {
                    // 4. Lấy đơn hàng thật từ Database của bạn
                    var order = db.OrderPro.FirstOrDefault(o => o.TrackingNumber == orderId);
                    
                    if (order != null)
                    {
                        // Kiểm tra số tiền có khớp không (Tránh hacker sửa số tiền thành 1 VNĐ)
                        // Ép kiểu về long nếu TotalAmount của bạn là decimal/double
                        if (order.TotalAmount == vnp_Amount) 
                        {
                            // Kiểm tra trạng thái đơn hàng (Chỉ cập nhật nếu đơn đang là "Chưa thanh toán")
                            if (order.PaymentStatus == "Chưa thanh toán")
                            {
                                if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                                {
                                    // Thanh toán thành công
                                    order.PaymentStatus = "Đã thanh toán";
                                    Console.WriteLine($"[IPN] Thanh toán thành công đơn {orderId}");
                                }
                                else
                                {
                                    // Thanh toán thất bại
                                    order.PaymentStatus = "Thanh toán lỗi";
                                    Console.WriteLine($"[IPN] Thanh toán lỗi đơn {orderId}. Mã: {vnp_ResponseCode}");
                                }

                                // LƯU DATABASE
                                db.SaveChanges();

                                // Trong .NET Core, dùng return Ok() để trả về JSON cho VNPay, KHÔNG dùng Response.Write
                                return View(new { RspCode = "00", Message = "Confirm Success" });
                            }
                            else
                            {
                                return View(new { RspCode = "02", Message = "Order already confirmed" });
                            }
                        }
                        else
                        {
                            return View(new { RspCode = "04", Message = "Invalid amount" });
                        }
                    }
                    else
                    {
                        return View(new { RspCode = "01", Message = "Order not found" });
                    }
                }
                else
                {
                    Console.WriteLine($"[IPN] Lỗi sai chữ ký: {Request.Path}{Request.QueryString}");
                    return View(new { RspCode = "97", Message = "Invalid signature" });
                }
            }

            return View(new { RspCode = "99", Message = "Input data required" });
        }
        #endregion

        //Tuple
       private (bool IsValid, string Error) ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3 || name.Length > 50)
                return (false, "Tên không hợp lệ, phải từ 3 đến 50 ký tự.");

            // \p{L} cho chữ cái (mọi ngôn ngữ), \s cho khoảng trắng
            string pattern = @"^[\p{L}\s]+$"; 
            if (!Regex.IsMatch(name, pattern))
                return (false, "Tên không hợp lệ, không được chứa số hoặc ký tự đặc biệt.");

            return (true, string.Empty);
        }

        private (bool IsValid, string Error) ValidateAddress(string address)
        {
            // Đổi thành IsNullOrWhiteSpace cho an toàn tuyệt đối
            if (string.IsNullOrWhiteSpace(address) || address.Length > 150)
                return (false, "Địa chỉ không được để trống và không quá 150 ký tự.");

            // \p{L} (chữ cái), 0-9 (số), \s (khoảng trắng), và các dấu , . - /
            string pattern = @"^[\p{L}0-9\s,.\-/]+$";
            if (!Regex.IsMatch(address, pattern))
                return (false, "Địa chỉ chỉ được chứa chữ, số, khoảng trắng và các dấu , . - /");

            return (true, string.Empty);
        }

        private (bool IsValid, string Error) ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) 
                return (false, "Email không được để trống.");

            // Regex cơ bản, đủ tốt để chặn các lỗi gõ sai thông thường
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(email, pattern)) 
                return (false, "Email không đúng định dạng.");

            return (true, string.Empty);
        }

        private (bool IsValid, string Error) ValidatePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) 
                return (false, "Số điện thoại không được để trống.");

            // Bắt buộc bắt đầu bằng số 0, theo sau là đúng 9 chữ số
            string pattern = @"^0\d{9}$";
            if (!Regex.IsMatch(phone, pattern)) 
                return (false, "Số điện thoại phải bao gồm 10 chữ số và bắt đầu bằng số 0.");

            return (true, string.Empty);
        }

        private string GenerateTrackingNumber()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 10).Select(s => s[random.Next(s.Length)]).ToArray());
        }
        #endregion

        #region Thao tác Giỏ Hàng (Thêm, Xóa, Sửa)
        public async Task<ActionResult> UpdateCart([FromForm] JsonData data)
        {
            
            var cartItem = _context.CartItems.FirstOrDefault(item => item.ProductID == data.productID);
            var inventory = db.Inventories
                        .FirstOrDefault(x => x.ProductID == data.productID);
            try
            {
                if (cartItem == null || inventory == null) throw new Exception("Có lỗi xảy ra khi thêm giỏ hàng");
                if (cartItem.Number < 0 || cartItem.Number > inventory.StockQuantity) throw new Exception("Có lỗi xảy ra khi thêm giỏ hàng vì kho đã hết");
                
                cartItem.Number++;
                await DBCart(cartItem, "update"); // LƯU SAU KHI SỬA xong giỏ hàng, can thiệp trực tiếp vào list kia
            }
            catch
            {
                return Json(new { 
                    success = false
                });
            }
            //Mục đích cập nhật lại số lượng
            return Json(new { 
                success = true, 
                cart = GetCart(), 
                total = TotalMoney() 
            });
        }

        public async Task<IActionResult> RemoveSubtractionCart([FromForm] JsonData data)
        {
            
            CartItem? currentProducts = _context.CartItems.FirstOrDefault(p => p.ProductID == data.productID);

            if (currentProducts != null)
            {
                currentProducts.Number--;
                if (currentProducts.Number <= 0)
                {
                    //Gỡ trực tiếp khỏi sql 
                    await DBCart(currentProducts, "remove");
                }
                await DBCart(currentProducts, "update");
            }
            else
            {
                return Json(new { 
                    success = false
                });
            }

            return Json(new { 
                success = true
            });
        }

        public async Task<IActionResult> RemoveFromCart([FromBody] JsonData data)
        {
            // Lấy giỏ hàng hiện tại
    
            // Tìm sản phẩm cần xóa
            CartItem? currentItem = _context.CartItems.SingleOrDefault(p => p.ProductID == data.productID);

            if (currentItem != null)
            {
               await DBCart(currentItem, "remove");
            }
            else
            {
                return Json(new { 
                    success = false
                });
            }

      
            return Json(new { 
                success = true, 
                cart = GetCart(), 
                total = TotalMoney() 
            });
        }

        [HttpPost]
        public async Task<ActionResult> UpdateQuantity(int ProductID, int newQuantity)
        {
            
            CartItem? currentProducts = _context.CartItems.FirstOrDefault(p => p.ProductID == ProductID);

            if (currentProducts != null)
            {
                if (newQuantity > 0){
                    currentProducts.Number = newQuantity;
                    await DBCart(currentProducts,"update");

                }
                else {
                    await DBCart(currentProducts,"remove");
                }
            }
            return RedirectToAction("ShowCart");
        }
        #endregion

        #region Tính Toán
        public ActionResult CartPartital() // Mình vẫn giữ nguyên tên hàm của bạn
        {
            ViewBag.TongSoLuong = TotalQuantity();
            return PartialView();
        }

        public JsonResult GetCartQuantity()
        {
            int totalQuantity = TotalQuantity();
            return Json(new { quantity = totalQuantity }); 
        }

        public decimal TotalMoney()
        {
            return GetCart().Sum(item => item.FinalPrice()); 
        }

        public int TotalQuantity()
        {
            return GetCart().Sum(item => item.Number); 
        }
        #endregion
    }
}