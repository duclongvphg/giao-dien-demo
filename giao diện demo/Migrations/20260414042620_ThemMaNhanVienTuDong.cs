using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace giao_diện_demo.Migrations
{
    /// <inheritdoc />
    public partial class ThemMaNhanVienTuDong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeCode",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployeeCode",
                table: "Employees");
        }
    }
}
