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
            bool pg = migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL";

            if (pg)
            {
                migrationBuilder.Sql(@"ALTER TABLE ""SeriesMensagens"" ADD COLUMN IF NOT EXISTS ""Slug"" VARCHAR(220);");
                migrationBuilder.Sql(@"ALTER TABLE ""PalavrasDoPastor"" ADD COLUMN IF NOT EXISTS ""Slug"" VARCHAR(320);");

                // Backfill: simple replace of spaces with hyphens + id suffix for uniqueness.
                // The application-level SlugHelper will regenerate proper slugs when records are edited.
                migrationBuilder.Sql(@"
                    UPDATE ""SeriesMensagens""
                    SET ""Slug"" = LOWER(REPLACE(REPLACE(REPLACE(""Nome"", ' ', '-'), '/', '-'), '.', ''))
                                  || '-' || CAST(""Id"" AS TEXT)
                    WHERE ""Slug"" IS NULL OR ""Slug"" = '';
                ");

                migrationBuilder.Sql(@"
                    UPDATE ""PalavrasDoPastor""
                    SET ""Slug"" = LOWER(REPLACE(REPLACE(REPLACE(""Titulo"", ' ', '-'), '/', '-'), '.', ''))
                                  || '-' || CAST(""Id"" AS TEXT)
                    WHERE ""Slug"" IS NULL OR ""Slug"" = '';
                ");

                migrationBuilder.Sql(@"ALTER TABLE ""SeriesMensagens"" ALTER COLUMN ""Slug"" SET NOT NULL;");
                migrationBuilder.Sql(@"ALTER TABLE ""PalavrasDoPastor"" ALTER COLUMN ""Slug"" SET NOT NULL;");

                migrationBuilder.Sql(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_SeriesMensagens_Slug"" ON ""SeriesMensagens"" (""Slug"");");
                migrationBuilder.Sql(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PalavrasDoPastor_Slug"" ON ""PalavrasDoPastor"" (""Slug"");");
            }
            else
            {
                // SQL Server
                migrationBuilder.Sql(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'SeriesMensagens') AND name = N'Slug')
                        ALTER TABLE [SeriesMensagens] ADD [Slug] NVARCHAR(220) NULL;
                ");
                migrationBuilder.Sql(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'PalavrasDoPastor') AND name = N'Slug')
                        ALTER TABLE [PalavrasDoPastor] ADD [Slug] NVARCHAR(320) NULL;
                ");

                migrationBuilder.Sql(@"
                    UPDATE [SeriesMensagens]
                    SET [Slug] = LOWER(REPLACE(REPLACE([Nome], ' ', '-'), '.', '')) + '-' + CAST([Id] AS NVARCHAR(20))
                    WHERE [Slug] IS NULL OR [Slug] = '';
                ");
                migrationBuilder.Sql(@"
                    UPDATE [PalavrasDoPastor]
                    SET [Slug] = LOWER(REPLACE(REPLACE([Titulo], ' ', '-'), '.', '')) + '-' + CAST([Id] AS NVARCHAR(20))
                    WHERE [Slug] IS NULL OR [Slug] = '';
                ");

                migrationBuilder.Sql(@"ALTER TABLE [SeriesMensagens] ALTER COLUMN [Slug] NVARCHAR(220) NOT NULL;");
                migrationBuilder.Sql(@"ALTER TABLE [PalavrasDoPastor] ALTER COLUMN [Slug] NVARCHAR(320) NOT NULL;");

                migrationBuilder.Sql(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'SeriesMensagens') AND name = N'IX_SeriesMensagens_Slug')
                        CREATE UNIQUE INDEX [IX_SeriesMensagens_Slug] ON [SeriesMensagens] ([Slug]);
                ");
                migrationBuilder.Sql(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'PalavrasDoPastor') AND name = N'IX_PalavrasDoPastor_Slug')
                        CREATE UNIQUE INDEX [IX_PalavrasDoPastor_Slug] ON [PalavrasDoPastor] ([Slug]);
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            bool pg = migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL";

            if (pg)
            {
                migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_SeriesMensagens_Slug"";");
                migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_PalavrasDoPastor_Slug"";");
                migrationBuilder.Sql(@"ALTER TABLE ""SeriesMensagens"" DROP COLUMN IF EXISTS ""Slug"";");
                migrationBuilder.Sql(@"ALTER TABLE ""PalavrasDoPastor"" DROP COLUMN IF EXISTS ""Slug"";");
            }
            else
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
}
