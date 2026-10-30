using Microsoft.EntityFrameworkCore;
using TallerCapricornio.Models;

var builder = WebApplication.CreateBuilder(args);

// Servicios MVC
builder.Services.AddControllersWithViews();

// Conexión a SQL Server de MonsterASP
var connectionString =
    builder.Configuration.GetConnectionString("TallerConnection");

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<TallerDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// Construcción de la aplicación
var app = builder.Build();

// Configuración de errores y seguridad
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Página principal: listado de presupuestos
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Presupuestos}/{action=Index}/{id?}");

app.Run();