using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using TechStore.Models;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace TechStore.Controllers
{
    public class CustomerController : Controller
    {
        private readonly DBTechStoreEntities dBO;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

    // 2. Tạo hàm khởi tạo (Constructor) và yêu cầu hệ thống "tiêm" DbContext vào
    public CustomerController(DBTechStoreEntities dbContext, ApplicationDbContext appContext, 
    IWebHostEnvironment env)
    {
        dBO = dbContext;
        _context = appContext;
        _env = env;
    }

        // GET: Customer
        public ActionResult Index()
        {
            var items = dBO.Customers.ToList();
            return View(items);
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                dBO.Customers.Add(customer);
                dBO.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(customer);
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            var item = dBO.Customers.FirstOrDefault(s => s.IDCus == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult ActionDelete(int id)
        {
            var item = dBO.Customers.FirstOrDefault(s => s.IDCus == id);
            if (item != null)
            {
                dBO.Customers.Remove(item);
                dBO.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var item = dBO.Customers.FirstOrDefault(s => s.IDCus == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Edit")]
        public ActionResult Edit_UP(Customer customer)
        {
            if (ModelState.IsValid)
            {
                var existingCustomer = dBO.Customers.FirstOrDefault(s => s.IDCus == customer.IDCus);
                if (existingCustomer != null)
                {
                    dBO.Entry(existingCustomer).CurrentValues.SetValues(customer);
                    try
                    {
                        dBO.SaveChanges();
                        return RedirectToAction("Index");
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        ViewBag.Error = "Lỗi cập nhật. Khách hàng này có thể đã bị sửa hoặc xóa bởi người khác. Vui lòng thử lại.";
                    }
                }
                else
                {
                    ViewBag.Error = "Không tìm thấy khách hàng.";
                }
            }
            return View(customer);
        }

        [HttpGet]
        public ActionResult Edit_KH(int id)
        {
            var item = dBO.Customers.FirstOrDefault(s => s.IDCus == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Edit_KH")]
        [ValidateAntiForgeryToken]
        public ActionResult EditKH(Customer customer, string? dateOBirth)
        {

            // 2. Kiểm tra tồn tại
            var existingCustomer = dBO.Customers.FirstOrDefault(s => s.IDCus == customer.IDCus);
            //Convert lại lịch sang định dạng tiêu chuẩn sql nếu có đổi cái gì đó
            if (!string.IsNullOrWhiteSpace(dateOBirth))
            {
                //Chuyển convert từ date VN sang date formatSQL(yy-MM-đd)
                string [] format = { "dd/MM/yyyy", "dd-MM-yyyy","yyyy-MM-dd"}; //Thêm điều kiện chấp nhận đổi convert định dạng
                string chuoiMoi = DateTime.ParseExact(dateOBirth, format, CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
                DateTime newBirth = DateTime.ParseExact(chuoiMoi,format,CultureInfo.InvariantCulture);
                //Kiểm tra nếu trùng ngày đã thay báo trong sql thì giữ nguyên
                customer.DateOfBirth = customer.DateOfBirth != newBirth ? newBirth : customer.DateOfBirth ;
            }
            if (existingCustomer == null)
            {
                // Hiển thị lỗi chung ở trên cùng của Form
                ModelState.AddModelError("", "Không tìm thấy thông tin khách hàng. Vui lòng đăng nhập lại.");
                return View(customer);
            }

            // 3. VALIDATION: Bắt tất cả các lỗi cùng lúc 
            bool isValid = true;

            var nameCheck = ValidateName(customer.NameCus);
            if (!nameCheck.IsValid) 
            { 
                ModelState.AddModelError("NameCus", nameCheck.Error); // Tên field phải khớp với HTML
                isValid = false; 
            }

            var dobCheck = ValidateBirthDate(customer.DateOfBirth ?? DateTime.MinValue);
            if (!dobCheck.IsValid) 
            { 
                ModelState.AddModelError("DateOfBirth", dobCheck.Error); 
                isValid = false; 
            }

            var emailCheck = ValidateEmail(customer.EmailCus);
            if (!emailCheck.IsValid) 
            { 
                ModelState.AddModelError("EmailCus", emailCheck.Error); 
                isValid = false; 
            }

            var phoneCheck = ValidatePhoneNumber(customer.PhoneCus);
            if (!phoneCheck.IsValid) 
            { 
                ModelState.AddModelError("PhoneCus", phoneCheck.Error); 
                isValid = false; 
            }

            // Nối chuỗi thông minh, tự động bỏ qua nếu StreetAddress bị null/rỗng
            string address = string.Join(", ", new[] { customer.StreetAddress, customer.Ward, customer.City }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var addressCheck = ValidateAddress(address);
            if (!addressCheck.IsValid) 
            { 
                // Đẩy lỗi vào ô StreetAddress cho khách dễ nhìn
                ModelState.AddModelError("StreetAddress", addressCheck.Error); 
                isValid = false; 
            }

            // Nếu có BẤT KỲ lỗi nào -> Trả về giao diện ngay lập tức cùng với toàn bộ chữ đỏ
            if (!isValid) return View(customer);
            using (var trans = dBO.Database.BeginTransaction())
            {
                try
                {
                    if (!ModelState.IsValid) throw new Exception("Một lỗi về dữ liệu"); //Kiểm tra lại dữ liệu để chắc ăn
                    dBO.Entry(existingCustomer).CurrentValues.SetValues(customer);//Lưu nguyên đối tượng customer vào hẳn sql
                    dBO.SaveChanges();
                    //Nếu ok thì 
                    trans.Commit(); 
                    return RedirectToAction("ThongTinCaNhan", "User");
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    //Báo lỗi chung hết cho cả form có ValidationMessage
                    Console.WriteLine("DB_ERROR"+ ex);
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu vào cơ sở dữ liệu. Vui lòng thử lại!" );
                    return View(customer);
                }
            }
        }
        
        public class JSONOrder
        {
            public IFormFile ? imageURL {get;set;}
        }
        [HttpPost]
        public async Task<IActionResult> saveAvatar([FromForm] JSONOrder data)
        {
            if(!User.Identity.IsAuthenticated)  return Json(new { success = false, message = "Chưa đăng nhập" });

            var existCus = dBO.Customers.FirstOrDefault(s => s.NameCus == User.Identity.Name);
            if (existCus == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng cần lưu" });
            }
            else
            {
                try
                {
                    //Kiểm tra tên file đã có trong file thư mục server chưa
                    // Chuyển file hình vào thư mục Images
                    if (data.imageURL != null && data.imageURL.Length > 0 )
                    {
                        
                        var baseFilename = Path.GetFileName(data.imageURL.FileName);
                        var path = Path.Combine(_env.WebRootPath, "Images", baseFilename);
                        int count = 0; bool isDuplicate = false;
                         //Lưu file vào thư mục server

                        //Kiểm tra tên file có giống ko
                        while (System.IO.File.Exists(path))
                        {
                            //Nếu file có tên là (1),(2)
                            string fileNameOnly = Path.GetFileNameWithoutExtension(baseFilename); 
                            string extension = Path.GetExtension(data.imageURL.FileName);
                            // if (checkDuplicateImages(path, data.imageURL)) {isDuplicate = true; break;}
                            //Cập nhật filename mới
                            //Tăng số lên đuôi file nếu có file trùng lặp vaf ko trùng lặp về hash
                            Console.WriteLine("Đã phát hiện file trùng lặp có tên là" + Path.GetFileName(path));
                            path = Path.Combine(_env.WebRootPath, "Images", $"{fileNameOnly}({++count}){extension}");
                        }
                        // if (isDuplicate) return Json(new { success = false, message = "File đã tồn tại trên server và có nội dung giống nhau. Vui lòng đổi tên file hoặc chọn file khác." });
                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await data.imageURL.CopyToAsync(stream);
                        }
                        string nameImg = Path.GetFileName(path) != baseFilename ? Path.GetFileName(path) : baseFilename ;
                        existCus.ImagePro = !string.IsNullOrEmpty(nameImg) ? nameImg : existCus.ImagePro;
                        dBO.Entry(existCus).State = EntityState.Modified;
                        await dBO.SaveChangesAsync();
                    }
                    else 
                        return Json(new { success = false, message = "" });

                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "" });
                }
            }
             return Json(new { success = true, message = "Đã set hình thành công." });
        }
        private bool checkDuplicateImages(string file1, IFormFile file2)
        {
            // Chỉ cần so sánh trực tiếp 2 mã Hash, bỏ luôn HashSet thừa
            return GetFileHash(file1) == GetFileHash(file2);
        }

        // Hàm phụ 1: Tính mã Hash từ ĐƯỜNG DẪN FILE trên ổ cứng (dành cho file1)
        private string GetFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            {
                // Dùng System.IO.File để mở luồng từ ổ cứng
                using (var stream = System.IO.File.OpenRead(filePath))
                {
                    var hashBytes = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        // Hàm phụ 2: Tính mã Hash từ IFORMFILE vừa upload lên (dành cho file2)
        private string GetFileHash(IFormFile file)
        {
            if (file == null || file.Length == 0) return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                // Dùng OpenReadStream() để mở luồng trực tiếp từ file upload trên RAM
                using (var stream = file.OpenReadStream())
                {
                    var hashBytes = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        [HttpPost]
        public ActionResult BanUser(int customerId, string reason)
        {
            try
            {
                var customer = dBO.Customers.FirstOrDefault(c => c.IDCus == customerId);
                if (customer == null) return Json(new { success = false, message = "Không tìm thấy user" });

                customer.IsBanned = true;
                customer.ReasonBanned = reason;

                dBO.Entry(customer).State = EntityState.Modified; //Lưu thay đổi nếu chỉnh giá trị và lưu thẳng vào sql
                dBO.SaveChanges();

                return Json(new { success = true, nameCus = customer.NameCus, reasonBanned = "Bạn đã bị ban vì lý do:" + reason + "và sẽ được tự động log out sau 1 phút"});
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error banning user: " + ex.Message);
                return Json(new { success = false, message = "Lỗi server" });
            }
        }

        public ActionResult UnbanUser(int id)
        {
            var customer = dBO.Customers.FirstOrDefault(c => c.IDCus == id);
            if (customer != null)
            {
                customer.IsBanned = false;
                customer.ReasonBanned = null;
                dBO.Entry(customer).State = EntityState.Modified;
                dBO.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        #region Validation Methods (Sử dụng Tuple thay cho static variable)

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

        private (bool IsValid, string Error) ValidateBirthDate(DateTime dob)
        {
            if (dob == DateTime.MinValue) return (false, "Ngày sinh không hợp lệ.");
            if (dob > DateTime.Today) return (false, "Ngày sinh không được lớn hơn hôm nay.");
            if (dob.Year < 1900) return (false, "Năm sinh không hợp lệ (trước 1900).");

            // Tính tuổi
            int age = DateTime.Today.Year - dob.Year;
            if (dob.Date > DateTime.Today.AddYears(-age)) age--;

            if (age < 18) return (false, "Khách hàng phải đủ 18 tuổi trở lên.");

            return (true, string.Empty);
        }

        // Tối ưu để dùng sau nếu cần bắt đổi mật khẩu
        private bool IsStrongPass(string pass)
        {
            if (string.IsNullOrWhiteSpace(pass)) return false;
            string pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";
            return Regex.IsMatch(pass, pattern);
        }

        #endregion

        // Rất quan trọng để tránh đầy bộ nhớ RAM Server
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dBO.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}