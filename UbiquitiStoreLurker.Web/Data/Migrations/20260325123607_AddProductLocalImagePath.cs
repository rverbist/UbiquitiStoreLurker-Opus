using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UbiquitiStoreLurker.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductLocalImagePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocalImagePath",
                table: "Products",
                type: "TEXT",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocalImagePath",
                table: "Products");
        }
    }
}
