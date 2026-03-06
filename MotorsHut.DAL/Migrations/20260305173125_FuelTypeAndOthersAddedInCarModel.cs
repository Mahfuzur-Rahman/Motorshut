using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorsHut.DAL.Migrations
{
    /// <inheritdoc />
    public partial class FuelTypeAndOthersAddedInCarModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReturned",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "IsSold",
                table: "Cars");

            migrationBuilder.AddColumn<string>(
                name: "FuelType",
                table: "Cars",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "InStock",
                table: "Cars",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                table: "Cars",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "TotalSold",
                table: "Cars",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Transmission",
                table: "Cars",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FuelType",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "InStock",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "ShortDescription",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "TotalSold",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "Transmission",
                table: "Cars");

            migrationBuilder.AddColumn<bool>(
                name: "IsReturned",
                table: "Cars",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSold",
                table: "Cars",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
