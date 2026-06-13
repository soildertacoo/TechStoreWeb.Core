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
        //Khi tạo một class mới và muốn nhập vào DB thì phải đăng ký vào trong DbContext này 
        public virtual DbSet<AdminUsers> AdminUsers { get; set; }
        public virtual DbSet<Category> Category { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<OrderDetails> OrderDetails { get; set; }
        public virtual DbSet<ShippingProvider> ShippingProvider { get; set; }

        public virtual DbSet<OrderPro> OrderPro { get; set; }
        public virtual DbSet<Products> Products { get; set; }
        public virtual DbSet<Review> Reviews { get; set; }
        public virtual DbSet<ChatMessage> ChatMessage { get; set; }
        public virtual DbSet<CartItem> CartItems {get; set;}
        public virtual DbSet<Inventory> Inventories { get; set; }
        public virtual DbSet<Banner> Banners { get; set; }
        public virtual DbSet<Promotion> Promotions { get; set; }
        public virtual DbSet<OTPModel> OTPModels { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Products>()
            .HasOne(p => p.Category1) // (Tên thuộc tính Navigation trong bảng Product)
            .WithMany(c => c.Products) // (Tên danh sách Products trong bảng Category)
            .HasForeignKey(p => p.Category) // Cột Khóa ngoại bên bảng Product
            .HasPrincipalKey(c => c.IDCate); //Bắt buộc cột ko phải khóa chính phải là unique nếu muốn trỏ vào
        }
    }
}
