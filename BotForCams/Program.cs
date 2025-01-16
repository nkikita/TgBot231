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
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

public class anecdots
{
    public int id { get; set;}
    public string anec { get; set;}
}

public class blacklist
{
    public int id {  get; set; }
    public int UID {  get; set; }
    public string UName { get; set; }
}

public class Info
{
    public string name { get; set; }
    public int id { get; set; }
    public bool stastus { get; set; }

    public DateTime datetame {  get; set; }
}

public class ApplicationContext : DbContext
{
    public DbSet<anecdots> anecdots { get; set; } = null!;
    public DbSet<Info> Info { get; set; } = null!;
    public DbSet<blacklist> blacklist { get; set; } = null!;

    public ApplicationContext()
    {
        Database.EnsureCreated();
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=testDB;Username=postgres;Password=nikitos");
    }

    public string GetAnecd(int coun)
    {
        var ane = anecdots.FirstOrDefault(anecdots => anecdots.id == coun);
        return ane.anec;
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

    public void InfoTable(string name, long id, bool stastus, DateTime datetame)
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

    public string GetNameById(int id)
    {
        var userInfo = Info.FirstOrDefault(info => info.id == id);
        return userInfo.name;
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
   

    public bool IsUserBlacklisted(long userId)
    {
        return blacklist.Any(b => b.UID == userId);
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
        Console.WriteLine("Запущен бот " + botClient.GetMeAsync().Result.FirstName + " команды бота: sendall, block, unblock, exit;");

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
            else if (txt == "block")
            {
                Console.WriteLine("Введите id пользователя, которого хотите заблокировать: ");
                int id = Convert.ToInt32(Console.ReadLine());
                BlockUser(id, db.GetNameById(id));
            }
            else if (txt == "unblock")
            {

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

        static async Task BlockUser(int UID, string UName)
        {
            ApplicationContext db = new ApplicationContext();
            try { }
            
            catch 
            { 
                Console.WriteLine($"Не удалось заблокировать пользователя");
            }
        }
    }

    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        //string connnect = "Host=localhost;Port=5432;Database=testDB;Username=postgres;Password=nikitos";
        var message = update.Message;
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
        Random rnd = new Random();
        int cou = rnd.Next(1, 17);

        List<string> Listcommsands= new List<string> { "/anecdot", "/help" , "/donate", "/bestofthebest"};

        if (update.Type == UpdateType.Message)
        {
            if (update.Message.Type == MessageType.Text)
            {
                string text = message.Text;
                string username = message.From.Username;
                long userId = message.From.Id;
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
                       text: "Привтствую тебя дорогой пользователь! Вот список доступных команд: " + commsands,
                       cancellationToken: cancellationToken);
                }
                else if (text == "/anecdot")
                {
                    await botClient.SendTextMessageAsync(
                       chatId: message.Chat.Id,
                       text: db.GetAnecd(cou) + "\nЕсли хотите поддержать разработчика то можете использовать команду /donate",
                       cancellationToken: cancellationToken);
                }

                else if(text == "/bestofthebest")
                {
                    await botClient.SendTextMessageAsync(
                      chatId: message.Chat.Id,
                      text: "Идёт Волк по лесу, видит — Заяц без ушей. Волк:\r\n— Ты это чего, где уши потерял?\r\n— Да вот армию закосил — уши обрезал — мне и сказали мол \"Негоден\".\r\n— Блин, так мне тоже повестка пришла!\r\n— Ну, серый, уши то у тебя маленькие, придётся хвост обрезать.\r\nОбрезали хвост волку, его тоже отпустили на свободу. Сидят вдвоём празднуют отмаз от армии. Идёт Медведь:\r\n— Чего это вы? Один без ушей, другой без хвоста?\r\n— Так мы от армии закосили!\r\n— Эх, блин, так мне тоже надо!\r\nПосмотрели звери на медведя и говорят:\r\n— Уши маленькие, хвост тоже, придётся яйца резать!\r\n— Да вы что, это же самое дорогое что у меня есть!!!!\r\n— Ну, тогда, Миша, шуруй в армию! Говорит волк\r\nДолго ломался медведь, по итогу решил:\r\n— Ладно, режьте!\r\nОтрезали Мишке его достоинство, и пошёл он на медкомиссию. \r\nТри дня его не было видно, и пошли заяц с волком искать медведя, проходят мимо военкомата и видят медведь на дереве повесился, а в руках у него бумажка: \r\nЗаяц взял её и читает: «Не годен. Косолапие».",
                      cancellationToken: cancellationToken);
                }

                else if (text == "/help")
                {
                    await botClient.SendTextMessageAsync(
                       chatId: message.Chat.Id,
                       text: "Список доступных команд: " + commsands +"."+"\nЕсли возникли какие-то вопросы по боту, то пишите сюда: @Nik8273172",
                       cancellationToken: cancellationToken) ;
                }

                else if (text == "/donate")
                {
                    await botClient.SendTextMessageAsync(
                       chatId: message.Chat.Id,
                       text: "Ваши донаты помогают нам улучшать и развивать нашего Telegram-бота. " +
                       "Все средства пойдут на разработку новых функций и улучшение существующих, на использование качественного оборудования (более быстрый сервер), регулярное обновление пула анекдотов и написание новых авторских от людей, которые занимаются этим профессионально. " +
                       "Вы готовы поддержать разработчика?",
                       replyMarkup: inlineKeyboard,
                       cancellationToken: cancellationToken);
                }

                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Вы отправили сообщение!("+text+")",
                        cancellationToken: cancellationToken);
                }
            }
            if (update.Message.Type == MessageType.Sticker)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Вы отправили стикер!",
                    cancellationToken: cancellationToken
                );
            }
            if (update.Message.Type == MessageType.Voice)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Вы отправили голосовое сообщение!",
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