using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PtixiakiReservations.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueManagerRequestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasRequestedVenueManagerRole",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VenueManagerRequestDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VenueManagerRequestReason",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VenueManagerRequestStatus",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasRequestedVenueManagerRole",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VenueManagerRequestDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VenueManagerRequestReason",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VenueManagerRequestStatus",
                table: "AspNetUsers");
        }
    }
}
