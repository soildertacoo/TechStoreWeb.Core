using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TechStore.Models;
using OtpNet;
using QRCoder;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using TechStoreWeb.Core.Models.EmailSystem;

namespace TechStore.Controllers
{
    public class UserController : Controller
    {
        private readonly DBTechStoreEntities dbO_Cus;
        private readonly ApplicationDbContext _context;
        private readonly string MasterKey = "#@TechStoreWeb_BaomatSieuCapNasa_A234667@#";
        private string? otpEmailCode = null; 
        private EmailUtils emailUtils;

    // 2. Tạo hàm khởi tạo (Constructor) và yêu cầu hệ thống "tiêm" DbContext vào
        public UserController(DBTechStoreEntities dbContext, ApplicationDbContext appContext, IConfiguration configuration)
        {
            dbO_Cus = dbContext;
            _context = appContext;
            emailUtils = new EmailUtils(configuration);
        }
        
        #region Đăng ký
        [HttpGet]
        public ActionResult DangKy()
        {
            return View();
        }
        [HttpPost, ActionName("DangKy")]
        public ActionResult DangKy_XN(Customer customer)
        {
            if (ModelState.IsValid)
            {

                var item = dbO_Cus.Customers.Where(s => s.NameCus == customer.NameCus).FirstOrDefault();
                if (item != null)
                {
                    ViewBag.Error = "Đã có người đăng ký ";
                    return View();
                }

                customer.RegisteredDate = DateTime.Now;
                dbO_Cus.Customers.Add(customer);
                dbO_Cus.SaveChanges();
                ViewBag.Success = "Đăng ký thành công, bạn có thể đăng nhập ngay bây giờ";
                return View();
            }
            else
            {
                ViewBag.Error = "Đăng ký không thành công";
                return View();
            }
        }
        #endregion

        #region HÀM BĂM SHA1
        public class FactorSetupRequest
        {
            public string User { get; set; }
            public string Pass { get; set; }
        }
        private static byte[] SHA1Hash(string input)
        {
            // Dùng SHA1.Create() chuẩn của .NET Core thay vì SHA1Managed
            using (var sha = System.Security.Cryptography.SHA1.Create()) 
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(input);
                return sha.ComputeHash(bytes);
            }
        }
        #endregion
        #region Các hàm linh tinh quan trọng của user
        [HttpPost]
        public async Task<IActionResult> ToggleAnalytic([FromBody] System.Text.Json.JsonElement data)
        {
            // 1. Kiểm tra xác thực chuẩn xác
            if (User.Identity == null || !User.Identity.IsAuthenticated) 
            {
                return Json(new { success = false });
            }

            bool isToggle = data.GetProperty("isToogled").GetBoolean();
            string user = User.Identity.Name ?? "";
            var currentCus = dbO_Cus.Customers.FirstOrDefault(c => c.NameCus == user);
            if (currentCus == null) return Json(new { success = false });
            string pass = currentCus.PassCus ?? "";
            if (ValidateUser(user, pass) == null) return Json(new { success = false });

            //  Thẻ Claim (cập nhật trạng thái)
            var identity = (ClaimsIdentity)User.Identity;
            var isAuth = identity.FindFirst("TrangThaiAnalytic");
            if (isAuth != null) identity.RemoveClaim(isAuth);
            // Dán thẻ trạng thái mới
            identity.AddClaim(new Claim("TrangThaiAnalytic", isToggle ? "True" : "False"));
            // Đóng dấu Cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(identity)
            );
            //Lưu vào DB 
            currentCus.IsAnalyticEnabled = isToggle; 
            dbO_Cus.SaveChanges();

            return Json(new { success = true });
        }
        [HttpPost]
        public async Task<IActionResult> Factor2Setup([FromBody] System.Text.Json.JsonElement data)
        {
            // 1. Kiểm tra xác thực chuẩn xác
            if (User.Identity == null || !User.Identity.IsAuthenticated) 
            {
                return Json(new { success = false });
            }

            bool isToggle = data.GetProperty("isToogled").GetBoolean();
            string user = User.Identity.Name ?? "";
            
            var currentCus = dbO_Cus.Customers.FirstOrDefault(c => c.NameCus == user);
            if (currentCus == null) return Json(new { success = false });
            currentCus.Is2FAEnabled = isToggle; 

            string pass = currentCus.PassCus ?? "";
            if (ValidateUser(user, pass) == null) return Json(new { success = false });

            // 2. Xử lý Thẻ Claim (Căn cước)
            var identity = (ClaimsIdentity)User.Identity;
            
            // Cắt bỏ các thẻ cũ (Bao gồm cả thẻ QRImage nếu trước đó lỡ lưu)
            var isAuth = identity.FindFirst("TrangThaiAuth");
            if (isAuth != null) identity.RemoveClaim(isAuth);
            
            var isQR = identity.FindFirst("QRImage");
            if (isQR != null) identity.RemoveClaim(isQR); // Dọn dẹp sạch sẽ rác cũ

            // Dán thẻ trạng thái mới
            identity.AddClaim(new Claim("TrangThaiAuth", isToggle ? "True" : "False"));

            // Đóng dấu Cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(identity)
            );

           
            if (!isToggle) 
            {
                //Xóa dữ liệu 
                if(currentCus.TwoFactorSecret != null)
                {
                    currentCus.TwoFactorSecret = null; //Xóa secret key
                    dbO_Cus.SaveChanges(); // LƯU VÀO DATABASE!
                }
                return Json(new { success = true });
            }

            // ==========================================
            // CHỈ KHI BẬT 2FA MỚI CHẠY ĐOẠN DƯỚI NÀY
            // ==========================================
            string issuer = "TechStoreWeb";
            string cus = user + pass; 
            var secret = Base32Encoding.ToString(SHA1Hash(cus)); 
            string otpUrl = $"otpauth://totp/{issuer}:{user}?secret={secret}&issuer={issuer}&digits=6";
            string qrCodeImage = "";

            //Lưu vào sql, mã hóa secret và lưu thẳng vào sql
            currentCus.TwoFactorSecret = EncryptionHelper.EncryptStringToBytes(secret, MasterKey);
            dbO_Cus.SaveChanges(); // LƯU VÀO DATABASE!

            //  Dùng 'using' để tối ưu RAM 
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(otpUrl, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData); 
                byte[] qrCodeBytes = qrCode.GetGraphic(10);
                qrCodeImage = "data:image/png;base64," + Convert.ToBase64String(qrCodeBytes);
            }

            var model = new Face2Factor
            {
                Email = user,
                SecretKey = secret,
                QrCodeImage = qrCodeImage
            };

            
            return Json(new { success = true, factorModel = model });
        }
        public ActionResult ThongTinCaNhan()
        {
           // Cánh cửa thép: Khách vãng lai bay màu ngay lập tức
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("DangNhap");
            }
            
            string name = User.Identity.Name;
            ViewBag.Error = (string?)TempData["Loi"];
            var customer = dbO_Cus.Customers.FirstOrDefault(s => s.NameCus == name);            
            
            switch (customer)
            {
                case null:
                    ViewBag.Error = "Không tìm thấy thông tin khách hàng.";
                    return RedirectToAction("DangNhap");
                default:
                    // 1. Tính số đơn hàng và tổng tiền
                    ViewBag.SoDonHang = dbO_Cus.OrderPro
                        .Count(s => s.IDCus == customer.IDCus && s.Status != "Đã hủy" && s.Status != "Hủy đơn");

                    ViewBag.TongTien = dbO_Cus.OrderPro
                        .Where(s => s.IDCus == customer.IDCus && s.Status != "Đã hủy" && s.Status != "Hủy đơn" &&
                        s.PaymentStatus.Trim().ToLower() == "đã thanh toán")
                        .Sum(s => (decimal?)s.TotalAmount) ?? 0;

                    // 2. [THÊM MỚI] Lấy thông tin VIP của khách hàng
                    var vipInfo = dbO_Cus.VIPCustomers.FirstOrDefault(v => v.IDCus == customer.IDCus);
                    
                    if (vipInfo != null)
                    {
                        ViewBag.HangVip = vipInfo.VipTier;
                        ViewBag.NgayHetHanVip = vipInfo.ExpireVIPDate;
                    }
                    else
                    {
                        // Nếu khách hàng mới, chưa có dữ liệu trong bảng VIPCustomer thì để mặc định
                        ViewBag.HangVip = "Thành viên";
                        ViewBag.NgayHetHanVip = null;
                    }
                    break;
            }

            //Nếu không chưa có mua gì hết
            if ((int)ViewBag.SoDonHang == 0 || ViewBag.SoDonHang == null)
            {
                ViewBag.SoDonHang = 0;
                ViewBag.TongTien = 0;
            }
            
            return View(customer);
        }
        [HttpGet]
        public async Task<IActionResult> SetVIP(string name, String membership)
        {
            bool success_bool = false;
            var customer = dbO_Cus.Customers.FirstOrDefault(c => c.NameCus == name);
            if (customer != null)
            {
                customer.IsVIP = true;
                customer.MembershipLevel = membership.Trim().ToLower() != "no vip" ? membership : null;
                await dbO_Cus.SaveChangesAsync();
                success_bool = true;
                return Json(new { success = success_bool });
            }
            return Json(new { success = success_bool, message = "Không tìm thấy khách hàng để nâng cấp VIP hay lỗi hệ thống." });
        }
        #endregion

        #region Đăng nhập, đăng nhập bằng google
        [HttpGet]
        public IActionResult LoginWithGoogle()
        {
            var reDirect = new AuthenticationProperties { RedirectUri = "/" };
            // Lệnh Challenge này sẽ tự động sinh ra mã 'state' và chuyển hướng sang Google
            return Challenge(reDirect, GoogleDefaults.AuthenticationScheme);
        }
        [HttpGet]
        public ActionResult DangNhap()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index","Home");
            }
            return View();
        }
        public class LoginRequest 
        {
            public string NameCus { get; set; }
            public string PassCus { get; set; }
            public bool isRemember { get; set; }
        }
        public class VerifyOtpRequest
        {
            public string NameCus { get; set; } 
            public string OtpCode { get; set; }
            public bool isRemember { get; set; }
        }
        [HttpPost]
        public async Task<IActionResult> DangNhapXThuc([FromBody] LoginRequest data)
        {
           
                var cus = dbO_Cus.Customers.FirstOrDefault(c => c.NameCus == data.NameCus);

                // Nếu không tìm thấy User trong DB -> Chặn luôn
                if (cus == null)
                {
                    return Json(new { success = false, message = "Sai thông tin đăng nhập." });
                }

                // BƯỚC 2: Kiểm tra xem tài khoản có đang bị khóa hay không?
                if (cus.IsBanned == true)
                {
                    // Kiểm tra xem đã hết thời hạn 30 phút chưa
                    if (cus.BannedUntil != null && cus.BannedUntil > DateTime.Now)
                    {
                        return Json(new { success = false, message = $"Tài khoản của bạn đã bị khóa đến {cus.BannedUntil:HH:mm}. Lý do: {cus.ReasonBanned}" });
                    }
                    else
                    {
                            // Mở khóa tài khoản
                            cus.IsBanned = false;
                            cus.ReasonBanned = null;
                            cus.BannedUntil = null;
                            cus.FailedLoginAttempts = 0; // Reset lại số lần thử sau khi mở khóa
                            await dbO_Cus.SaveChangesAsync();
                    }
                }

                
                bool isPasswordCorrect = (cus.PassCus.Trim() == data.PassCus.Trim()) ? true : false; 

                if (!isPasswordCorrect)
                {
                   
                    //Thêm số lần thử vào database để tránh trường hợp tấn công bằng cách gửi nhiều request
                    cus.FailedLoginAttempts = (cus.FailedLoginAttempts ?? 0) + 1;
                    await dbO_Cus.SaveChangesAsync();

                    if (cus.FailedLoginAttempts >= 5)
                    {
                        cus.IsBanned = true;
                        cus.ReasonBanned = "Quá nhiều lần đăng nhập thất bại";
                        cus.BannedUntil = DateTime.Now.AddMinutes(30); // Thiết lập thời gian khóa 30 phút
                        
                        await dbO_Cus.SaveChangesAsync();
                        
                        return Json(new { success = false, message = "Tài khoản đã bị khóa 30 phút do nhập sai quá 5 lần. Vui lòng liên hệ hỗ trợ." });
                    }
                    
                    return Json(new { success = false, message = $"Sai thông tin đăng nhập. Bạn còn {5 - cus.FailedLoginAttempts} lần thử." });
                }
            
            cus.FailedLoginAttempts = 0; // Reset lại số lần thử sau khi mở khóa
            await dbO_Cus.SaveChangesAsync();
            
            //Kiểm tra có bật 2FA hay không
            if (cus.Is2FAEnabled) 
            {
                return Json(new { success = true, need2fa = true });
            }
            // Nếu ko bật OTP thì vào luôn
            await SignInUserInternal(cus, data.isRemember); 
            return Json(new { success = true, need2fa = false, redirectUrl = "/Home/Index" });
        }
        // Hàm Xác thực OTP (Chỉ chạy khi khách có 2FA)
        [HttpPost]
        public async Task<IActionResult> VerifyLoginOTP([FromBody] VerifyOtpRequest data)
        {
            var user = dbO_Cus.Customers.FirstOrDefault(c => c.NameCus == data.NameCus);

            if (user == null)
                return Json(new { success = false, message = "Lỗi hệ thống" });

            string decrypt = EncryptionHelper.DecryptStringFromBytes(user.TwoFactorSecret, MasterKey);

            if (decrypt == null)
                return Json(new { success = false, message = "Lỗi hệ thống hay là chưa có bật 2FA" });
            
            bool isValid = VerifyOTP(data.OtpCode, decrypt);
            if (!isValid) return Json(new { success = false, message = "Sai mã OTP, vui lòng kiểm tra lại" });
            
            await SignInUserInternal(user, data.isRemember); //Lưu cookie phiên đăng nhập
            return Json(new { success = true, redirectUrl = "/Home/Index"});
        }
        
        [HttpGet]
        public async Task<IActionResult> SendResetOTP(String email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Vui lòng nhập email." });
            }
            
            // Tìm khách hàng trước để lấy tên 
            var customer = _context.Customers.FirstOrDefault(c => c.EmailCus == email);
            if (customer == null)
            {
                return Json(new { success = false, message = "Email không tồn tại trong hệ thống." });
            }

            // Tạo mã OTP ngẫu nhiên
            string otpEmailCode = new Random().Next(100000, 999999).ToString();
            
            // Lưu vào Database
            _context.OTPModels.Add(new OTPModel
            {
                Email = email,
                NameCus = customer.NameCus, 
                OtpCode = otpEmailCode,     
                sendedTime = DateTime.Now,
                ExpirationTime = DateTime.Now.AddMinutes(5),
                typeOTP = 1
            });
            
            await _context.SaveChangesAsync();

            // Gửi Email
            await emailUtils.SendEmailAsync(
                email, 
                "Mã OTP đặt lại mật khẩu TechStore", 
                $"Mã OTP của bạn là: <b style='font-size:24px'>{otpEmailCode}</b>. Mã có hiệu lực trong 5 phút."
            );
            
            return Json(new { success = true, message = "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư." });
            
        }
        [HttpPost]
        public async Task<IActionResult> VerifyResetOTP([FromBody] OTPRequestModel otpIn)
        {
            var getCus = _context.OTPModels
                            .Where(x => x.Email == otpIn.emailInput && x.typeOTP == 1)
                            .OrderByDescending(x => x.sendedTime)
                            .FirstOrDefault();
            if (getCus == null)
            {
                return Json(new { success = false, message = "Lỗi xác thực OTP, vui lòng kiểm tra lại" });
            }
            bool isValid = (getCus.OtpCode == otpIn.otpInput && getCus.ExpirationTime > DateTime.Now) ? true : false ;
            if (!isValid) return Json(new { success = false, message = "Sai mã OTP hoặc đã hết hạn quá 5 phút, vui lòng kiểm tra lại" });
            return Json(new { success = true});
        }
        public bool VerifyOTP(String OtpCode, String SecretKey) 
        {
            //Khởi tạo đối tượng
            var totp = new Totp(Base32Encoding.ToBytes(SecretKey)); //(từ base32 sang byte , step: thời gian hiệu lực otp)
            //Nếu sai thì báo lỗi
            return totp.VerifyTotp(OtpCode, out long timeWindowUsed, new VerificationWindow(1, 1));
            
        }
        private async Task SignInUserInternal(Customer user, bool isRemember)
        {
            var userInform = new List<Claim>(){
                new Claim(ClaimTypes.Name, user.NameCus),
                new Claim("TrangThaiCookie", isRemember.ToString()),
                new Claim("TrangThaiAuth", user.Is2FAEnabled.ToString()),
                new Claim("TrangThaiAnalytic", user.IsAnalyticEnabled.ToString() )
            };
           
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(userInform, CookieAuthenticationDefaults.AuthenticationScheme)),
                new AuthenticationProperties
                {
                    IsPersistent = isRemember, 
                    ExpiresUtc = isRemember ? DateTimeOffset.UtcNow.AddDays(30) : null
                }
            );
        }
        #endregion

        #region  Bật cookie lưu phiên đăng nhập
        [HttpPost]
        public async Task <IActionResult> ToogleSession([FromBody] System.Text.Json.JsonElement data)
        {
            if (User.Identity?.IsAuthenticated == null) 
            {
                return Json(new { success = false });
            }
            bool isToggle = data.GetProperty("isToogled").GetBoolean();
            var identity = (ClaimsIdentity)User.Identity;
            var theCu = identity.FindFirst("TrangThaiCookie");
            // Nếu tìm thấy chữ cũ, lấy kéo "Cắt" nó vứt đi
            if (theCu != null)
            {
                identity.RemoveClaim(theCu);
            }
            //Đổi trạng thái 
            if (isToggle) identity.AddClaim(new Claim("TrangThaiCookie", "True"));
            else identity.AddClaim(new Claim("TrangThaiCookie", "False"));
            // 2. Tạo mốc thời gian mới (Gia hạn thêm 3 phút kể từ giây phút này)
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isToggle, 
                ExpiresUtc = isToggle ? DateTimeOffset.UtcNow.AddDays(30) : null
            };

            // Lấy lại đúng thông tin của khách đang đăng nhập ClaimsPrincipal((ClaimsIdentity)User.Identity)
            //  Đóng dấu lại Cookie và gửi xuống trình duyệt
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal((ClaimsIdentity)User.Identity), 
                authProperties
            );

            return Json(new { success = true });
        }
        #endregion
        
        #region Các hàm khác như đăng xuất,reset pass
        [HttpGet]
        public async Task<ActionResult> DangXuat()
        {
            //Clear cookie sau khi nhấn nút đăng xuất 
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (HttpContext.Session.IsAvailable)  HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
        private Customer? ValidateUser(string username, string password)
        {
            
            return dbO_Cus.Customers.FirstOrDefault(s => s.NameCus == username && s.PassCus == password);
        }
        [HttpGet]
        public ActionResult ResetPassword()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index","Home");
            }
            return View(); //Hiện view để đặt lại mật khẩu
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(Customer cus)
        {
            var user = dbO_Cus.Customers.FirstOrDefault(u => u.NameCus == cus.NameCus);

            if (user != null)
            {
                try
                {
                    user.PassCus = cus.PassCus;
                    dbO_Cus.Entry(user).State = EntityState.Modified;
                    dbO_Cus.SaveChanges();
                    ViewBag.Success = "Đã reset password thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    ViewBag.Error = "Reset không thành công. Có thể bị một vấn đề gì đó nên là có gì thử lại sao";
                }
            }
            else
            {
                ViewBag.Error = "Không reset được, vì không tồn tại người dùng này";
            }
            return View(); //Quay lại view cũ để nhập lại
        }
        #endregion
    }
    
} 
