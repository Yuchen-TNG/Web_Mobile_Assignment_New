using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web_Mobile_Assignment_New.Migrations
{
    /// <inheritdoc />
    public partial class TanDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Houses");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Houses",
                newName: "RoomType");

            migrationBuilder.AddColumn<int>(
                name: "Bathrooms",
                table: "Houses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Houses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Other",
                table: "Houses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rooms",
                table: "Houses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Sqft",
                table: "Houses",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bathrooms",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "Other",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "Rooms",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "Sqft",
                table: "Houses");

            migrationBuilder.RenameColumn(
                name: "RoomType",
                table: "Houses",
                newName: "Title");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Houses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
