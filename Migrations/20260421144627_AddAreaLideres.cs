using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BatistaFloramar.Migrations
{
    /// <inheritdoc />
    public partial class AddAreaLideres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LiderNome",
                table: "Celulas",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Integrantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CelulaId = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    Visitante = table.Column<bool>(type: "bit", nullable: false),
                    DataIngresso = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Integrantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Integrantes_Celulas_CelulaId",
                        column: x => x.CelulaId,
                        principalTable: "Celulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Presencas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CelulaId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Presencas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Presencas_Celulas_CelulaId",
                        column: x => x.CelulaId,
                        principalTable: "Celulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PresencasDetalhes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PresencaId = table.Column<int>(type: "int", nullable: false),
                    IntegranteId = table.Column<int>(type: "int", nullable: false),
                    Presente = table.Column<bool>(type: "bit", nullable: false),
                    Justificativa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresencasDetalhes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PresencasDetalhes_Integrantes_IntegranteId",
                        column: x => x.IntegranteId,
                        principalTable: "Integrantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresencasDetalhes_Presencas_PresencaId",
                        column: x => x.PresencaId,
                        principalTable: "Presencas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Integrantes_CelulaId",
                table: "Integrantes",
                column: "CelulaId");

            migrationBuilder.CreateIndex(
                name: "IX_Presencas_CelulaId",
                table: "Presencas",
                column: "CelulaId");

            migrationBuilder.CreateIndex(
                name: "IX_PresencasDetalhes_IntegranteId",
                table: "PresencasDetalhes",
                column: "IntegranteId");

            migrationBuilder.CreateIndex(
                name: "IX_PresencasDetalhes_PresencaId",
                table: "PresencasDetalhes",
                column: "PresencaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PresencasDetalhes");

            migrationBuilder.DropTable(
                name: "Integrantes");

            migrationBuilder.DropTable(
                name: "Presencas");

            migrationBuilder.DropColumn(
                name: "LiderNome",
                table: "Celulas");
        }
    }
}
