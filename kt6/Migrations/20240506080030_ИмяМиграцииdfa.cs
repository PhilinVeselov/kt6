using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kt6.Migrations
{
    /// <inheritdoc />
    public partial class ИмяМиграцииdfa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", nullable: false),
                    LastName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    IDпроекта = table.Column<int>(name: "ID_проекта", type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Датаначала = table.Column<DateTime>(name: "Дата_начала", type: "TEXT", nullable: false),
                    Датаокончания = table.Column<DateTime>(name: "Дата_окончания", type: "TEXT", nullable: true),
                    Названиепроекта = table.Column<string>(name: "Название_проекта", type: "TEXT", nullable: false),
                    Описание = table.Column<string>(type: "TEXT", nullable: false),
                    Статус = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.IDпроекта);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                });
        }
    }
}
