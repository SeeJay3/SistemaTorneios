using Microsoft.EntityFrameworkCore;
using TournamentSystem.Data;
using TournamentSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Configura��o da connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=TournamentSystemDb;Trusted_Connection=true;MultipleActiveResultSets=true";

// Configura��o do Entity Framework (SEM Identity)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Registro de servi�os customizados
builder.Services.AddHttpClient<RiotApiService>();
builder.Services.AddScoped<TournamentService>();
builder.Services.AddScoped<RiotApiService>();

// Configura��o MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configura��o da pipeline de middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// REMOVIDO: app.UseAuthentication();
// REMOVIDO: app.UseAuthorization();

// Configura��o das rotas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Inicializa��o do banco de dados
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        TournamentDbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erro ao inicializar banco de dados.");
    }
}

app.Run();