using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BatistaFloramar.Migrations
{
    /// <inheritdoc />
    public partial class AddSlugsToPalavraESeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Slug to SeriesMensagens (idempotent — skips if column already exists)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'SeriesMensagens') AND name = N'Slug'
                )
                BEGIN
                    ALTER TABLE [SeriesMensagens] ADD [Slug] NVARCHAR(220) NULL;
                END
            ");

            // Add Slug to PalavrasDoPastor (idempotent)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'PalavrasDoPastor') AND name = N'Slug'
                )
                BEGIN
                    ALTER TABLE [PalavrasDoPastor] ADD [Slug] NVARCHAR(320) NULL;
                END
            ");

            // Backfill SeriesMensagens: only rows without a real slug yet
            migrationBuilder.Sql(@"
                UPDATE [SeriesMensagens]
                SET [Slug] = LOWER(
                    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                    [Nome],
                    N' ', N'-'), N'ã', N'a'), N'â', N'a'), N'á', N'a'), N'à', N'a'),
                    N'ê', N'e'), N'é', N'e'), N'è', N'e'), N'ô', N'o'), N'ó', N'o'),
                    N'õ', N'o'), N'ú', N'u'), N'ü', N'u'), N'í', N'i'), N'ç', N'c'),
                    N'ñ', N'n'), N',', N''), N'.', N''), N'!', N''), N'?', N''))
                + N'-' + CAST([Id] AS NVARCHAR(20))
                WHERE [Slug] IS NULL OR [Slug] = N''
            ");

            // Backfill PalavrasDoPastor: only rows without a real slug yet
            migrationBuilder.Sql(@"
                UPDATE [PalavrasDoPastor]
                SET [Slug] = LOWER(
                    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                    [Titulo],
                    N' ', N'-'), N'ã', N'a'), N'â', N'a'), N'á', N'a'), N'à', N'a'),
                    N'ê', N'e'), N'é', N'e'), N'è', N'e'), N'ô', N'o'), N'ó', N'o'),
                    N'õ', N'o'), N'ú', N'u'), N'ü', N'u'), N'í', N'i'), N'ç', N'c'),
                    N'ñ', N'n'), N',', N''), N'.', N''), N'!', N''), N'?', N''))
                + N'-' + CAST([Id] AS NVARCHAR(20))
                WHERE [Slug] IS NULL OR [Slug] = N''
            ");

            // Make NOT NULL now that every row has a value
            migrationBuilder.Sql(@"
                ALTER TABLE [SeriesMensagens] ALTER COLUMN [Slug] NVARCHAR(220) NOT NULL;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE [PalavrasDoPastor] ALTER COLUMN [Slug] NVARCHAR(320) NOT NULL;
            ");

            // Create unique indexes (idempotent)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'SeriesMensagens') AND name = N'IX_SeriesMensagens_Slug'
                )
                BEGIN
                    CREATE UNIQUE INDEX [IX_SeriesMensagens_Slug] ON [SeriesMensagens] ([Slug]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'PalavrasDoPastor') AND name = N'IX_PalavrasDoPastor_Slug'
                )
                BEGIN
                    CREATE UNIQUE INDEX [IX_PalavrasDoPastor_Slug] ON [PalavrasDoPastor] ([Slug]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'SeriesMensagens') AND name = N'IX_SeriesMensagens_Slug')
                    DROP INDEX [IX_SeriesMensagens_Slug] ON [SeriesMensagens];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'PalavrasDoPastor') AND name = N'IX_PalavrasDoPastor_Slug')
                    DROP INDEX [IX_PalavrasDoPastor_Slug] ON [PalavrasDoPastor];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'SeriesMensagens') AND name = N'Slug')
                    ALTER TABLE [SeriesMensagens] DROP COLUMN [Slug];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'PalavrasDoPastor') AND name = N'Slug')
                    ALTER TABLE [PalavrasDoPastor] DROP COLUMN [Slug];
            ");
        }
    }
}
