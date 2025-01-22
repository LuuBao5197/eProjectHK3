using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eProject.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwardStatus",
                table: "Awards");

            migrationBuilder.DropColumn(
                name: "status",
                table: "Awards");

            migrationBuilder.AddColumn<bool>(
                name: "IsFirstLogin",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsReviewer",
                table: "Staff",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAwarded",
                table: "Awards",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFirstLogin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsReviewer",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "IsAwarded",
                table: "Awards");

            migrationBuilder.AddColumn<string>(
                name: "AwardStatus",
                table: "Awards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "Awards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
