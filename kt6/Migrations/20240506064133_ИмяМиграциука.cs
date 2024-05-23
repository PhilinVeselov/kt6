using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kt6.Migrations
{
    /// <inheritdoc />
    public partial class ИмяМиграциука : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    IDпроекта = table.Column<int>(name: "ID_проекта", type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Названиепроекта = table.Column<string>(name: "Название_проекта", type: "TEXT", nullable: false),
                    Описание = table.Column<string>(type: "TEXT", nullable: false),
                    Статус = table.Column<string>(type: "TEXT", nullable: false),
                    Датаначала = table.Column<DateTime>(name: "Дата_начала", type: "TEXT", nullable: false),
                    Датаокончания = table.Column<DateTime>(name: "Дата_окончания", type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.IDпроекта);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
