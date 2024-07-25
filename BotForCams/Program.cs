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

public class User
{
    public int id { get; set; }
    public string username { get; set; }
    public string message { get; set; }
}

public class ApplicationContext : DbContext
{
    public DbSet<User> info { get; set; } = null!;
    public ApplicationContext()
    {
        Database.EnsureCreated();
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=testDB;Username=postgres;Password=nikitos");
    }
}

class Program
{
    static ITelegramBotClient botClient;

    static void Main()
    {
        botClient = new TelegramBotClient("7242370794:AAFrI45C08puMjeYkthCHKVPSr1mWg0i4uE");

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
        var message = update.Message;
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
        using var cmd = new NpgsqlCommand();


        if (update.Type == UpdateType.Message)
        {
            if (update.Message.Type == MessageType.Text)
            {
                int IDD = Convert.ToInt32(message.From.Id);
                string text = message.Text;
                string username = message.From.Username;
                using (ApplicationContext db = new ApplicationContext())
                {
                    cmd.CommandText = ("CREATE TABLE" + username);
                    User user = new User {id = IDD, message = text, username = username };
                    db.info.Add(user);
                    db.SaveChanges();
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