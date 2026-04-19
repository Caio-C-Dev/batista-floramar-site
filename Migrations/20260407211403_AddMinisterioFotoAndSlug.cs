using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BatistaFloramar.Migrations
{
    /// <inheritdoc />
    public partial class AddMinisterioFotoAndSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResumoBreve",
                table: "Ministerios",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Ministerios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "MinisterioFotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MinisterioId = table.Column<int>(type: "int", nullable: false),
                    CaminhoArquivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Legenda = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DataUpload = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinisterioFotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MinisterioFotos_Ministerios_MinisterioId",
                        column: x => x.MinisterioId,
                        principalTable: "Ministerios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Popular slugs e resumos nos ministérios existentes antes de criar o índice único
            migrationBuilder.Sql(@"
                UPDATE Ministerios SET Slug = 'jovens',      ResumoBreve = 'Culto de Jovens e ações de impacto para a juventude.'                                            WHERE Nome = 'Jovens';
                UPDATE Ministerios SET Slug = 'kids',        ResumoBreve = 'Cuidando das crianças e bebês com amor enquanto os pais participam do culto.'                    WHERE Nome IN ('Infantil','Kids');
                UPDATE Ministerios SET Slug = 'louvor',      ResumoBreve = 'Liderando a congregação na adoração através da música.'                                          WHERE Nome = 'Louvor';
                UPDATE Ministerios SET Slug = 'batismo',     ResumoBreve = 'Preparando e celebrando o passo público da fé.'                                                  WHERE Nome IN ('Ensino','Batismo');
                UPDATE Ministerios SET Slug = 'acao-social', ResumoBreve = 'Expressando o amor de Cristo em ações concretas na comunidade.'                                  WHERE Nome = 'Ação Social';
                UPDATE Ministerios SET Slug = 'missoes',     ResumoBreve = 'Levando o Evangelho ao Brasil e às nações.'                                                      WHERE Nome = 'Missões';
                UPDATE Ministerios SET Slug = 'familia',     ResumoBreve = 'Fortalecendo lares e relacionamentos com base Bíblica.'                                          WHERE Nome = 'Família';
                UPDATE Ministerios SET Slug = 'midia',       ResumoBreve = 'Comunicando a mensagem da igreja com criatividade e alcance.'                                    WHERE Nome IN ('Comunicação','Mídia');
                UPDATE Ministerios SET Slug = 'na-palavra',  ResumoBreve = 'Vídeos, podcasts, gravações e entrevistas da igreja.'                                            WHERE Nome = 'Na Palavra';
                UPDATE Ministerios SET Slug = 'diaconato',   ResumoBreve = 'Cuidando da igreja e recebendo cada visitante com boas-vindas.'                                  WHERE Nome = 'Diaconato';
                -- Para linhas sem slug ainda (segurança)
                UPDATE Ministerios SET Slug = LOWER(REPLACE(REPLACE(Nome,' ','-'),'.','')) WHERE Slug = '' OR Slug IS NULL;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Ministerios_Slug",
                table: "Ministerios",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MinisterioFotos_MinisterioId",
                table: "MinisterioFotos",
                column: "MinisterioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MinisterioFotos");

            migrationBuilder.DropIndex(
                name: "IX_Ministerios_Slug",
                table: "Ministerios");

            migrationBuilder.DropColumn(
                name: "ResumoBreve",
                table: "Ministerios");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Ministerios");
        }
    }
}
