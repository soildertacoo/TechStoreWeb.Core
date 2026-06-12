using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TechStore.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting; // Bắt buộc cho IWebHostEnvironment
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Google.GenAI.Types;    // Bắt buộc cho IFormFile
using System.Security.Cryptography;

namespace TechStore.Controllers
{
    public class ProductsController : Controller
    {
        private readonly DBTechStoreEntities db;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env; // Thêm biến môi trường để tìm thư mục wwwroot

        // Tiêm DbContext và IWebHostEnvironment vào
        public ProductsController(DBTechStoreEntities dbContext, ApplicationDbContext appContext, IWebHostEnvironment env)
        {
            db = dbContext;
            _context = appContext;
            _env = env; // Gán giá trị
        }        
        
        public ActionResult Index()
        {
            var item = db.Products.Include(c => c.Category1).ToList();
            return View(item);
        }

        [HttpGet]
        public ActionResult Create()
        {
            ViewData["Category"] = new SelectList(db.Category, "IDCate", "NameCate");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Products pro, IFormFile ImagePro, bool isValid)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu vào cơ sở dữ liệu. Vui lòng thử lại!" );
                isValid = false;
            }
            
            if (pro == null)
            {
                ViewBag.Loi = "Không thể thay đổi vì có lỗi ID";
                isValid = false;

            }
            try
            {
                    string? nameImg = null;
                    // Chuyển file hình vào thư mục IMg
                    if (ImagePro != null && ImagePro.Length > 0 )
                    {
                        
                        var baseFilename = Path.GetFileName(ImagePro.FileName);
                        var path = Path.Combine(_env.WebRootPath, "Images", baseFilename);
                        string fileNameOnly = Path.GetFileNameWithoutExtension(baseFilename); //Bỏ đuôi file ra
                        string extension = Path.GetExtension(ImagePro.FileName);//Lấy đuổi file
                        int count = 0; bool isDuplicate = false;

                        //Kiểm tra tên file có giống ko
                        while (System.IO.File.Exists(path))
                        {
                            if (checkDuplicateImages(path, ImagePro)) {isDuplicate = true; break;}
                            //Cập nhật filename mới
                            //Tăng số lên đuôi file nếu có file trùng lặp
                            Console.WriteLine("Đã phát hiện file trùng lặp có tên là" + Path.GetFileName(path));
                            path = Path.Combine(_env.WebRootPath, "Images", $"{fileNameOnly}({++count}){extension}");
                        }
                        if (isDuplicate) throw new Exception("File đã tồn tại trên server và có nội dung giống nhau. Vui lòng đổi tên file hoặc chọn file khác.");
                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await ImagePro.CopyToAsync(stream);
                        }
                        //So sánh xem có tên mới so với tên hình được đưa vào đây không
                        nameImg = Path.GetFileName(path) != baseFilename ? Path.GetFileName(path) : baseFilename ;

                    }
                pro.ImagePro = nameImg ?? "";
                pro.CreatedDate = System.DateTime.Now;
                decimal? price = pro.Price * (pro.Discount > 0 ? pro.Discount : 0 / 100);
                pro.Price -= (decimal)price;
                db.Products.Add(pro);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorCreate = "Bị lỗi khi tạo sản phẩm : " + ex.Message;
                isValid = false;
            }
            if (!isValid)
            {
                ViewData["Category"] = new SelectList(db.Category, "IDCate", "NameCate");
                return View();
            }
            return RedirectToAction("Index");
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

        [HttpGet]
        public async Task<IActionResult> LookupBarcode(string upc)
        {
            if (string.IsNullOrEmpty(upc)) 
            {
                return BadRequest("Vui lòng nhập mã vạch.");
            }

            try
            {
                // Dùng HttpClient của C# để gọi API (C# không bao giờ bị dính lỗi CORS)
                using (HttpClient client = new HttpClient())
                {
                    string apiUrl = $"https://api.upcitemdb.com/prod/trial/lookup?upc={upc}";
                    
                    // Thực hiện gọi GET
                    HttpResponseMessage response = await client.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        // Đọc cục JSON trả về từ API quốc tế
                        string jsonString = await response.Content.ReadAsStringAsync();
                        
                        //Trả nguyên json về khi dùng get, và nhớ kiểu string
                        return Content(jsonString, "application/json");
                    }
                    else
                    {
                        return StatusCode((int)response.StatusCode, "Không thể lấy dữ liệu từ nhà cung cấp."); //Trả lỗi về javascript khi gọi hàm kiểu get để trả json về
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        public ActionResult Details(int id)
        {
            var item = db.Products.FirstOrDefault(s => s.ProductID == id);
            //tương đương thêm selected vào vào một option có giá trị ở tham số thứ 4 và thứ 2
            ViewBag.CategoryName = new SelectList(db.Category, "IDCate", "NameCate",item?.Category);
            return View(item);
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            //lấy toàn bộ đối tượng của một bảng trên sql thành danh sách list
            //tương đương thêm selected vào vào một option có giá trị ở tham số thứ 4 và thứ 2
            var item = db.Products.FirstOrDefault(s => s.ProductID == id);
            ViewBag.CategoryName = new SelectList(db.Category, "IDCate", "NameCate",item?.Category);            
            return View(item);
        }

        [HttpPost, ActionName("Edit")]
        public async Task<IActionResult> Edit_Up(Products pro, IFormFile ImagePro, bool isValid)
        {

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu vào cơ sở dữ liệu. Vui lòng thử lại!" );
                isValid = false;
            }
            var existPro = db.Products.FirstOrDefault(s => s.ProductID == pro.ProductID);
            if (existPro == null)
            {
                ViewBag.Loi = "Không thể thay đổi vì có lỗi ID";
                isValid = false;

            }
            else
            {
                try
                {
                    string nameImg = "";
                    // Chuyển file hình vào thư mục IMg
                    if (ImagePro != null && ImagePro.Length > 0 )
                    {
                        
                        var baseFilename = Path.GetFileName(ImagePro.FileName);
                        var path = Path.Combine(_env.WebRootPath, "Images", baseFilename);
                        string fileNameOnly = Path.GetFileNameWithoutExtension(baseFilename); //Bỏ đuôi file ra
                        string extension = Path.GetExtension(ImagePro.FileName);//Lấy đuổi file
                        int count = 0;

                        //Kiểm tra tên file có giống ko
                        while (System.IO.File.Exists(path))
                        {
                            //Cập nhật filename mới
                            //Tăng số lên đuôi file nếu có file trùng lặp
                            Console.WriteLine("Đã phát hiện file trùng lặp có tên là" + Path.GetFileName(path));
                            path = Path.Combine(_env.WebRootPath, "Images", $"{fileNameOnly}({++count}){extension}");
                        }
                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await ImagePro.CopyToAsync(stream);
                        }
                        //So sánh xem có tên mới so với tên hình được đưa vào đây không
                        nameImg = Path.GetFileName(path) != baseFilename ? Path.GetFileName(path) : baseFilename ;

                    }
                    existPro.ImagePro = !string.IsNullOrEmpty(nameImg) ? nameImg : existPro.ImagePro;
                    // Cập nhật từng trường
                    existPro.NamePro = pro.NamePro;
                    existPro.DecriptionPro = pro.DecriptionPro;
                    existPro.Price = pro.Price;
                    existPro.Category = pro.Category ?? existPro.Category;
                    existPro.Discount = pro.Discount > 0 ? pro.Discount : 0;
                    db.Entry(existPro).State = EntityState.Modified;
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    ViewBag.Loi = "Không thể thay đổi vì có lỗi " + ex.Message;
                    isValid = false;
                }
            }
            if (!isValid)
            {
                ViewBag.CategoryName = new SelectList(db.Category, "IDCate", "NameCate",pro?.Category); 
                return View(existPro);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            var item = db.Products.FirstOrDefault(s => s.ProductID == id);
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var item = db.Products.FirstOrDefault(s => s.ProductID == id);
            if (item != null)
            {
                try
                {
                    db.Products.Remove(item);
                    db.SaveChanges();
                }
                catch
                {
                    ViewBag.Loi = "Không xóa được vì đã có ghi nhận trong lịch sử mua hàng";
                    return View(item); // Trả lại item để View hiển thị chi tiết sản phẩm bị lỗi
                }
            }
            return RedirectToAction("Index", "Products");
        }
    }
}