using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eProject.Migrations
{
    /// <inheritdoc />
    public partial class ADDFIELDAVERAGERATINGTOSUBMISSIONTB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AverageRating",
                table: "Submissions",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Submissions");
        }
    }
}
