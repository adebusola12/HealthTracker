using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Health_Tracker.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePhotoToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoPath",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePhotoPath",
                table: "AspNetUsers");
        }
    }
}
