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


public class ApplicationContext : DbContext
{
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

    public void AddUserMessage(string tableName, string messageText)
    {
        var addMessageQuery = $@"
            INSERT INTO ""{tableName}"" (""MessageText"", ""SentAt"")
            VALUES (@p0, @p1)";

        Database.ExecuteSqlRaw(addMessageQuery, messageText, DateTime.UtcNow);
    }
}

class Program   
{
    static ITelegramBotClient botClient;

    static void Main()
    {

        botClient = new TelegramBotClient("7242370794:AAFrI45C08puMjeYkthCHKVPSr1mWg0i4uE");

        ApplicationContext db = new ApplicationContext();
        db.SaveChangesAsync();

        var me = botClient.GetMeAsync().Result;
        Console.WriteLine("Запущен бот " + botClient.GetMeAsync().Result.FirstName);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { } 
        };
        botClient.StartReceiving(
            HandleUpdateAsync,      
            HandleErrorAsync,
            receiverOptions
        );
        Console.ReadKey();
        
        

    }
    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        string connnect = "Host=localhost;Port=5432;Database=testDB;Username=postgres;Password=nikitos";
        var message = update.Message;
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));


        if (update.Type == UpdateType.Message)
        {
            if (update.Message.Type == MessageType.Text)
            {

                string text = message.Text;
                string username = message.From.Username;
                long userId = message.From.Id;
                string tableName = $"{username}_{userId}";

                using (ApplicationContext db = new ApplicationContext())
                {
                    db.CreateDynamicTabl(tableName);
                    db.AddUserMessage(tableName, text);
                }


                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "You sent a text!("+text+")",
                    cancellationToken: cancellationToken);
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
}