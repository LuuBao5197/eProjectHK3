using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eProject.Migrations
{
    /// <inheritdoc />
    public partial class updatefieldImageANDaddConditionTB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Contests");

            migrationBuilder.RenameColumn(
                name: "ParticipationCriteria",
                table: "Contests",
                newName: "Status");

            migrationBuilder.AddColumn<string>(
                name: "Phase",
                table: "Exhibitions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "Exhibitions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "thumbnail",
                table: "Exhibitions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phase",
                table: "Contests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Thumbnail",
                table: "Contests",
                type: "nvarchar(max)",
                nullable: true);

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

            migrationBuilder.CreateTable(
                name: "Conditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContestId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conditions_Contests_ContestId",
                        column: x => x.ContestId,
                        principalTable: "Contests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conditions_ContestId",
                table: "Conditions",
                column: "ContestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Conditions");

            migrationBuilder.DropColumn(
                name: "Phase",
                table: "Exhibitions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "Exhibitions");

            migrationBuilder.DropColumn(
                name: "thumbnail",
                table: "Exhibitions");

            migrationBuilder.DropColumn(
                name: "Phase",
                table: "Contests");

            migrationBuilder.DropColumn(
                name: "Thumbnail",
                table: "Contests");

            migrationBuilder.DropColumn(
                name: "AwardStatus",
                table: "Awards");

            migrationBuilder.DropColumn(
                name: "status",
                table: "Awards");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Contests",
                newName: "ParticipationCriteria");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Contests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
