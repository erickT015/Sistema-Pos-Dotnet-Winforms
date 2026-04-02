using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaPOS.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposPorcentajeYGananciaProductos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GananciaUnitaria",
                table: "Productos",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeGanancia",
                table: "Productos",
                type: "decimal(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GananciaUnitaria",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "PorcentajeGanancia",
                table: "Productos");
        }
    }
}
