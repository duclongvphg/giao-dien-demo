using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace giao_diện_demo.Migrations
{
    /// <inheritdoc />
    public partial class AddJobTitleToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "Employees");
        }
    }
}
