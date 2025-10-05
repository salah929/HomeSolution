using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Home.Data.Migrations
{
    /// <inheritdoc />
    public partial class OrderItemId_NameChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SupplierOrderItemId",
                table: "SupplierOrderItems",
                newName: "OrderItemId");

            migrationBuilder.RenameColumn(
                name: "CustomerOrderItemId",
                table: "CustomerOrderItems",
                newName: "OrderItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrderItemId",
                table: "SupplierOrderItems",
                newName: "SupplierOrderItemId");

            migrationBuilder.RenameColumn(
                name: "OrderItemId",
                table: "CustomerOrderItems",
                newName: "CustomerOrderItemId");
        }
    }
}
