using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PtixiakiReservations.Migrations
{
    public partial class FamilyEvent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "Event");

            migrationBuilder.AddColumn<string>(
                name: "Desc",
                table: "SubArea",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duration",
                table: "Reservation",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "Reservation",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "Event",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "Event",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FamilyEventId",
                table: "Event",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDateTime",
                table: "Event",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "FamilyEvent",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyEvent", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_EventId",
                table: "Reservation",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Event_FamilyEventId",
                table: "Event",
                column: "FamilyEventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Event_FamilyEvent_FamilyEventId",
                table: "Event",
                column: "FamilyEventId",
                principalTable: "FamilyEvent",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservation_Event_EventId",
                table: "Reservation",
                column: "EventId",
                principalTable: "Event",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Event_FamilyEvent_FamilyEventId",
                table: "Event");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservation_Event_EventId",
                table: "Reservation");

            migrationBuilder.DropTable(
                name: "FamilyEvent");

            migrationBuilder.DropIndex(
                name: "IX_Reservation_EventId",
                table: "Reservation");

            migrationBuilder.DropIndex(
                name: "IX_Event_FamilyEventId",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "Desc",
                table: "SubArea");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "FamilyEventId",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "StartDateTime",
                table: "Event");

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "Event",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
