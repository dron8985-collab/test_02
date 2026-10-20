using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

namespace TasksApi_NET
{
    // Класс напоминания (трекер привычек)
    public class Reminder
    {
        public int Id { get; set; }
        public string Time { get; set; } = string.Empty;      // Формат "ЧЧ:ММ"
        public string Action { get; set; } = string.Empty;    // Что сделать
        public string Category { get; set; } = string.Empty;  // Категория
        public bool IsTriggered { get; set; }                 // Флаг, чтобы не дублировать логи каждую секунду
    }

    class Program
    {
        // Потокобезопасный список напоминаний
        private static readonly List<Reminder> Reminders = new List<Reminder>
        {
            new Reminder { Id = 1, Time = "21:00", Action = "Идти спать для здорового сна", Category = "Сон" },
            new Reminder { Id = 2, Time = "20:30", Action = "Почитать книгу перед сном", Category = "Саморазвитие" }
        };
        private static int _nextId = 3;
        private static bool _isRunning = true;

        static void Main(string[] args)
        {
            // Настройка веб-сервера (API)
            var builder = WebApplication.CreateBuilder(args);

            // Настраиваем порт 8080, как указано в вашем Dockerfile
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(8080);
            });

            var app = builder.Build();

            // Эндпоинт API: Возвращает список всех напоминаний
            app.MapGet("/api/reminders", () => Results.Ok(Reminders));

            // Запускаем веб-сервер в фоновом потоке, чтобы он не блокировал консольное меню
            Task.Run(() => app.Run());

            // Запуск фонового потока для отслеживания времени (текстовые логи)
            Task.Run(() => WatchTimeAndLog());

            // Главное консольное меню (работает локально на ПК, в Docker уходит в фоновое ожидание)
            if (!Console.IsInputRedirected)
            {
                while (_isRunning)
                {
                    Console.Clear();
                    Console.WriteLine($"=== ТРЕКЕР ПРИВЫЧЕК И НАПОМИНАНИЙ (.NET {Environment.Version.Major}) ===");
                    Console.WriteLine("1. Показать все напоминания");
                    Console.WriteLine("2. Добавить напоминание");
                    Console.WriteLine("3. Изменить напоминание");
                    Console.WriteLine("4. Удалить напоминание");
                    Console.WriteLine("5. Выход");
                    Console.Write("\nВыберите действие: ");

                    string? choice = Console.ReadLine();
                    if (choice == null) { _isRunning = false; break; }

                    switch (choice)
                    {
                        case "1": ShowReminders(); break;
                        case "2": AddReminder(); break;
                        case "3": EditReminder(); break;
                        case "4": DeleteReminder(); break;
                        case "5": _isRunning = false; break;
                        default:
                            Console.WriteLine("Неверный ввод. Нажмите любую клавишу...");
                            Console.ReadKey();
                            break;
                    }
                }
            }
            else
            {
                // Запуск внутри Docker-контейнера (без интерактивной консоли)
                Console.WriteLine("[SERVER MODE] Приложение успешно запущено внутри Docker-контейнера.");
                Console.WriteLine("[SERVER MODE] Web API доступно по порту 8080 на хосте.");
                while (_isRunning)
                {
                    Thread.Sleep(10000); // Держим поток живым, чтобы контейнер не выключался
                }
            }

            app.StopAsync().Wait();
        }

        // --- МЕТОДЫ УПРАВЛЕНИЯ НАПОМИНАНИЯМИ ---

        private static void ShowReminders()
        {
            Console.Clear();
            Console.WriteLine("=== ВАШИ НАПОМИНАНИЯ ===");
            lock (Reminders)
            {
                if (Reminders.Count == 0)
                {
                    Console.WriteLine("Список пуст.");
                }
                else
                {
                    foreach (var r in Reminders)
                    {
                        Console.WriteLine($"[{r.Id}] Время: {r.Time} | Действие: {r.Action} ({r.Category})");
                    }
                }
            }
            Console.WriteLine("\nНажмите любую клавишу для возврата в меню...");
            Console.ReadKey();
        }

        private static void AddReminder()
        {
            Console.Clear();
            Console.WriteLine("=== ДОБАВЛЕНИЕ НАПОМИНАНИЯ ===");
            Console.Write("Введите время (например, 21:00): ");
            string time = Console.ReadLine() ?? "";
            Console.Write("Введите текст напоминания: ");
            string action = Console.ReadLine() ?? "";
            Console.Write("Введите категорию: ");
            string category = Console.ReadLine() ?? "";

            lock (Reminders)
            {
                Reminders.Add(new Reminder { Id = _nextId++, Time = time, Action = action, Category = category });
            }
            Console.WriteLine("Напоминание успешно добавлено!");
            Thread.Sleep(1000);
        }

        private static void EditReminder()
        {
            Console.Clear();
            Console.WriteLine("=== ИЗМЕНЕНИЕ НАПОМИНАНИЯ ===");
            Console.Write("Введите ID напоминания для изменения: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                lock (Reminders)
                {
                    var reminder = Reminders.FirstOrDefault(r => r.Id == id);
                    if (reminder != null)
                    {
                        Console.Write($"Новое время (оставьте пустым для сохранения '{reminder.Time}'): ");
                        string time = Console.ReadLine() ?? "";
                        if (!string.IsNullOrEmpty(time)) reminder.Time = time;

                        Console.Write($"Новый текст (оставьте пустым для сохранения '{reminder.Action}'): ");
                        string action = Console.ReadLine() ?? "";
                        if (!string.IsNullOrEmpty(action)) reminder.Action = action;

                        Console.Write($"Новая категория (оставьте пустым для сохранения '{reminder.Category}'): ");
                        string category = Console.ReadLine() ?? "";
                        if (!string.IsNullOrEmpty(category)) reminder.Category = category;

                        reminder.IsTriggered = false;
                        Console.WriteLine("Изменения сохранены!");
                    }
                    else
                    {
                        Console.WriteLine("Напоминание с таким ID не найдено.");
                    }
                }
            }
            else
            {
                Console.WriteLine("Некорректный ID.");
            }
            Thread.Sleep(1500);
        }

        // --- УДАЛЕНИЕ НАПОМИНАНИЯ ---
        private static void DeleteReminder()
        {
            Console.Clear();
            Console.WriteLine("=== УДАЛЕНИЕ НАПОМИНАНИЯ ===");
            Console.Write("Введите ID напоминания для удаления: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                lock (Reminders)
                {
                    var reminder = Reminders.FirstOrDefault(r => r.Id == id);
                    if (reminder != null)
                    {
                        Reminders.Remove(reminder);
                        Console.WriteLine("Напоминание успешно удалено.");
                    }
                    else
                    {
                        Console.WriteLine("Напоминание не найдено.");
                    }
                }
            }
            else
            {
                Console.WriteLine("Некорректный ID.");
            }
            Thread.Sleep(1500);
        }

        // --- ФОНОВЫЙ ПОТОК: ТЕКСТОВЫЕ НАПОМИНАНИЯ В ЛОГ ---
        private static void WatchTimeAndLog()
        {
            while (_isRunning)
            {
                string currentTime = DateTime.Now.ToString("HH:mm");
                lock (Reminders)
                {
                    foreach (var r in Reminders)
                    {
                        if (r.Time == currentTime)
                        {
                            if (!r.IsTriggered)
                            {
                                r.IsTriggered = true;
                                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] [НАПОМИНАНИЕ] Пора делать: {r.Action} ({r.Category})!");
                            }
                        }
                        else
                        {
                            r.IsTriggered = false;
                        }
                    }
                }
                Thread.Sleep(5000); // Проверяем время каждые 5 секунд
            }
        }
    }
}
