using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStoreWeb.Core.Migrations
{
    /// <inheritdoc />
    public partial class adminFailedAttempt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastLogin",
                table: "AdminUsers",
                newName: "BannedUntil");

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "AdminUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBanned",
                table: "AdminUsers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonBanned",
                table: "AdminUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "IsBanned",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "ReasonBanned",
                table: "AdminUsers");

            migrationBuilder.RenameColumn(
                name: "BannedUntil",
                table: "AdminUsers",
                newName: "LastLogin");
        }
    }
}
