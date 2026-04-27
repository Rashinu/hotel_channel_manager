using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelChannelManager.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationTypeAndContentUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentFileUrl",
                table: "IncomingMails",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReservationType",
                table: "IncomingMails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentFileUrl",
                table: "IncomingMails");

            migrationBuilder.DropColumn(
                name: "ReservationType",
                table: "IncomingMails");
        }
    }
}
