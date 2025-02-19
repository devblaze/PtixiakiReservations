using Microsoft.EntityFrameworkCore.Migrations;

namespace PtixiakiReservations.Migrations
{
    public partial class AddCity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Venue");

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Venue",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "City",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Venue_CityId",
                table: "Venue",
                column: "CityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Venue_City_CityId",
                table: "Venue",
                column: "CityId",
                principalTable: "City",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Venue_City_CityId",
                table: "Venue");

            migrationBuilder.DropTable(
                name: "City");

            migrationBuilder.DropIndex(
                name: "IX_Venue_CityId",
                table: "Venue");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Venue");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Venue",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
