using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStoreWeb.Core.Migrations
{
    /// <inheritdoc />
    public partial class userBannedUntil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BannedUntil",
                table: "Customers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannedUntil",
                table: "Customers");
        }
    }
}
