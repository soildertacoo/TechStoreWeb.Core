using Microsoft.EntityFrameworkCore;
using TechStore.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TechStoreWeb.Core.Hub;
using System.Net.Http.Headers;
using System.Text.Json;


// Nhớ using thư mục chứa DbContext của bạn vào đây (ví dụ: TechStore.Models)

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Kích hoạt mô hình MVC
        builder.Services.AddControllersWithViews();
        builder.Services.AddSignalR(); //Thêm chathub vào service

        // 2. Cấu hình kết nối SQL Server (Lấy chuỗi từ appsettings.json)
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        builder.Services.AddDbContext<DBTechStoreEntities>(options =>
            options.UseSqlServer(connectionString));

        // Kích hoạt luôn ApplicationDbContext nếu project của bạn có dùng Identity
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
        builder.Services.AddScoped<TechStoreWeb.Core.InventoryServices.IInventoryService,
            TechStoreWeb.Core.InventoryServices.InventoryService>();
        builder.Services.AddScoped<TechStoreWeb.Core.InventoryServices.IInventoryCalculationService,
            TechStoreWeb.Core.InventoryServices.InventoryCalculationService>();
        builder.Services.AddHealthChecks()
        .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));


        // 3. KÍCH HOẠT SESSION (Cực kỳ quan trọng cho Giỏ hàng và Đăng nhập)
        builder.Services.AddDistributedMemoryCache(); // Lưu Session trên RAM Server
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(45); // Thời gian sống của Session
            options.Cookie.HttpOnly = true; // Bảo mật Cookie
            options.Cookie.IsEssential = true;
        });
        
        // Cung cấp công cụ để lấy Session ở mọi nơi nếu cần
        builder.Services.AddHttpContextAccessor();
        //Bật cookie lưu phiên đăng nhập
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/User/Login"; // Đường dẫn nếu họ chưa đăng nhập
            options.LogoutPath = "/User/DangXuat"; // Đường dẫn đăng xuất
            options.ExpireTimeSpan = TimeSpan.FromDays(30); // Thời gian sống tối đa của Cookie
        }).AddGoogle(googleOptions =>
        {
            googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"]; //lấy từ appSetting.json
            googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
            googleOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;//Lưu thông tin google trực tiếp vào cookie để lưu phiên đăng nhập
            //Lấy thông tin quan trọng và xin quyền để access tài khoản google người dùng
            googleOptions.Scope.Add("https://www.googleapis.com/auth/user.phonenumbers.read");
            googleOptions.Scope.Add("https://www.googleapis.com/auth/user.birthday.read");
            googleOptions.Scope.Add("https://www.googleapis.com/auth/user.addresses.read");
            googleOptions.Scope.Add("https://www.googleapis.com/auth/user.gender.read");
            googleOptions.Scope.Add("https://www.googleapis.com/auth/contacts");
            googleOptions.Scope.Add("profile");
            googleOptions.SaveTokens = true; //Lấy token về
            googleOptions.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
            {
            OnTicketReceived = async context =>
            {
                    // Lấy các thông tin như email tên từ Google
                    var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
                    var name = context.Principal?.FindFirstValue(ClaimTypes.Name);                
                    // Lấy URL ảnh đại diện 
                    var avatarUrl = context.Principal?.FindFirstValue("urn:google:picture");
                    //Lấy token sau khi xin quyền
                    var accessToken = context.Properties?.GetTokenValue("access_token");
                    var refreshToken = context.Properties?.GetTokenValue("refresh_token");
                    //Lấy số điện thoại 
                    var phoneNumber = ""; 
                    DateTime? birthday = new DateTime();
                    //Check token đã lấy từ google 
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        //Tạo kết nối 
                        using (var httpClient = new HttpClient())
                        {
                            //Set up client, gắn accessToken vào header 
                            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",accessToken);
                            //Kết nối lên google
                            var response = await httpClient.GetAsync(builder.Configuration["Authentication:Google:peopleAPI"]+"phoneNumbers,birthdays");
                            //Lấy kết quả về 
                            if (response.IsSuccessStatusCode)
                            {
                                //Xử lý JSON
                                var jsonString = await response.Content.ReadAsStringAsync();
                                // Dùng thư viện có sẵn của C# để đọc JSON
                                using var jsonDoc = JsonDocument.Parse(jsonString);
                                var root = jsonDoc.RootElement;
                                //Lấy phần tử JSON ĐẦU TIÊN của dữ liệu JSON
                                if (root.TryGetProperty("phoneNumbers", out var phones) && phones.GetArrayLength() > 0)
                                {
                                    phoneNumber = phones[0].GetProperty("value").GetString();
                                    //kiểm tra đối yuongwj tả về 
                                    
                                }
                                if (root.TryGetProperty("birthdays", out var birthdays) && birthdays.GetArrayLength() > 0)
                                {
                                    var dateObj = birthdays[0].GetProperty("date"); //Lấy đối tượng đầu tiên của birthdays
                                    if (dateObj.TryGetProperty("year", out var year) && 
                                        dateObj.TryGetProperty("month", out var month) && 
                                        dateObj.TryGetProperty("day", out var day))
                                    {
                                        birthday = new DateTime(year.GetInt32(), month.GetInt32(), day.GetInt32());
                                    }
                                }
                            }
                        }
                    }
                    // Kết nối với Database của bạn
                    // Lấy DbContext thông qua RequestServices
                    var dbContext = context.HttpContext.RequestServices.GetRequiredService<DBTechStoreEntities>();

                    // 3. Kiểm tra xem khách hàng này đã tồn tại trong DB chưa, kiểm tra phần ApplicationDBContext
                    var user = dbContext.Customers.FirstOrDefault(k => k.EmailCus == email);
                    if (user == null)
                    {
                        // Nếu chưa có thì tạo mới tài khoản tự động
                        var newCustomer = new Customer
                        {
                            EmailCus = email,
                            NameCus = name,
                            DateOfBirth = birthday != null ? birthday : null,
                            PhoneCus = phoneNumber != null ? phoneNumber : null,
                            RegisteredDate = DateTime.Now
                        };
                        dbContext.Customers.Add(newCustomer);//Thêm nguyên dữ liệu mới vào sql
    ;
                    }
                    else
                    {
                        //Trong trường hợp có đổi tên hay đổi địa chỉ email, ngày sinh gì đó
                        user.NameCus = name;
                        user.EmailCus = email;
                        user.DateOfBirth = birthday != null ? birthday : null;
                        dbContext.Entry(user).State = EntityState.Modified;
                    }

                    // Lưu thay đổi vào SQL Server
                    await dbContext.SaveChangesAsync();

                    // Cho phép tiếp tục luồng đăng nhập và tạo Cookie phiên làm việc
                    await Task.CompletedTask;
                }
        };
        });;
        var app = builder.Build();

        // --- BẮT ĐẦU CẤU HÌNH ĐƯỜNG ỐNG (PIPELINE) CHẠY WEB ---

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
            app.UseStatusCodePagesWithReExecute("/Home/ErrorCode", "?statusCode={0}"); // goi ham xu ly trong HomeController

        }
        else app.UseDeveloperExceptionPage();
        app.UseHttpsRedirection();
        // Cho phép web đọc file tĩnh từ thư mục wwwroot (css, js, images)
        app.UseStaticFiles();
        app.UseRouting();
        // BẮT BUỘC: Lệnh bật Session phải nằm GIỮA UseRouting và UseAuthorization
        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHub<chatHub>("/chatHub"); //Khởi động chathub
        app.MapHub<notifyHub>("/notifyHub"); //Khởi động notifyHub(Thông báo tương tác)
        // Thiết lập trang chủ mặc định khi vừa mở web lên là HomeController -> Index
        app.MapHealthChecks("/health");
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.Run();
    }
}