using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelChannelManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMailAndReservationModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReceivedAt",
                table: "IncomingMails",
                newName: "UniqueId");

            migrationBuilder.RenameColumn(
                name: "From",
                table: "IncomingMails",
                newName: "UidKey");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                table: "IncomingMails",
                newName: "MailDate");

            migrationBuilder.AddColumn<int>(
                name: "AdultCount",
                table: "Reservations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Agency",
                table: "Reservations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "BabyCount",
                table: "Reservations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChildCount",
                table: "Reservations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Hotel",
                table: "Reservations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Pension",
                table: "Reservations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderName",
                table: "Reservations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReservationType",
                table: "Reservations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RoomCount",
                table: "Reservations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SaleDate",
                table: "Reservations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Voucher",
                table: "Reservations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConvertErrorMessage",
                table: "IncomingMails",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "IncomingMails",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromAddress",
                table: "IncomingMails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "HasAttachments",
                table: "IncomingMails",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "IncomingMails",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ProviderId",
                table: "IncomingMails",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceiveDate",
                table: "IncomingMails",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ToAddress",
                table: "IncomingMails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdultCount",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Agency",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "BabyCount",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ChildCount",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Hotel",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Pension",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ProviderName",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ReservationType",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RoomCount",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "SaleDate",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Voucher",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ConvertErrorMessage",
                table: "IncomingMails");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "IncomingMails");

            migrationBuilder.DropColumn(
                name: "FromAddress",
                table: "IncomingMails");

            migrationBuilder.DropColumn(
                name: "HasAttachments",
                table: "IncomingMails");

            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "IncomingMails");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "IncomingMails");

            migrationBuilder.DropColumn(
                name: "ReceiveDate",
                table: "IncomingMails");

            migrationBuilder.DropColumn(
                name: "ToAddress",
                table: "IncomingMails");

            migrationBuilder.RenameColumn(
                name: "UniqueId",
                table: "IncomingMails",
                newName: "ReceivedAt");

            migrationBuilder.RenameColumn(
                name: "UidKey",
                table: "IncomingMails",
                newName: "From");

            migrationBuilder.RenameColumn(
                name: "MailDate",
                table: "IncomingMails",
                newName: "ErrorMessage");
        }
    }
}
