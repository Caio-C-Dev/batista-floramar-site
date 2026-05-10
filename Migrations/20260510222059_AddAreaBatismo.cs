using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BatistaFloramar.Migrations
{
    /// <inheritdoc />
    public partial class AddAreaBatismo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AulasBatismo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NumeroAula = table.Column<int>(type: "int", nullable: false),
                    DataAula = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProfessorNome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Observacoes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AulasBatismo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BatizadosHistorico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DataBatismo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WhatsApp = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Observacoes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatizadosHistorico", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PresencasAulaBatismo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AulaBatismoId = table.Column<int>(type: "int", nullable: false),
                    NomePessoa = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Presente = table.Column<bool>(type: "bit", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresencasAulaBatismo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PresencasAulaBatismo_AulasBatismo_AulaBatismoId",
                        column: x => x.AulaBatismoId,
                        principalTable: "AulasBatismo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PresencasAulaBatismo_AulaBatismoId",
                table: "PresencasAulaBatismo",
                column: "AulaBatismoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BatizadosHistorico");

            migrationBuilder.DropTable(
                name: "PresencasAulaBatismo");

            migrationBuilder.DropTable(
                name: "AulasBatismo");
        }
    }
}
