using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoffeeSchedule.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coffee_shops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OpeningTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ClosingTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coffee_shops", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Position = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PrimaryCoffeeShopId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employees_coffee_shops_PrimaryCoffeeShopId",
                        column: x => x.PrimaryCoffeeShopId,
                        principalTable: "coffee_shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_shop_accesses",
                columns: table => new
                {
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoffeeShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_shop_accesses", x => new { x.EmployeeId, x.CoffeeShopId });
                    table.ForeignKey(
                        name: "FK_employee_shop_accesses_coffee_shops_CoffeeShopId",
                        column: x => x.CoffeeShopId,
                        principalTable: "coffee_shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_shop_accesses_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_coffee_shops_Name",
                table: "coffee_shops",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_employee_shop_accesses_CoffeeShopId",
                table: "employee_shop_accesses",
                column: "CoffeeShopId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_Email",
                table: "employees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_Phone",
                table: "employees",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_PrimaryCoffeeShopId",
                table: "employees",
                column: "PrimaryCoffeeShopId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_shop_accesses");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "coffee_shops");
        }
    }
}
