using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eProject.Migrations
{
    /// <inheritdoc />
    public partial class AddAverageRatingToSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "status",
                table: "StudentAwards",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Exhibitions",
                newName: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "StudentAwards",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Exhibitions",
                newName: "status");
        }
    }
}
