using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PtixiakiReservations.Migrations
{
    /// <inheritdoc />
    public partial class AddSubAreaIdToEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubAreaId",
                table: "Event",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Event_SubAreaId",
                table: "Event",
                column: "SubAreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Event_SubArea_SubAreaId",
                table: "Event",
                column: "SubAreaId",
                principalTable: "SubArea",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Event_SubArea_SubAreaId",
                table: "Event");

            migrationBuilder.DropIndex(
                name: "IX_Event_SubAreaId",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "SubAreaId",
                table: "Event");
        }
    }
}
