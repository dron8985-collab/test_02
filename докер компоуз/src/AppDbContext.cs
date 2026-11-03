using Microsoft.EntityFrameworkCore;

namespace HabitsApi;

public class HabitReminder
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;       // Название привычки (например: Зарядка)
    public string ReminderText { get; set; } = string.Empty; // Текст напоминания (например: Вставай из-за стола!)
    public string ReminderTime { get; set; } = string.Empty; // Время (например: 09:00)
    public bool IsCompletedToday { get; set; }               // Закреплена ли сегодня
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<HabitReminder> Habits => Set<HabitReminder>();
}
