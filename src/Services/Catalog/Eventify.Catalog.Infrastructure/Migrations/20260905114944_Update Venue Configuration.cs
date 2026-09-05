using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventify.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVenueConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Address_ZipCode",
                table: "venues",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Address_Street",
                table: "venues",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Address_State",
                table: "venues",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Address_Country",
                table: "venues",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Address_City",
                table: "venues",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_venues_Name",
                table: "venues",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_venues_Address_City",
                table: "venues",
                column: "Address_City");

            migrationBuilder.CreateIndex(
                name: "IX_venues_Address_ZipCode",
                table: "venues",
                column: "Address_ZipCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_venues_Address_City",
                table: "venues");

            migrationBuilder.DropIndex(
                name: "IX_venues_Address_ZipCode",
                table: "venues");

            migrationBuilder.DropIndex(
                name: "IX_venues_Name",
                table: "venues");

            migrationBuilder.AlterColumn<string>(
                name: "Address_ZipCode",
                table: "venues",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Address_Street",
                table: "venues",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Address_State",
                table: "venues",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Address_Country",
                table: "venues",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Address_City",
                table: "venues",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}
