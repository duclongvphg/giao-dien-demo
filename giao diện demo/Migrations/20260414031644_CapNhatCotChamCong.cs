using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace giao_diện_demo.Migrations
{
    /// <inheritdoc />
    public partial class CapNhatCotChamCong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LateMinutes",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "OvertimeHours",
                table: "Attendances",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "StandardHours",
                table: "Attendances",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "StatusNote",
                table: "Attendances",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LateMinutes",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "OvertimeHours",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "StandardHours",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "StatusNote",
                table: "Attendances");
        }
    }
}
