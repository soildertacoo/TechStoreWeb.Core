using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStoreWeb.Core.Migrations
{
    /// <inheritdoc />
    public partial class DBChatIntegrated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUserss",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameUser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoleUser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordUser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUserss", x => x.ID);
                });

            // migrationBuilder.CreateTable(
            //     name: "Category",
            //     columns: table => new
            //     {
            //         IDCate = table.Column<string>(type: "nvarchar(450)", nullable: false),
            //         Id = table.Column<int>(type: "int", nullable: false),
            //         NameCate = table.Column<string>(type: "nvarchar(max)", nullable: true)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_Category", x => x.IDCate);
            //     });

            // migrationBuilder.CreateTable(
            //     name: "Customers",
            //     columns: table => new
            //     {
            //         IDCus = table.Column<int>(type: "int", nullable: false)
            //             .Annotation("SqlServer:Identity", "1, 1"),
            //         NameCus = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         PhoneCus = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         EmailCus = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         PassCus = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         StreetAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         Ward = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         District = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         City = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
            //         RegisteredDate = table.Column<DateTime>(type: "datetime2", nullable: true),
            //         IsVIP = table.Column<bool>(type: "bit", nullable: true),
            //         MembershipLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         IsBanned = table.Column<bool>(type: "bit", nullable: true),
            //         ReasonBanned = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         TwoFactorSecret = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
            //         Is2FAEnabled = table.Column<bool>(type: "bit", nullable: false),
            //         IsAnalyticEnabled = table.Column<bool>(type: "bit", nullable: false),
            //         ImagePro = table.Column<string>(type: "nvarchar(max)", nullable: true)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_Customers", x => x.IDCus);
            //     });

            // migrationBuilder.CreateTable(
            //     name: "ShippingProvider",
            //     columns: table => new
            //     {
            //         ProviderCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
            //         ProviderName = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         ApiToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         IsActive = table.Column<bool>(type: "bit", nullable: false),
            //         ApiCreateOrder = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         ApiCancelOrder = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         ApiCheckStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         SupportFast = table.Column<bool>(type: "bit", nullable: false),
            //         SupportStandard = table.Column<bool>(type: "bit", nullable: false),
            //         SupportExpress = table.Column<bool>(type: "bit", nullable: false),
            //         PriceFast = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
            //         PriceStandard = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
            //         PriceExpress = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_ShippingProvider", x => x.ProviderCode);
            //     });

            // migrationBuilder.CreateTable(
            //     name: "Products",
            //     columns: table => new
            //     {
            //         ProductID = table.Column<int>(type: "int", nullable: false)
            //             .Annotation("SqlServer:Identity", "1, 1"),
            //         NamePro = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         DecriptionPro = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         Category = table.Column<string>(type: "nvarchar(450)", nullable: true),
            //         Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
            //         ImagePro = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
            //         CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_Products", x => x.ProductID);
            //         table.ForeignKey(
            //             name: "FK_Products_Category_Category",
            //             column: x => x.Category,
            //             principalTable: "Category",
            //             principalColumn: "IDCate");
            //     });

            migrationBuilder.CreateTable(
                name: "ChatMessage",
                columns: table => new
                {
                    MessageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IDCus = table.Column<int>(type: "int", nullable: true),
                    AdminID = table.Column<int>(type: "int", nullable: true),
                    RoomId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsFromSupport = table.Column<bool>(type: "bit", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessage", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_ChatMessage_AdminUserss_AdminID",
                        column: x => x.AdminID,
                        principalTable: "AdminUserss",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_ChatMessage_Customers_IDCus",
                        column: x => x.IDCus,
                        principalTable: "Customers",
                        principalColumn: "IDCus");
                });

            // migrationBuilder.CreateTable(
            //     name: "OrderPro",
            //     columns: table => new
            //     {
            //         ID = table.Column<int>(type: "int", nullable: false)
            //             .Annotation("SqlServer:Identity", "1, 1"),
            //         DateOrder = table.Column<DateTime>(type: "datetime2", nullable: true),
            //         IDCus = table.Column<int>(type: "int", nullable: false),
            //         AddressDeliverry = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
            //         PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
            //         TrackingNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         ShippingCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         ShippingCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_OrderPro", x => x.ID);
            //         table.ForeignKey(
            //             name: "FK_OrderPro_Customers_IDCus",
            //             column: x => x.IDCus,
            //             principalTable: "Customers",
            //             principalColumn: "IDCus",
            //             onDelete: ReferentialAction.Cascade);
            //     });

            // migrationBuilder.CreateTable(
            //     name: "LoveProducts",
            //     columns: table => new
            //     {
            //         ProductID = table.Column<int>(type: "int", nullable: false)
            //             .Annotation("SqlServer:Identity", "1, 1"),
            //         CustomerID = table.Column<int>(type: "int", nullable: false),
            //         CustomerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         ProductsProductID = table.Column<int>(type: "int", nullable: true)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_LoveProducts", x => x.ProductID);
            //         table.ForeignKey(
            //             name: "FK_LoveProducts_Customers_CustomerID",
            //             column: x => x.CustomerID,
            //             principalTable: "Customers",
            //             principalColumn: "IDCus",
            //             onDelete: ReferentialAction.Cascade);
            //         table.ForeignKey(
            //             name: "FK_LoveProducts_Products_ProductsProductID",
            //             column: x => x.ProductsProductID,
            //             principalTable: "Products",
            //             principalColumn: "ProductID");
            //     });

            // migrationBuilder.CreateTable(
            //     name: "Reviews",
            //     columns: table => new
            //     {
            //         ReviewID = table.Column<int>(type: "int", nullable: false)
            //             .Annotation("SqlServer:Identity", "1, 1"),
            //         ProductID = table.Column<int>(type: "int", nullable: false),
            //         CustomerID = table.Column<int>(type: "int", nullable: false),
            //         Rating = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
            //         ReviewContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         ReviewDate = table.Column<DateTime>(type: "datetime2", nullable: true),
            //         ReviewerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //         IsHidden = table.Column<bool>(type: "bit", nullable: true),
            //         IsBanned = table.Column<bool>(type: "bit", nullable: true)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_Reviews", x => x.ReviewID);
            //         table.ForeignKey(
            //             name: "FK_Reviews_Customers_CustomerID",
            //             column: x => x.CustomerID,
            //             principalTable: "Customers",
            //             principalColumn: "IDCus",
            //             onDelete: ReferentialAction.Cascade);
            //         table.ForeignKey(
            //             name: "FK_Reviews_Products_ProductID",
            //             column: x => x.ProductID,
            //             principalTable: "Products",
            //             principalColumn: "ProductID",
            //             onDelete: ReferentialAction.Cascade);
            //     });

            // migrationBuilder.CreateTable(
            //     name: "OrderDetails",
            //     columns: table => new
            //     {
            //         ID = table.Column<int>(type: "int", nullable: false)
            //             .Annotation("SqlServer:Identity", "1, 1"),
            //         IDProduct = table.Column<int>(type: "int", nullable: false),
            //         IDOrder = table.Column<int>(type: "int", nullable: false),
            //         Quantity = table.Column<int>(type: "int", nullable: true),
            //         UnitPrice = table.Column<double>(type: "float", nullable: true),
            //         Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
            //         Subtotal = table.Column<double>(type: "float", nullable: true),
            //         Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_OrderDetails", x => x.ID);
            //         table.ForeignKey(
            //             name: "FK_OrderDetails_OrderPro_IDOrder",
            //             column: x => x.IDOrder,
            //             principalTable: "OrderPro",
            //             principalColumn: "ID",
            //             onDelete: ReferentialAction.Cascade);
            //         table.ForeignKey(
            //             name: "FK_OrderDetails_Products_IDProduct",
            //             column: x => x.IDProduct,
            //             principalTable: "Products",
            //             principalColumn: "ProductID",
            //             onDelete: ReferentialAction.Cascade);
            //     });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_AdminID",
                table: "ChatMessage",
                column: "AdminID");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_IDCus",
                table: "ChatMessage",
                column: "IDCus");

            // migrationBuilder.CreateIndex(
            //     name: "IX_LoveProducts_CustomerID",
            //     table: "LoveProducts",
            //     column: "CustomerID");

            // migrationBuilder.CreateIndex(
            //     name: "IX_LoveProducts_ProductsProductID",
            //     table: "LoveProducts",
            //     column: "ProductsProductID");

            // migrationBuilder.CreateIndex(
            //     name: "IX_OrderDetails_IDOrder",
            //     table: "OrderDetails",
            //     column: "IDOrder");

            // migrationBuilder.CreateIndex(
            //     name: "IX_OrderDetails_IDProduct",
            //     table: "OrderDetails",
            //     column: "IDProduct");

            // migrationBuilder.CreateIndex(
            //     name: "IX_OrderPro_IDCus",
            //     table: "OrderPro",
            //     column: "IDCus");

            // migrationBuilder.CreateIndex(
            //     name: "IX_Products_Category",
            //     table: "Products",
            //     column: "Category");

            // migrationBuilder.CreateIndex(
            //     name: "IX_Reviews_CustomerID",
            //     table: "Reviews",
            //     column: "CustomerID");

            // migrationBuilder.CreateIndex(
            //     name: "IX_Reviews_ProductID",
            //     table: "Reviews",
            //     column: "ProductID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessage");

            // migrationBuilder.DropTable(
            //     name: "LoveProducts");

            // migrationBuilder.DropTable(
            //     name: "OrderDetails");

            // migrationBuilder.DropTable(
            //     name: "Reviews");

            // migrationBuilder.DropTable(
            //     name: "ShippingProvider");

            migrationBuilder.DropTable(
                name: "AdminUserss");

            // migrationBuilder.DropTable(
            //     name: "OrderPro");

            // migrationBuilder.DropTable(
            //     name: "Products");

            // migrationBuilder.DropTable(
            //     name: "Customers");

            // migrationBuilder.DropTable(
            //     name: "Category");
        }
    }
}
