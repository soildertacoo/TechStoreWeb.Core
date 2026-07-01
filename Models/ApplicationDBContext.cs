using AspNetCoreGeneratedDocument;
using Microsoft.EntityFrameworkCore;

namespace TechStore.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
    : base(options)
        {
        }
        
        // 1. Tên class là AdminUsers (chuẩn theo file của bạn), tên biến DbSet cũng là AdminUsers (để ChatHub gọi được)
        public virtual DbSet<AdminUsers> AdminUsers { get; set; }
        
        public virtual DbSet<Category> Category { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<OrderDetails> OrderDetails { get; set; }
        public virtual DbSet<ShippingProviders> ShippingProviders { get; set; }
        public virtual DbSet<OrderPro> OrderPro { get; set; }
        public virtual DbSet<Products> Products { get; set; }
        public virtual DbSet<Review> Reviews { get; set; }
        
        // 2. Tên biến ChatMessages có "s" để ChatHub không báo lỗi
        public virtual DbSet<ChatMessage> ChatMessages { get; set; }
        public virtual DbSet<ProductRecommendation> ProductRecommendations { get; set; }
        
        public virtual DbSet<CartItem> CartItems {get; set;}
        public virtual DbSet<Inventory> Inventories { get; set; }
        public virtual DbSet<Banner> Banners { get; set; }
        public virtual DbSet<Promotion> Promotions { get; set; }
        public virtual DbSet<OTPModel> OTPModels { get; set; }
        public virtual DbSet<LoveProducts> LoveProducts { get; set; }
        public virtual DbSet<VIPCustomer> VIPCustomers { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Products>()
                .HasOne(p => p.Category1) 
                .WithMany(c => c.Products) 
                .HasForeignKey(p => p.Category) 
                .HasPrincipalKey(c => c.IDCate); 
                
            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.Customer)
                .WithMany() 
                .HasForeignKey(c => c.IDCus)
                .OnDelete(DeleteBehavior.SetNull);

            // 3. Sử dụng đúng class AdminUsers (CÓ CHỮ S) để tạo khóa ngoại
            modelBuilder.Entity<ChatMessage>()
                .HasOne(a => a.AdminUser)
                .WithMany()
                .HasForeignKey(a => a.AdminID)
                .OnDelete(DeleteBehavior.SetNull);
                
            // 4. Báo cho hệ thống biết class AdminUsers sẽ map với bảng tên "AdminUser" trong SQL
            modelBuilder.Entity<AdminUsers>()
                .ToTable("AdminUsers"); 

            // Cấu hình báo cho EF Core biết bảng OrderPro đang chứa Trigger
            // modelBuilder.Entity<OrderPro>()
            //     .ToTable("OrderPro",tb => tb.HasTrigger("trg_AutoUpgradeVIP"));
                modelBuilder.Entity<OrderPro>()
                .ToTable(tb => tb.UseSqlOutputClause(false));
            }
            
    }
}