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

namespace TechStore.Controllers
{
    public class UserController : Controller
    {
        private readonly DBTechStoreEntities dbO_Cus;
        private readonly ApplicationDbContext _context;
        private readonly string MasterKey = "#@TechStoreWeb_BaomatSieuCapNasa_A234667@#";

    // 2. Tạo hàm khởi tạo (Constructor) và yêu cầu hệ thống "tiêm" DbContext vào
        public UserController(DBTechStoreEntities dbContext, ApplicationDbContext appContext)
        {
            dbO_Cus = dbContext;
            _context = appContext;
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
            string ? session = HttpContext.Session.GetString("DaDangNhap") != null ?  
            HttpContext.Session.GetString("DaDangNhap") : User.Identity.Name;

            if (session== null)
            {
                return RedirectToAction("DangNhap");
            }
            string name = session;
            ViewBag.Error = (string?)TempData["Loi"];
            var customer = dbO_Cus.Customers.FirstOrDefault(s => s.NameCus == name);
            //Đếm số đơn hàng của khách hàng
            
           switch (customer)
            {
                case null:
                    ViewBag.Error = "Không tìm thấy thông tin khách hàng.";
                    return RedirectToAction("DangNhap");
                default:
                    ViewBag.SoDonHang = dbO_Cus.OrderPro.Where(s => s.IDCus == customer.IDCus).Count();
                    ViewBag.TongTien = dbO_Cus.OrderPro.Where(s => s.IDCus == customer.IDCus).Sum(s => s.TotalAmount);
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
        [HttpPost]
        public ActionResult SetVIP(int id, String membership, String message)
        {
            System.Diagnostics.Debug.WriteLine(message);
            bool success_bool = false;
            var customer = dbO_Cus.Customers.FirstOrDefault(c => c.IDCus == id);
            if (customer != null)
            {
                customer.IsVIP = true;
                customer.MembershipLevel = membership ;
                dbO_Cus.SaveChanges();
                success_bool = true;
                return Json(new { success = success_bool });
            }
            return Json(new { success = success_bool });
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
            var user = ValidateUser(data.NameCus, data.PassCus);
            
            if (user == null && signAttempt < 5) // Giới hạn 5 lần thử
            {
                //Nếu quá 5 lần thì khóa hẳn tài khoản
                if (signAttempt >= 4) 
                {
                    var cus = dbO_Cus.Customers.FirstOrDefault(c => c.NameCus == data.NameCus);
                    if (cus != null)
                    {
                        cus.IsBanned = true; // Cột IsBanned trong database để đánh dấu tài khoản bị khóa
                        cus.ReasonBanned = "Quá nhiều lần đăng nhập thất bại"; // Lý do khóa tài khoản
                        await dbO_Cus.SaveChangesAsync();
                    }
                    return Json(new { success = false, message = "Tài khoản đã bị khóa do quá nhiều lần đăng nhập thất bại. Vui lòng liên hệ bộ phận hỗ trợ." });
                }
                signAttempt++;
                return Json(new { success = false, message = $"Sai thông tin đăng nhập. Còn {5 - signAttempt} lần thử." });
            }
            //Đúng thì reset lại biến đếm
            signAttempt = 0;
            //Kiểm tra có bật 2FA hay không
            if (user.Is2FAEnabled) 
            {
                return Json(new { success = true, need2fa = true });
            }

            // Nếu ko bật OTP thì vào luôn
            await SignInUserInternal(user, data.isRemember); 
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
