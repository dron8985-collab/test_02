using Microsoft.EntityFrameworkCore;
using HabitsApi;

var builder = WebApplication.CreateBuilder(args);


var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated(); 
}

// 1.Посмотреть 
app.MapGet("/habits", async (AppDbContext db) => 
    await db.Habits.ToListAsync());

// 2.Добавить 
app.MapPost("/habits", async (HabitReminder habit, AppDbContext db) =>
{
    db.Habits.Add(habit);
    await db.SaveChangesAsync();
    return Results.Created($"/habits/{habit.Id}", habit);
});

// 3.Обновить 
app.MapPut("/habits/{id}", async (int id, HabitReminder inputHabit, AppDbContext db) =>
{
    var habit = await db.Habits.FindAsync(id);
    if (habit is null) return Results.NotFound();

    habit.Title = inputHabit.Title;
    habit.ReminderText = inputHabit.ReminderText;
    habit.ReminderTime = inputHabit.ReminderTime;
    habit.IsCompletedToday = inputHabit.IsCompletedToday;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

// 4. Удалить 
app.MapDelete("/habits/{id}", async (int id, AppDbContext db) =>
{
    var habit = await db.Habits.FindAsync(id);
    if (habit is null) return Results.NotFound();

    db.Habits.Remove(habit);
    await db.SaveChangesAsync();
    return Results.Ok(habit);
});

app.Run();
