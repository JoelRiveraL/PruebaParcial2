using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prueba2Hotel.Migrations
{
    /// <inheritdoc />
    public partial class ActualizacionReservas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CedulaCliente",
                table: "Reserva",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumHabitacion",
                table: "Reserva",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CedulaCliente",
                table: "Reserva");

            migrationBuilder.DropColumn(
                name: "NumHabitacion",
                table: "Reserva");
        }
    }
}
