using Microsoft.EntityFrameworkCore.Migrations;

namespace PtixiakiReservations.Migrations
{
    public partial class fixVenue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rating",
                table: "Venue");

            migrationBuilder.DropColumn(
                name: "thesis",
                table: "Venue");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "rating",
                table: "Venue",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "thesis",
                table: "Venue",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
