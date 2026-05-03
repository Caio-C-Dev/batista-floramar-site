using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Compressão Brotli + Gzip para HTML, JSON, CSS, JS
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "text/html", "text/css", "application/javascript",
        "application/json", "image/svg+xml", "text/plain"
    });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<BatistaFloramar.Application.Services.YouTubeRssService>();
builder.Services.AddHttpClient<BatistaFloramar.Application.Services.BibleService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<BatistaFloramar.Application.Services.FinanceiroService>();
builder.Services.AddScoped<BatistaFloramar.Application.Services.DoacaoService>();

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (databaseUrl != null)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var connStr = $"Host={uri.Host};Port={uri.Port};"
        + $"Username={userInfo[0]};Password={Uri.UnescapeDataString(userInfo[1])};"
        + $"Database={uri.AbsolutePath.TrimStart('/')};SSL Mode=Require;Trust Server Certificate=true";
    builder.Services.AddDbContext<BatistaFloramarDbContext>(options => options.UseNpgsql(connStr));
}
else
{
    builder.Services.AddDbContext<BatistaFloramarDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

builder.Services.AddAuthentication("AdminCookie")
    .AddCookie("AdminCookie", options =>
    {
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
    })
    .AddCookie("LiderCookie", options =>
    {
        options.LoginPath = "/Lider/Login";
        options.AccessDeniedPath = "/Lider/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = "LiderSession";
    });

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "api-docs";
    });
}

// Forçar www + HTTPS em produção (deve ser o primeiro middleware do pipeline)
if (app.Environment.IsProduction())
{
    app.Use(async (context, next) =>
    {
        var host = context.Request.Host.Host;
        var path = context.Request.Path.Value ?? string.Empty;

        // BYPASS healthcheck: Railway/Cloudflare/uptime monitors batem em hosts/paths
        // diferentes (ex: healthcheck.railway.app/health). Não redirecionar.
        if (path.Equals("/health", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/healthz", StringComparison.OrdinalIgnoreCase) ||
            host.Contains("healthcheck", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        // Railway termina TLS na borda e repassa como HTTP internamente.
        // Usar X-Forwarded-Proto para saber o scheme real do cliente.
        var realScheme = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault()
                         ?? context.Request.Scheme;

        // Se o host não começa com "www.", redirecionar 301 para https://www.
        if (!host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            var wwwHost = new HostString("www." + host);
            var redirect = UriHelper.BuildAbsolute(
                scheme: "https",
                host: wwwHost,
                pathBase: context.Request.PathBase,
                path: context.Request.Path,
                query: context.Request.QueryString);

            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            context.Response.Headers.Location = redirect;
            return;
        }

        // Se chegou com www mas via HTTP real (não interno do Railway), forçar HTTPS
        if (realScheme.Equals("http", StringComparison.OrdinalIgnoreCase))
        {
            var redirect = UriHelper.BuildAbsolute(
                scheme: "https",
                host: context.Request.Host,
                pathBase: context.Request.PathBase,
                path: context.Request.Path,
                query: context.Request.QueryString);

            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            context.Response.Headers.Location = redirect;
            return;
        }

        await next();
    });
}

// Arquivos estáticos com cache longo para CSS, JS e imagens (Core Web Vitals)
app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.File.Name;
        var headers = ctx.Context.Response.Headers;
        if (path.EndsWith(".css") || path.EndsWith(".js") ||
            path.EndsWith(".woff2") || path.EndsWith(".woff") || path.EndsWith(".ttf"))
        {
            // CSS/JS versionados pelo asp-append-version — cache de 1 ano
            headers.CacheControl = "public, max-age=31536000, immutable";
        }
        else if (path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".jpeg") ||
                 path.EndsWith(".webp") || path.EndsWith(".svg") || path.EndsWith(".ico") ||
                 path.EndsWith(".jfif") || path.EndsWith(".gif") || path.EndsWith(".avif"))
        {
            // Imagens — cache de 1 ano (uploads usam GUIDs, estáticas raramente mudam)
            headers.CacheControl = "public, max-age=31536000, immutable";
        }
        else if (path.EndsWith(".xml") || path.EndsWith(".txt"))
        {
            // sitemap.xml e robots.txt — sem cache agressivo
            headers.CacheControl = "public, max-age=3600";
        }
    }
});
app.UseStatusCodePagesWithReExecute("/erro/{0}");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Healthcheck leve — não toca DB. Garante 200 instantâneo pro Railway/uptime.
app.MapGet("/health", () => Results.Text("OK"));
app.MapGet("/healthz", () => Results.Text("OK"));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Migrations + seed em BACKGROUND — app sobe instantâneo, migrations rodam paralelo.
// Sem isso: Railway healthcheck timeout enquanto SQL slug/seed roda.
_ = Task.Run(async () =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BatistaFloramarDbContext>();
        if (databaseUrl != null)
        {
            await db.Database.EnsureCreatedAsync();
        // Cria tabelas novas que EnsureCreated não adiciona em bancos já existentes
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""EventosSemanais"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Titulo"" VARCHAR(200) NOT NULL,
                ""DiaSemana"" VARCHAR(20) NOT NULL,
                ""Horario"" VARCHAR(30) NOT NULL,
                ""Descricao"" VARCHAR(500),
                ""Ativo"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""Ordem"" INTEGER NOT NULL DEFAULT 0,
                ""DataCriacao"" TIMESTAMP NOT NULL
            )
        ");
        // Adiciona colunas novas em tabelas existentes (EnsureCreated não faz isso)
        await db.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""AdminCredenciais"" ADD COLUMN IF NOT EXISTS ""Role"" VARCHAR(100) NULL;
        ");
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""SolicitacoesBatismo"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Nome"" VARCHAR(150) NOT NULL,
                ""WhatsApp"" VARCHAR(30) NOT NULL,
                ""Email"" VARCHAR(180) NOT NULL,
                ""Tipo"" VARCHAR(30) NOT NULL DEFAULT 'Batismo',
                ""Mensagem"" VARCHAR(1000),
                ""DataEnvio"" TIMESTAMP NOT NULL,
                ""Atendido"" BOOLEAN NOT NULL DEFAULT FALSE
            )
        ");
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""GaleriaAlbuns"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Nome"" VARCHAR(200) NOT NULL,
                ""Descricao"" VARCHAR(1000),
                ""Data"" TIMESTAMP NOT NULL,
                ""CriadoEm"" TIMESTAMP NOT NULL
            )
        ");
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""GaleriaFotos"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""CaminhoArquivo"" VARCHAR(500) NOT NULL,
                ""Legenda"" VARCHAR(300),
                ""AlbumId"" INTEGER NOT NULL REFERENCES ""GaleriaAlbuns""(""Id"") ON DELETE CASCADE
            )
        ");
        await db.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""Celulas"" ADD COLUMN IF NOT EXISTS ""LiderNome"" VARCHAR(150) NULL;
        ");
        await db.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""PalavrasDoPastor"" ADD COLUMN IF NOT EXISTS ""Slug"" VARCHAR(320);
        ");
        await db.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""SeriesMensagens"" ADD COLUMN IF NOT EXISTS ""Slug"" VARCHAR(220);
        ");
        await db.Database.ExecuteSqlRawAsync(@"
            UPDATE ""PalavrasDoPastor""
            SET ""Slug"" = LOWER(REPLACE(REPLACE(REPLACE(""Titulo"", ' ', '-'), '/', '-'), '.', ''))
                          || '-' || CAST(""Id"" AS TEXT)
            WHERE ""Slug"" IS NULL OR ""Slug"" = '';
        ");
        await db.Database.ExecuteSqlRawAsync(@"
            UPDATE ""SeriesMensagens""
            SET ""Slug"" = LOWER(REPLACE(REPLACE(REPLACE(""Nome"", ' ', '-'), '/', '-'), '.', ''))
                          || '-' || CAST(""Id"" AS TEXT)
            WHERE ""Slug"" IS NULL OR ""Slug"" = '';
        ");
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PalavrasDoPastor_Slug"" ON ""PalavrasDoPastor"" (""Slug"");
        ");
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_SeriesMensagens_Slug"" ON ""SeriesMensagens"" (""Slug"");
        ");
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Integrantes"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Nome"" VARCHAR(150) NOT NULL,
                ""CelulaId"" INTEGER NOT NULL REFERENCES ""Celulas""(""Id"") ON DELETE CASCADE,
                ""Ativo"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""Visitante"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""DataIngresso"" TIMESTAMP NOT NULL
            )
        ");
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Presencas"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Data"" TIMESTAMP NOT NULL,
                ""CelulaId"" INTEGER NOT NULL REFERENCES ""Celulas""(""Id"") ON DELETE CASCADE,
                ""Tipo"" VARCHAR(30) NOT NULL DEFAULT 'Normal',
                ""CriadoEm"" TIMESTAMP NOT NULL
            )
        ");
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""PresencasDetalhes"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""PresencaId"" INTEGER NOT NULL REFERENCES ""Presencas""(""Id"") ON DELETE CASCADE,
                ""IntegranteId"" INTEGER NOT NULL REFERENCES ""Integrantes""(""Id"") ON DELETE RESTRICT,
                ""Presente"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""Justificativa"" VARCHAR(500)
            )
        ");
        }
        else
            await db.Database.MigrateAsync();

        await SeedData.InicializarAsync(app.Services);
        Console.WriteLine("[Startup] Migrations + Seed concluídos OK");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Startup] ERRO em migrations/seed (background): {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
});

app.Run();
