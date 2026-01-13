using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Foodtrucks.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorStripeAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeAccountId",
                table: "Vendors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeAccountId",
                table: "Vendors");
        }
    }
}
