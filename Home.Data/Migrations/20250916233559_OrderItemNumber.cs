using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Home.Data.Migrations
{
    /// <inheritdoc />
    public partial class OrderItemNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderItemNumber",
                table: "SupplierOrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrderItemNumber",
                table: "CustomerOrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderItemNumber",
                table: "SupplierOrderItems");

            migrationBuilder.DropColumn(
                name: "OrderItemNumber",
                table: "CustomerOrderItems");
        }
    }
}
