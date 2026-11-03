using Microsoft.EntityFrameworkCore;
using wt_lab7_7postgres_efcore_zhiznevskaya.Data;
using wt_lab7_7postgres_efcore_zhiznevskaya.Models;

var builder = WebApplication.CreateBuilder(args);

// ===== ДИАГНОСТИКА =====
Console.WriteLine("=== ПРОВЕРКА ПЕРЕМЕННЫХ ОКРУЖЕНИЯ ===");
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") ?? builder.Configuration["DATABASE_URL"];
Console.WriteLine($"DATABASE_URL: '{(string.IsNullOrEmpty(databaseUrl) ? "ПУСТО" : "НАЙДЕНО")}'");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    Console.WriteLine("Парсим DATABASE_URL вручную...");
    try 
    {
        // Убираем протокол (postgres:// или postgresql://)
        var cleanUrl = databaseUrl.Replace("postgresql://", "").Replace("postgres://", "");
        
        // Разделяем на user:pass и host/db
        var parts = cleanUrl.Split('@');
        var userInfo = parts[0].Split(':', 2);
        var hostAndDb = parts[1].Split('/', 2);
        
        // Разделяем host и port
        var hostAndPort = hostAndDb[0].Split(':', 2);

        string user = userInfo[0];
        string password = userInfo.Length > 1 ? userInfo[1] : "";
        string host = hostAndPort[0];
        string port = hostAndPort.Length > 1 ? hostAndPort[1] : "5432";
        string database = hostAndDb.Length > 1 ? hostAndDb[1] : "postgres";

        connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Disable";
        Console.WriteLine($"Успешно распарсено! Host={host}, Database={database}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ОШИБКА ПАРСИНГА URL: {ex.Message}");
    }
}
else
{
    Console.WriteLine("ВНИМАНИЕ: DATABASE_URL не найдена. Используем локальную строку.");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// ===== Swagger =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ===== Swagger UI =====
app.UseSwagger();
app.UseSwaggerUI();

// ===== Автоматическое применение миграций при запуске =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ===== Эндпоинты =====
app.MapGet("/", () => "Minimal API работает!");

app.MapGet("/api/info", () => new
{
    Application = "Web Technologies",
    Technology = "ASP.NET Core Minimal API + EF Core",
    Version = "2.0"
});

// ===== CRUD через EF Core =====

app.MapGet("/api/cars", async (AppDbContext db) =>
{
    var cars = await db.Cars.ToListAsync();
    return Results.Ok(cars);
});

app.MapGet("/api/cars/{id}", async (int id, AppDbContext db) =>
{
    var car = await db.Cars.FirstOrDefaultAsync(x => x.Id == id);
    if (car == null) return Results.NotFound();
    return Results.Ok(car);
});

app.MapGet("/api/cars/search", async (string? search, AppDbContext db) =>
{
    var query = db.Cars.AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
    {
        query = query.Where(x => x.Brand.ToLower().Contains(search.ToLower()));
    }
    var cars = await query.ToListAsync();
    return Results.Ok(cars);
});

app.MapPost("/api/cars", async (Car car, AppDbContext db) =>
{
    db.Cars.Add(car);
    await db.SaveChangesAsync();
    return Results.Created($"/api/cars/{car.Id}", car);
});

app.MapPut("/api/cars/{id}", async (int id, Car updatedCar, AppDbContext db) =>
{
    var car = await db.Cars.FirstOrDefaultAsync(x => x.Id == id);
    if (car == null) return Results.NotFound();

    car.Brand = updatedCar.Brand;
    car.Model = updatedCar.Model;
    car.Year = updatedCar.Year;
    car.PricePerDay = updatedCar.PricePerDay;
    car.IsAvailable = updatedCar.IsAvailable;

    await db.SaveChangesAsync();
    return Results.Ok(car);
});

app.MapDelete("/api/cars/{id}", async (int id, AppDbContext db) =>
{
    var car = await db.Cars.FirstOrDefaultAsync(x => x.Id == id);
    if (car == null) return Results.NotFound();

    db.Cars.Remove(car);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// ===== Запуск =====
// Используем переменную appPort, чтобы не было конфликта имен
var appPort = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Run($"http://0.0.0.0:{appPort}");