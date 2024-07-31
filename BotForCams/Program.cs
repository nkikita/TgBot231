﻿using System;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using yoomoney_api.authorize;


using Microsoft.EntityFrameworkCore;
using System.Runtime.Intrinsics.X86;
using Npgsql;

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
            DO UPDATE SET ""stastus"" = true;";

        Database.ExecuteSqlRaw(createTable, id, name, stastus, datetame);
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
                int userId = Convert.ToInt32(message.From.Id);
                string tableName = $"{username}_{userId}";
                DateTime dateTime = DateTime.Now;
                DateTime utcNow = dateTime.ToUniversalTime();
                ApplicationContext db = new ApplicationContext();


                db.CreateDynamicTabl(tableName);
                db.AddUserMessage(tableName, text);

                if (text == "/pay")
                {
                    db.InfoTable(username, userId, false, utcNow);
                    await botClient.SendTextMessageAsync(
                       chatId: message.Chat.Id,
                       text: "wait",
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