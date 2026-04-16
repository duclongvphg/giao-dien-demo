using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace giao_diện_demo.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPositionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobPositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Group = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPositions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "JobPositions",
                columns: new[] { "Id", "Group", "MinSalary", "Name" },
                values: new object[,]
                {
                    { 1, "1. Ban Giám đốc", 60000000m, "Giám đốc điều hành (CEO)" },
                    { 2, "1. Ban Giám đốc", 50000000m, "Giám đốc Kỹ thuật (CTO)" },
                    { 3, "1. Ban Giám đốc", 40000000m, "Giám đốc Nhân sự (CHRO)" },
                    { 4, "2. Cấp Quản lý", 30000000m, "Quản lý Dự án (PM)" },
                    { 5, "2. Cấp Quản lý", 30000000m, "Trưởng phòng Kỹ thuật" },
                    { 6, "2. Cấp Quản lý", 20000000m, "Trưởng phòng HR / Sales" },
                    { 7, "3. Cấp Nhân viên", 25000000m, "Lập trình viên (Senior)" },
                    { 8, "3. Cấp Nhân viên", 12000000m, "Lập trình viên (Junior/Mid)" },
                    { 9, "3. Cấp Nhân viên", 15000000m, "Phân tích nghiệp vụ (BA)" },
                    { 10, "3. Cấp Nhân viên", 10000000m, "Kiểm thử phần mềm (Tester/QA)" },
                    { 11, "4. Khối Back-Office", 9000000m, "Chuyên viên Nhân sự" },
                    { 12, "4. Khối Back-Office", 9000000m, "Kế toán viên" },
                    { 13, "4. Khối Back-Office", 7000000m, "Nhân viên Kinh doanh (Sales)" },
                    { 14, "5. Thực tập sinh", 3000000m, "Thực tập sinh (Intern)" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobPositions");
        }
    }
}
