using BatistaFloramar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<BatistaFloramar.Application.Services.YouTubeRssService>();
builder.Services.AddSingleton<BatistaFloramar.Application.Services.BibleService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<BatistaFloramar.Application.Services.FinanceiroService>();
builder.Services.AddScoped<BatistaFloramar.Application.Services.DoacaoService>();
builder.Services.AddDbContext<BatistaFloramarDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication("AdminCookie")
    .AddCookie("AdminCookie", options =>
    {
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "api-docs";
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Redirecionar raiz para Home/Index
app.MapGet("/", async (context) =>
{
    context.Response.Redirect("/Home/Index");
    await Task.CompletedTask;
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await SeedData.InicializarAsync(app.Services);

app.Run();
