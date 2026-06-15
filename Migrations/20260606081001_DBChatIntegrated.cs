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
            // CHỈ TẠO MỖI BẢNG CHAT MỚI
            migrationBuilder.CreateTable(
                name: "ChatMessages",
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
        table.PrimaryKey("PK_ChatMessages", x => x.MessageId);
        
        table.ForeignKey(
            name: "FK_ChatMessages_AdminUsers_AdminID",
            column: x => x.AdminID,
            principalTable: "AdminUser", // <--- BỎ CHỮ 's' Ở ĐÂY
            principalColumn: "ID",
            onDelete: ReferentialAction.SetNull);
            
        table.ForeignKey(
            name: "FK_ChatMessages_Customers_IDCus",
            column: x => x.IDCus,
            principalTable: "Customers", // <--- BỎ CHỮ 's' Ở ĐÂY LUÔN
            principalColumn: "IDCus",
            onDelete: ReferentialAction.SetNull);
    });

            // TẠO INDEX CHO CÁC KHÓA NGOẠI CỦA BẢNG CHAT
            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_AdminID",
                table: "ChatMessages",
                column: "AdminID");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_IDCus",
                table: "ChatMessages",
                column: "IDCus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // NẾU UNDO THÌ CHỈ XÓA MỖI BẢNG CHAT
            migrationBuilder.DropTable(
                name: "ChatMessages");
        }
    }
}