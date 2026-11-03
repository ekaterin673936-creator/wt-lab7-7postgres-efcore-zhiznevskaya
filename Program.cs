using Microsoft.EntityFrameworkCore;
using wt_lab7_7postgres_efcore_zhiznevskaya.Data;
using wt_lab7_7postgres_efcore_zhiznevskaya.Models;

var builder = WebApplication.CreateBuilder(args);

// ===== ДИАГНОСТИКА (выведет в логи Render, что он видит) =====
Console.WriteLine("=== ПРОВЕРКА ПЕРЕМЕННЫХ ОКРУЖЕНИЯ ===");
Console.WriteLine($"DATABASE_URL (Environment): '{Environment.GetEnvironmentVariable("DATABASE_URL")}'");
Console.WriteLine($"DATABASE_URL (Configuration): '{builder.Configuration["DATABASE_URL"]}'");
Console.WriteLine("=======================================");

// ===== Подключение к PostgreSQL =====
var databaseUrl = builder.Configuration["DATABASE_URL"] 
                  ?? Environment.GetEnvironmentVariable("DATABASE_URL");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    Console.WriteLine("Переменная DATABASE_URL найдена! Преобразуем...");
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    
    connectionString = $"Host={uri.Host};" +
                       $"Port={uri.Port};" +
                       $"Database={uri.AbsolutePath.TrimStart('/')};" +
                       $"Username={Uri.UnescapeDataString(userInfo[0])};" +
                       $"Password={Uri.UnescapeDataString(userInfo[1])};" +
                       $"SSL Mode=Disable"; 
}
else
{
    Console.WriteLine("ВНИМАНИЕ: DATABASE_URL не найдена! Используем локальную строку.");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// ===== Настройка Swagger =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ===== Swagger UI =====
app.UseSwagger();
app.UseSwaggerUI();

// ===== Автоматическое применение миграций =====
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

// ===== Блок 4: CRUD через EF Core =====

// 1. GET /api/cars
app.MapGet("/api/cars", async (AppDbContext db) =>
{
    var cars = await db.Cars.ToListAsync();
    return Results.Ok(cars);
});

// 2. GET /api/cars/{id}
app.MapGet("/api/cars/{id}", async (int id, AppDbContext db) =>
{
    var car = await db.Cars.FirstOrDefaultAsync(x => x.Id == id);
    if (car == null) return Results.NotFound();
    return Results.Ok(car);
});

// 3. GET /api/cars/search
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

// 4. POST /api/cars
app.MapPost("/api/cars", async (Car car, AppDbContext db) =>
{
    db.Cars.Add(car);
    await db.SaveChangesAsync();
    return Results.Created($"/api/cars/{car.Id}", car);
});

// 5. PUT /api/cars/{id}
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

// 6. DELETE /api/cars/{id}
app.MapDelete("/api/cars/{id}", async (int id, AppDbContext db) =>
{
    var car = await db.Cars.FirstOrDefaultAsync(x => x.Id == id);
    if (car == null) return Results.NotFound();

    db.Cars.Remove(car);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// ===== Запуск приложения =====
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Run($"http://0.0.0.0:{port}");