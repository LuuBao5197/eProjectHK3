using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eProject.Migrations
{
    /// <inheritdoc />
    public partial class addFieldStatusForStudentAward : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "StudentAwards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                table: "StudentAwards");
        }
    }
}
