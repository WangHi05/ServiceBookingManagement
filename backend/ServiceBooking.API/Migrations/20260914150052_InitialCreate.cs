using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ServiceBooking.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Staffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staffs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkSchedules_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CustomerNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Users_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Services",
                columns: new[] { "Id", "Description", "DurationMinutes", "IsActive", "Name", "Price" },
                values: new object[,]
                {
                    { 1, "Cắt gọn gàng, tạo kiểu cơ bản", 30, true, "Cắt tóc nam", 100000m },
                    { 2, "Uốn nếp tự nhiên", 90, true, "Uốn tóc", 350000m },
                    { 3, "Nhuộm màu theo yêu cầu", 60, true, "Nhuộm tóc", 300000m },
                    { 4, "Massage thư giãn da đầu", 45, true, "Gội đầu dưỡng sinh", 150000m },
                    { 5, "Chăm sóc và làm sạch da mặt", 60, true, "Spa da mặt", 400000m }
                });

            migrationBuilder.InsertData(
                table: "Staffs",
                columns: new[] { "Id", "Email", "FullName", "IsActive" },
                values: new object[,]
                {
                    { 1, "staff1@bookingdemo.com", "Le Van Staff", true },
                    { 2, "staff2@bookingdemo.com", "Pham Thi Staff", true }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "Role" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 10, 8, 0, 0, 0, DateTimeKind.Utc), "admin@bookingdemo.com", "System Admin", true, "$2b$11$8eHBHt/uDocDWkhSuExsJeCCqHt0Zwi17kdIAxLV4kJG.Q1gcqgUK", "Admin" },
                    { 2, new DateTime(2026, 9, 10, 8, 0, 0, 0, DateTimeKind.Utc), "customer1@bookingdemo.com", "Nguyen Van A", true, "$2b$11$LdAU0Mx2RLKLlBT1ur3oK.du2VYk6.LKhdYV5Om4H64j4bdJncrWW", "Customer" },
                    { 3, new DateTime(2026, 9, 10, 8, 0, 0, 0, DateTimeKind.Utc), "customer2@bookingdemo.com", "Tran Thi B", true, "$2b$11$LdAU0Mx2RLKLlBT1ur3oK.du2VYk6.LKhdYV5Om4H64j4bdJncrWW", "Customer" }
                });

            migrationBuilder.InsertData(
                table: "Bookings",
                columns: new[] { "Id", "BookingCode", "CancellationReason", "CreatedAt", "CustomerId", "CustomerNote", "EndTime", "ServiceId", "StaffId", "StartTime", "Status" },
                values: new object[,]
                {
                    { 1, "BK000001", null, new DateTime(2026, 9, 10, 8, 0, 0, 0, DateTimeKind.Utc), 2, "Cắt gọn nhẹ", new DateTime(2026, 9, 15, 9, 30, 0, 0, DateTimeKind.Unspecified), 1, 1, new DateTime(2026, 9, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "Completed" },
                    { 2, "BK000002", null, new DateTime(2026, 9, 10, 8, 0, 0, 0, DateTimeKind.Utc), 2, null, new DateTime(2026, 9, 15, 11, 0, 0, 0, DateTimeKind.Unspecified), 3, 1, new DateTime(2026, 9, 15, 10, 0, 0, 0, DateTimeKind.Unspecified), "Confirmed" },
                    { 3, "BK000003", null, new DateTime(2026, 9, 10, 8, 0, 0, 0, DateTimeKind.Utc), 3, null, new DateTime(2026, 9, 15, 10, 30, 0, 0, DateTimeKind.Unspecified), 2, 2, new DateTime(2026, 9, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "Completed" },
                    { 4, "BK000004", "Khách bận đột xuất", new DateTime(2026, 9, 10, 8, 0, 0, 0, DateTimeKind.Utc), 3, null, new DateTime(2026, 9, 16, 8, 45, 0, 0, DateTimeKind.Unspecified), 4, 2, new DateTime(2026, 9, 16, 8, 0, 0, 0, DateTimeKind.Unspecified), "Cancelled" },
                    { 5, "BK000005", null, new DateTime(2026, 9, 10, 8, 0, 0, 0, DateTimeKind.Utc), 2, null, new DateTime(2026, 9, 16, 14, 0, 0, 0, DateTimeKind.Unspecified), 5, 1, new DateTime(2026, 9, 16, 13, 0, 0, 0, DateTimeKind.Unspecified), "Pending" },
                    { 6, "BK000006", null, new DateTime(2026, 9, 10, 8, 0, 0, 0, DateTimeKind.Utc), 3, null, new DateTime(2026, 9, 17, 9, 30, 0, 0, DateTimeKind.Unspecified), 1, 1, new DateTime(2026, 9, 17, 9, 0, 0, 0, DateTimeKind.Unspecified), "Confirmed" },
                    { 7, "BK000007", null, new DateTime(2026, 9, 10, 8, 0, 0, 0, DateTimeKind.Utc), 2, null, new DateTime(2026, 9, 17, 15, 0, 0, 0, DateTimeKind.Unspecified), 3, 2, new DateTime(2026, 9, 17, 14, 0, 0, 0, DateTimeKind.Unspecified), "Pending" },
                    { 8, "BK000008", "Đổi lịch khác", new DateTime(2026, 9, 10, 8, 0, 0, 0, DateTimeKind.Utc), 3, null, new DateTime(2026, 9, 18, 11, 30, 0, 0, DateTimeKind.Unspecified), 2, 1, new DateTime(2026, 9, 18, 10, 0, 0, 0, DateTimeKind.Unspecified), "Cancelled" },
                    { 9, "BK000009", null, new DateTime(2026, 9, 10, 8, 0, 0, 0, DateTimeKind.Utc), 2, null, new DateTime(2026, 9, 18, 15, 45, 0, 0, DateTimeKind.Unspecified), 4, 2, new DateTime(2026, 9, 18, 15, 0, 0, 0, DateTimeKind.Unspecified), "Confirmed" },
                    { 10, "BK000010", null, new DateTime(2026, 9, 10, 8, 0, 0, 0, DateTimeKind.Utc), 3, null, new DateTime(2026, 9, 19, 12, 0, 0, 0, DateTimeKind.Unspecified), 5, 1, new DateTime(2026, 9, 19, 11, 0, 0, 0, DateTimeKind.Unspecified), "Pending" }
                });

            migrationBuilder.InsertData(
                table: "WorkSchedules",
                columns: new[] { "Id", "EndTime", "StaffId", "StartTime", "WorkDate" },
                values: new object[,]
                {
                    { 1, new TimeOnly(17, 0, 0), 1, new TimeOnly(8, 0, 0), new DateOnly(2026, 9, 15) },
                    { 2, new TimeOnly(17, 0, 0), 2, new TimeOnly(8, 0, 0), new DateOnly(2026, 9, 15) },
                    { 3, new TimeOnly(17, 0, 0), 1, new TimeOnly(8, 0, 0), new DateOnly(2026, 9, 16) },
                    { 4, new TimeOnly(17, 0, 0), 2, new TimeOnly(8, 0, 0), new DateOnly(2026, 9, 16) },
                    { 5, new TimeOnly(17, 0, 0), 1, new TimeOnly(8, 0, 0), new DateOnly(2026, 9, 17) },
                    { 6, new TimeOnly(17, 0, 0), 2, new TimeOnly(8, 0, 0), new DateOnly(2026, 9, 17) },
                    { 7, new TimeOnly(17, 0, 0), 1, new TimeOnly(8, 0, 0), new DateOnly(2026, 9, 18) },
                    { 8, new TimeOnly(17, 0, 0), 2, new TimeOnly(8, 0, 0), new DateOnly(2026, 9, 18) },
                    { 9, new TimeOnly(17, 0, 0), 1, new TimeOnly(8, 0, 0), new DateOnly(2026, 9, 19) },
                    { 10, new TimeOnly(17, 0, 0), 2, new TimeOnly(8, 0, 0), new DateOnly(2026, 9, 19) },
                    { 11, new TimeOnly(17, 0, 0), 1, new TimeOnly(8, 0, 0), new DateOnly(2026, 9, 20) },
                    { 12, new TimeOnly(17, 0, 0), 2, new TimeOnly(8, 0, 0), new DateOnly(2026, 9, 20) },
                    { 13, new TimeOnly(17, 0, 0), 1, new TimeOnly(8, 0, 0), new DateOnly(2026, 9, 21) },
                    { 14, new TimeOnly(17, 0, 0), 2, new TimeOnly(8, 0, 0), new DateOnly(2026, 9, 21) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingCode",
                table: "Bookings",
                column: "BookingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerId",
                table: "Bookings",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ServiceId",
                table: "Bookings",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_StaffId_StartTime_EndTime",
                table: "Bookings",
                columns: new[] { "StaffId", "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status",
                table: "Bookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Services_Name",
                table: "Services",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_Email",
                table: "Staffs",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_StaffId_WorkDate",
                table: "WorkSchedules",
                columns: new[] { "StaffId", "WorkDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "WorkSchedules");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Staffs");
        }
    }
}
