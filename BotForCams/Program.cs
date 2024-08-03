using System;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using yoomoney_api.authorize;


using Microsoft.EntityFrameworkCore;
using System.Runtime.Intrinsics.X86;
using Npgsql;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Telegram.Bot.Types.ReplyMarkups;
using System.Security.Cryptography.X509Certificates;
using System.Numerics;
using System.Collections.Generic;
using System.Xml.Linq;

public class Info
{
    public string name { get; set; }
    public int id { get; set; }
    public bool stastus { get; set; }

    public DateTime datetame {  get; set; }
}

public class ApplicationContext : DbContext
{
    public DbSet<Info> Info { get; set; } = null!;
    public ApplicationContext()
    {
        Database.EnsureCreated();
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=testDB;Username=postgres;Password=nikitos");
    }
    public void CreateDynamicTabl(string tableName)
    {
        var createTableQuery = $@"
            CREATE TABLE IF NOT EXISTS ""{tableName}"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""MessageText"" TEXT NOT NULL,
                ""SentAt"" TIMESTAMP NOT NULL
            )";

        Database.ExecuteSqlRaw(createTableQuery);
    }

    public void InfoTable(string name, int id, bool stastus, DateTime datetame)
    {
        var createTable = $@"
            INSERT INTO info (""id"", ""name"", ""stastus"", ""datetame"")
            VALUES (@p0, @p1, @p2, @p3)
            ON CONFLICT (""id"") 
            DO NOTHING";

        Database.ExecuteSqlRaw(createTable, id, name, stastus, datetame);
    }

    public void AddUserMessage(string tableName, string messageText)
    {
        var addMessageQuery = $@"
            INSERT INTO ""{tableName}"" (""MessageText"", ""SentAt"")
            VALUES (@p0, @p1)";

        Database.ExecuteSqlRaw(addMessageQuery, messageText, DateTime.UtcNow);
    }
    public void MakingAPayment(long userId)
    {
        var updateStatusQuery = @"
            UPDATE info
            SET ""stastus"" = @p0
            WHERE ""id"" = @p1";

        Database.ExecuteSqlRaw(updateStatusQuery, true, userId);
    }

    public List<long> GetAllUserIds()
    {
        var userIds = new List<long>();
        using (var command = Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "SELECT id FROM info";
            Database.OpenConnection();
            using (var result = command.ExecuteReader())
            {
                while (result.Read())
                {
                    userIds.Add(result.GetInt64(0));
                }
            }
        }
        return userIds;
    }
}

class Program
{
    static ITelegramBotClient botClient;

    static void Main()
    {
        bool tr = true;
        

        botClient = new TelegramBotClient("7242370794:AAFrI45C08puMjeYkthCHKVPSr1mWg0i4uE");
        ApplicationContext db = new ApplicationContext();
        db.SaveChangesAsync();

        var me = botClient.GetMeAsync().Result;
        Console.WriteLine("Запущен бот " + botClient.GetMeAsync().Result.FirstName + " команды бота: sendall, exit;");


        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { }
        };
        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions
        );
        while (tr)
        {
            string txt = Console.ReadLine();

            if (txt == "sendall")
            {
                Console.WriteLine("Введите сообщение для всех пользователей:");
                string messageForAll = Console.ReadLine();
                SendMessageToAllUsers(messageForAll).Wait();
            }
            else if (txt == "exit")
            {
                tr = false;
            }
            else
            {
                Console.WriteLine("Неизвестная команда");
            }
        }
        static async Task SendMessageToAllUsers(string message)
        {
            ApplicationContext db = new ApplicationContext();
            var userIds = db.GetAllUserIds();

            foreach (var userId in userIds)
            {
                try
                {
                    await botClient.SendTextMessageAsync(userId, message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Не удалось отправить сообщение пользователю {userId}: {ex.Message}");
                }
            }
        }
    }


    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        //string connnect = "Host=localhost;Port=5432;Database=testDB;Username=postgres;Password=nikitos";
        var message = update.Message;
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
        
        List<string> Listcommsands= new List<string> {"/donate", "/help" };

        if (update.Type == UpdateType.Message)
        {
            if (update.Message.Type == MessageType.Text)
            {
                string text = message.Text;
                string username = message.From.Username;
                int userId = Convert.ToInt32(message.From.Id);
                string tableName = $"{username}_{userId}";
                DateTime dateTime = DateTime.Now;
                DateTime utcNow = dateTime.ToUniversalTime();
                ApplicationContext db = new ApplicationContext();
                string commsands = System.String.Join(", ", Listcommsands);

                InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                new []
                        {
                            InlineKeyboardButton.WithCallbackData("да", "yes"),
                            InlineKeyboardButton.WithCallbackData("нет", "no")
                        }
                });


                db.CreateDynamicTabl(tableName);
                db.AddUserMessage(tableName, text);
                db.InfoTable(username, userId, false, utcNow);

                if (text == "/start")
                {
                    await botClient.SendTextMessageAsync(
                       chatId: message.Chat.Id,
                       text: "Привтствую тебя дорогой пользователь! Вот список доступных комаанд: " + commsands,
                       cancellationToken: cancellationToken);

                }

                else if (text == "/help")
                {
                    await botClient.SendTextMessageAsync(
                       chatId: message.Chat.Id,
                       text: "Список досступных комаанд: " + commsands,
                       cancellationToken: cancellationToken);
                    

                }

                else if (text == "/donate")
                {
                    await botClient.SendTextMessageAsync(
                       chatId: message.Chat.Id,
                       text: "Ваши донаты помогают нам улучшать и развивать нашего Telegram-бота. " +
                       "Все средства пойдут на разработку новых функций и улучшение существующих. " +
                       "Вы готовы поддержать разработчика?",
                       replyMarkup: inlineKeyboard,
                       cancellationToken: cancellationToken);
                }

                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "You sent a text!("+text+")",
                        cancellationToken: cancellationToken);
                }
            }
            if (update.Message.Type == MessageType.Sticker)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "You sent a sticker!",
                    cancellationToken: cancellationToken
                );
            }
            if (update.Message.Type == MessageType.Voice)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "You sent a voise message!",
                    cancellationToken: cancellationToken
                );
            }
        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            await HandleCallbackQuery(update.CallbackQuery, cancellationToken);
        }
    }
    static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }


    static async Task HandleCallbackQuery(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        
        ApplicationContext db = new ApplicationContext();

        string responseText = callbackQuery.Data
            switch
        {
            "yes" => "(donate link)",
            "no" => "хорошо, если передумаете, возвращайтесь ;)",
            _ => "Unknown callback"
        };
        if(callbackQuery.Data == "yes")
        {
            db.MakingAPayment(callbackQuery.From.Id);
        }


        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: "Button pressed",
            cancellationToken: cancellationToken
        );


        await botClient.SendTextMessageAsync(
        chatId: callbackQuery.Message.Chat.Id,
        text: responseText,
        cancellationToken: cancellationToken);

    }

    static async Task DeleteMessageAfterDelay(ITelegramBotClient botClient, long chatId, int messageId, CancellationToken cancellationToken)
    {
        // Задержка в 10 секунд (можете изменить на нужное значение)
        await Task.Delay(5000);

        try
        {
            await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не удалось удалить сообщение: {ex.Message}");
        }
    }
}