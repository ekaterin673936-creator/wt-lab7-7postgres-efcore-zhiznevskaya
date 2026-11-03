using Microsoft.EntityFrameworkCore;
using wt_lab7_7postgres_efcore_zhiznevskaya.Models;

namespace wt_lab7_7postgres_efcore_zhiznevskaya.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Таблица для автомобилей
    public DbSet<Car> Cars => Set<Car>();

    // ===== Начальные данные (Seed) =====
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Car>().HasData(
            new Car 
            { 
                Id = 1, 
                Brand = "Toyota", 
                Model = "Camry", 
                Year = 2022, 
                PricePerDay = 5000, 
                IsAvailable = true, 
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 1, 1), DateTimeKind.Utc) 
            },
            new Car 
            { 
                Id = 2, 
                Brand = "BMW", 
                Model = "X5", 
                Year = 2023, 
                PricePerDay = 12000, 
                IsAvailable = true, 
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 1, 2), DateTimeKind.Utc) 
            },
            new Car 
            { 
                Id = 3, 
                Brand = "Volvo", 
                Model = "FH16", 
                Year = 2021, 
                PricePerDay = 18000, 
                IsAvailable = false, 
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 1, 3), DateTimeKind.Utc) 
            }
        );
    }
}