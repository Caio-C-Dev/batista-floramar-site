using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<BatistaFloramar.Application.Services.YouTubeRssService>();
builder.Services.AddSingleton<BatistaFloramar.Application.Services.BibleService>();
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
                 path.EndsWith(".webp") || path.EndsWith(".svg") || path.EndsWith(".ico"))
        {
            // Imagens — cache de 7 dias
            headers.CacheControl = "public, max-age=604800";
        }
        else if (path.EndsWith(".xml") || path.EndsWith(".txt"))
        {
            // sitemap.xml e robots.txt — sem cache agressivo
            headers.CacheControl = "public, max-age=3600";
        }
    }
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
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
    }
    else
        await db.Database.MigrateAsync();
}

await SeedData.InicializarAsync(app.Services);

app.Run();
