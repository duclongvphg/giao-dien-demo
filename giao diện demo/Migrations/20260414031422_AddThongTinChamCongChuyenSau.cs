using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace giao_diện_demo.Migrations
{
    /// <inheritdoc />
    public partial class AddThongTinChamCongChuyenSau : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "BonusAndAllowance",
                table: "Salaries",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BonusAndAllowance",
                table: "Salaries");
        }
    }
}
