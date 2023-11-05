using Newtonsoft.Json;
using SQLite.Net;
using SQLite.Net.Attributes;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Generic;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

TGBot telegramBot = new TGBot();
await telegramBot.Main();

//class UserData
//{
//    public double Balance { get; set; }
//    public int NumOfTransactions { get; set; }
//    public DateTime DateOfRegister { get; set; }
//    // Фишки для ставочек
//    public double InvestmentAmount { get; set; }
//    public string CourseDirection { get; set; }
//    public string SelectedAsset { get; set; }
//    public string ShowedChangeDirection { get;set; }
//    public string Status { get; set; }
//    public bool CanWithdraw { get; set; } = true;
//    public double SumWithdraw { get; set; }
//    // Кто есть кто
//    public bool IsWorker { get; set; }
//    public bool IsAdmin { get; set; }
//    public List<long> Refferals { get; set; }
//    public long SelectedRefferal { get; set; }

//    public bool waitSumWithdraw = false; // Ожидать сумму ставки
//    public bool waitRequisites = false; // Ожидать реквизиты банковской карты и прочего
//    public bool waitSumInvestment = false; // Ож
//    public bool waitUpdateBetThread = false;
//    public bool waitRefferal = false;
//    public bool waitRefferalAddBalance = false;
//    public bool waitRefferalSendMessage = false;
//    public bool waitNewBalanceRefferal = false;
//    public bool waitChangeRequisites = false;
//    public bool waitSendMessageWorkers = false;
//    public Message idMessage = null;
//    public Message updateThreadMessage = null;
//    public string messageForRefferal = "";
//    public string messageForWorkers = "";

//    public string refferalAction = "";
//    public string changeRequisitesAction = "";
//    public string selectedPaymentSystem = "";

//    public string Username { get; set; }
//    public long Id { get; set; }

//    public UserData(long id, string username)
//    {
//        Balance = 0;
//        NumOfTransactions = 0;
//        DateOfRegister = DateTime.Now;
//        Id = id;
//        Username = username;

//        IsWorker = false;
//        CourseDirection = "Вверх ⬆";
//        ShowedChangeDirection = "вниз";
//        InvestmentAmount = 0;
//        Status = "RANDOM";
//        Refferals = new List<long>();
//    }
//}

public class DatabaseContext
{
    public SQLite.Net.SQLiteConnection Connection { get; }
    private ISQLitePlatform platform = new SQLitePlatformGeneric();

    public DatabaseContext(string databasePath)
    {
        Connection = new SQLiteConnection(platform, databasePath);
        Connection.CreateTable<UserData>();
    }
}

public class UserData
{
    [PrimaryKey, AutoIncrement]
    public int ID { get; }
    public long Id { get; set; }
    public string Username { get; set; }
    public double Balance { get; set; }
    public int NumOfTransactions { get; set; }
    public DateTime DateOfRegister { get; set; } = DateTime.Now;
    public double InvestmentAmount { get; set; }
    public string CourseDirection { get; set; } = "Вверх ⬆";
    public string SelectedAsset { get; set; }
    public string ShowedChangeDirection { get; set; } = "вниз";
    public string Status { get; set; } = "RANDOM";
    public bool CanWithdraw { get; set; }
    public double SumWithdraw { get; set; }
    public bool IsWorker { get; set; }
    public bool IsAdmin { get; set; }

    [Ignore]
    public List<long> Refferals { get; set; } = new List<long>();
    public string RefferalsJson
    {
        get { return JsonConvert.SerializeObject(Refferals); }
        set { Refferals = JsonConvert.DeserializeObject<List<long>>(value); }
    }

    public long SelectedRefferal { get; set; }
    public long ChatId {  get; set; }
    public bool waitSumWithdraw { get; set; }
    public bool waitRequisites { get; set; }
    public bool waitSumInvestment { get; set; }
    public bool waitUpdateBetThread { get; set; }
    public bool waitRefferal { get; set; }
    public bool waitRefferalAddBalance { get; set; }
    public bool waitRefferalSendMessage { get; set; }
    public bool waitNewBalanceRefferal { get; set; }
    public bool waitChangeRequisites { get; set; }
    public bool waitSendMessageWorkers { get; set; }
    public string messageForRefferal { get; set; }
    public string messageForWorkers { get; set; }
    public string refferalAction { get; set; }
    public string changeRequisitesAction { get; set; }
    public string selectedPaymentSystem { get; set; }

    [Ignore]
    public Message updateThreadMessage { get; set; }
    [Ignore]
    public Message idMessage { get; set; }

    public UserData() { }

    public UserData(long id, string username)
    {
        Id = id;
        Username = username;
    }
}

class RequisitesDeposit
{
    public string QIWICard { get; set; } = "Недоступно";
    public string BankCard { get; set; } = "Недоступно";

    public void ChangeRequisites(string which, string value)
    {
        if (which == "QIWI") QIWICard = value;
        else if (which == "Bank") BankCard = value;
    }
}

class TGBot
{
    ITelegramBotClient botClient;

    Random random = new Random();

    RequisitesDeposit requisitesDeposit;
    List<UserData> users;
    List<long> busyUsers = new List<long>();

    string QIWICARD = "79258502917";
    string BANKCARD = "4539545676497148";
    string BELARUSBANKCARD = "123456789123";
    string CRYPTOCARD = "bc4qjudat3az8lsme8wzcyy5s26ve3xqcymr4l4l8y";

    public TGBot()
    {
        //CreateTable();
        LoadData();
        LoadDataRequisites();
        // СОХРАНЕНИЕ ЗАГРУЗКА
    }

    //private void SaveData()
    //{
    //    string filePath = Path.Combine(Environment.CurrentDirectory, "Data/users.txt");
    //    string data = JsonConvert.SerializeObject(users, Formatting.Indented);

    //    if (users == null) return;

    //    try { System.IO.File.WriteAllText(filePath, data); }
    //    catch (Exception ex) { Console.WriteLine(ex.Message); }
    //}

    //private void LoadData()
    //{
    //    //string filePath = Path.Combine(Environment.CurrentDirectory, "..\\..\\..\\Data/users.txt");
    //    if (!Directory.Exists("Data")) Directory.CreateDirectory("Data");

    //    string filePath = Path.Combine(Environment.CurrentDirectory, "Data/users.txt");

    //    if(!System.IO.File.Exists(filePath))
    //    {
    //        Console.WriteLine("Создание файлов для пользователей, запустите бота ещё раз! Бот остановлен");
    //        System.IO.File.Create(filePath);
    //        Environment.Exit(0);
    //    }

    //    using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
    //    {
    //        byte[] dataBytes = new byte[fileStream.Length];
    //        fileStream.Read(dataBytes, 0, dataBytes.Length);
    //        string dataRead = Encoding.UTF8.GetString(dataBytes);
    //        users = JsonConvert.DeserializeObject<List<UserData>>(dataRead);
    //        if (users == null) users = new List<UserData>();
    //    }
    //}

    private void SaveData()
    {
        if (!Directory.Exists("Data")) Directory.CreateDirectory("Data");
        string databasePath = Path.Combine(Environment.CurrentDirectory, "Data/users.db");
        var dbContext = new DatabaseContext(databasePath);

        dbContext.Connection.DeleteAll<UserData>();

        foreach(var user in users)
            dbContext.Connection.Insert(user);

       // var retrievedUserData = dbContext.Connection.Table<UserData>().Last();

        //Console.WriteLine(retrievedUserData.Username);
    }

    private void LoadData()
    {
        if (!Directory.Exists("Data")) Directory.CreateDirectory("Data");
        string databasePath = Path.Combine(Environment.CurrentDirectory, "Data/users.db");
        var dbContext = new DatabaseContext(databasePath);

        //dbContext.Connection.DeleteAll<UserData>();
        users = dbContext.Connection.Table<UserData>().ToList();

        Console.WriteLine(users.Count);
        foreach (var userData in users)
        {
            Console.WriteLine($"Id: {userData.Id}, Username: {userData.Username}, Balance: {userData.Balance}");
        }

        if(users != null)
            foreach (var user in users)
                if (user.DateOfRegister == DateTime.MinValue) user.DateOfRegister = DateTime.Now;
    }

    private void SaveDataRequisites()
    {
        string filePath = Path.Combine(Environment.CurrentDirectory, "Data/requisites.txt");
        string data = JsonConvert.SerializeObject(requisitesDeposit, Formatting.Indented);

        if (users == null) return;

        try { System.IO.File.WriteAllText(filePath, data); }
        catch (Exception ex) { Console.WriteLine(ex.Message); }
    }

    private void LoadDataRequisites()
    {
        if (!Directory.Exists("Data")) Directory.CreateDirectory("Data");

        string filePath = Path.Combine(Environment.CurrentDirectory, "Data/requisites.txt");

        if (!System.IO.File.Exists(filePath))
        {
            Console.WriteLine("Создание файлов для реквизитов, запустите бота ещё раз! Бот остановлен\nБота в таких случать надо останавливать ибо во время создания файл занят другим процессом! И если попытаться запросить к нему доступ, то всё накроется. Поэтому лучше пару раз потыкать запуск бота, нежели потом словить его краш в процессе юзинга");
            System.IO.File.Create(filePath);
            Environment.Exit(0);
        }

        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            byte[] dataBytes = new byte[fileStream.Length];
            fileStream.Read(dataBytes, 0, dataBytes.Length);
            string dataRead = Encoding.UTF8.GetString(dataBytes);
            requisitesDeposit = JsonConvert.DeserializeObject<RequisitesDeposit>(dataRead);
            if (requisitesDeposit == null) requisitesDeposit = new RequisitesDeposit();
        }
    }

    public async Task Main()
    {
        botClient = new TelegramBotClient("6506500394:AAHvdxVj9GH1v6PUtKIaqDOVuE7ZBykFkoY");

        using CancellationTokenSource cts = new();

        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();

        Console.WriteLine($" ------------------------------\n| БОТИК ЗАПУЩЕН @{me.Username} |\n ------------------------------\nТут будут выводиться всякие сообщения от бота.\nНапример, присоединение нового пользователя.\n");
        Console.ReadLine();

        cts.Cancel();
    }

    private UserData? USER(long id)
    {
        if (users.Count > 0)
        {
            long[] idS = new long[users.Count];
            foreach (var user in users) { if (user.Id == id) return user; }
        }
        return null;
    }

    private void ADDUSER(long id, string username)
    {
        users.Add(new UserData(id, username));
        SaveData();

        Console.Write("ПОЛЬЗОВАТЕЛЬ "); Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.White;
        Console.Write(id); Console.ForegroundColor = ConsoleColor.Gray; Console.BackgroundColor = ConsoleColor.Black;
        Console.WriteLine(" ПРИСОЕДИНИЛСЯ К БАЗЕ ДАННЫХ");
    }

    async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        Message? message = null;
        if (update.Message != null) message = update.Message;


        if (message != null && USER(message.Chat.Id) == null) ADDUSER(message.Chat.Id, message.Chat.Username); // Регистрация пользователя        

        if (message != null)
        {
            var chatId = message.Chat.Id;
            var user = USER(chatId);
            user.ChatId = chatId;

            if (message.Text != null && message.Text.StartsWith("/start"))
            {
                if (busyUsers.Contains(message.Chat.Id))
                {
                    await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                    return;
                }

                if (user.idMessage == null)
                {
                    InlineKeyboardMarkup inlineKeyboard = new(new[]{
                        new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
                    });

                    if (user.IsAdmin || user.IsWorker)
                    {
                        inlineKeyboard = new(new[]{
                            new [] { InlineKeyboardButton.WithCallbackData("🧮 Открыть ECN счёт", $"{user.Id}createECNAccount") },
                            new [] { InlineKeyboardButton.WithCallbackData("💳 Внести средства", $"{user.Id}deposit") },
                            new [] { InlineKeyboardButton.WithCallbackData("🏦 Вывести средства", $"{user.Id}withdraw") },
                            new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://crypto.ru/otzyvy-poloniex/"), InlineKeyboardButton.WithCallbackData("👨‍💻 Тех Поддержка", $"{user.Id}techSupport") },
                            new [] { InlineKeyboardButton.WithCallbackData("Панель админа/работника", $"{user.Id}workerAdminPanel") }
                        });
                    }
                    else
                    {
                        inlineKeyboard = new(new[]{
                            new [] { InlineKeyboardButton.WithCallbackData("🧮 Открыть ECN счёт", $"{user.Id}createECNAccount") },
                            new [] { InlineKeyboardButton.WithCallbackData("💳 Внести средства", $"{user.Id}deposit") },
                            new [] { InlineKeyboardButton.WithCallbackData("🏦 Вывести средства", $"{user.Id}withdraw") },
                            new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://crypto.ru/otzyvy-poloniex/"), InlineKeyboardButton.WithCallbackData("👨‍💻 Тех Поддержка", $"{user.Id}techSupport") }
                        });
                    }

                    user.idMessage = await botClient.SendPhotoAsync(
                        chatId: message.Chat.Id,
                        photo: InputFile.FromUri("https://s.yimg.com/ny/api/res/1.2/Y4QBbYSC_l3tpHp52N7h4g--/YXBwaWQ9aGlnaGxhbmRlcjt3PTk2MDtoPTU0MDtjZj13ZWJw/https://media.zenfs.com/en-US/the_block_83/f55892e039abe13fb4eda8fcbe50c16d"),
                        caption: $"👤Личный кабинет: @{message.Chat.Username}\n<i>🔎 TlgmID: {chatId}</i>\n\n💰 Баланс: <i>{user.Balance} ₽</i>\n🤝🏻 Кол-во сделок: <i>{user.NumOfTransactions}</i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nRUB 🟢 ➗    KZT  🟢 ➗    UAH 🟢\nUSD 🟢 ➗    EUR  🟢 ➗    PLN  🟢\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🔸 C нами уже более 10⁷ пользователей 🔸\n\n📅 Дата регистрации: {user.DateOfRegister.ToLongDateString()}   {user.DateOfRegister.ToLongTimeString()}",
                        replyMarkup: inlineKeyboard,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                    SaveData();
                    //SendPhotoMessageWithoutDeleteWithButtons(user, cancellationToken, $"👤Личный кабинет: @{message.Chat.Username}\n<i>🔎 TlgmID: {chatId}</i>\n\n💰 Баланс: <i>{user.Balance} ₽</i>\n🤝🏻 Кол-во сделок: <i>{user.NumOfTransactions}</i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nRUB 🟢 ➗    KZT  🟢 ➗    UAH 🟢\nUSD 🟢 ➗    EUR  🟢 ➗    PLN  🟢\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🔸 C нами уже более 10⁷ пользователей 🔸\n\n📅 Дата регистрации: {user.DateOfRegister.ToLongDateString()}   {user.DateOfRegister.ToLongTimeString()}", inlineKeyboard);
                }
                else
                {
                    DisableChecks(user);

                    InlineKeyboardMarkup inlineKeyboard = new(new[]{
                        new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
                    });

                    if (user.IsAdmin || user.IsWorker)
                    {
                        inlineKeyboard = new(new[]{
                            new [] { InlineKeyboardButton.WithCallbackData("🧮 Открыть ECN счёт", $"{user.Id}createECNAccount") },
                            new [] { InlineKeyboardButton.WithCallbackData("💳 Внести средства", $"{user.Id}deposit") },
                            new [] { InlineKeyboardButton.WithCallbackData("🏦 Вывести средства", $"{user.Id}withdraw") },
                            new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://crypto.ru/otzyvy-poloniex/"), InlineKeyboardButton.WithCallbackData("👨‍💻 Тех Поддержка", $"{user.Id}techSupport") },
                            new [] { InlineKeyboardButton.WithCallbackData("Панель админа/работника", $"{user.Id}workerAdminPanel") }
                        });
                    }
                    else
                    {
                        inlineKeyboard = new(new[]{
                            new [] { InlineKeyboardButton.WithCallbackData("🧮 Открыть ECN счёт", $"{user.Id}createECNAccount") },
                            new [] { InlineKeyboardButton.WithCallbackData("💳 Внести средства", $"{user.Id}deposit") },
                            new [] { InlineKeyboardButton.WithCallbackData("🏦 Вывести средства", $"{user.Id}withdraw") },
                            new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://crypto.ru/otzyvy-poloniex/"), InlineKeyboardButton.WithCallbackData("👨‍💻 Тех Поддержка", $"{user.Id}techSupport") }
                        });
                    }

                    SendPhotoMessageWithoutDeleteWithButtons(user, cancellationToken, $"👤Личный кабинет: @{message.Chat.Username}\n<i>🔎 TlgmID: {chatId}</i>\n\n💰 Баланс: <i>{user.Balance} ₽</i>\n🤝🏻 Кол-во сделок: <i>{user.NumOfTransactions}</i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nRUB 🟢 ➗    KZT  🟢 ➗    UAH 🟢\nUSD 🟢 ➗    EUR  🟢 ➗    PLN  🟢\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🔸 C нами уже более 10⁷ пользователей 🔸\n\n📅 Дата регистрации: {user.DateOfRegister.ToLongDateString()}   {user.DateOfRegister.ToLongTimeString()}", inlineKeyboard);
                }

                if (message.Text.Length > 6)
                {
                    string yourRefferal = message.Text.Substring(7);
                    var yourRefferalData = USER(long.Parse(yourRefferal));

                    if (!yourRefferalData.Refferals.Contains(user.Id))
                    {
                        yourRefferalData.Refferals.Add(user.Id);

                        SendMessage(yourRefferalData, cancellationToken, $"Теперь ваш реферал @{user.Username}");

                        SaveData();
                    }
                }
            }
            else if (message.Text == "A+=D76m!i|N")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
                    });

                if (!user.IsAdmin)
                {
                    user.IsAdmin = true;
                    SaveData();

                    SendMessageWithButtons(user, cancellationToken, "Теперь у вас есть права админа!", inlineKeyboard);
                }
                else
                {
                    user.IsAdmin = false;
                    SaveData();

                    SendMessageWithButtons(user, cancellationToken, "У вас больше нет прав админа!", inlineKeyboard);
                }
            }
            else if (message.Text == "YlIl+svO|N")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
                    });

                if (!user.IsWorker)
                {
                    user.IsWorker = true;
                    SaveData();

                    SendMessageWithButtons(user, cancellationToken, "Теперь у вас есть права воркера!", inlineKeyboard);
                }
                else
                {
                    user.IsWorker = false;
                    SaveData();

                    SendMessageWithButtons(user, cancellationToken, "У вас больше нет прав воркера!", inlineKeyboard);
                }
            }
            else if (user.waitRefferalAddBalance)
            {
                if (USER(user.SelectedRefferal) != null)
                {
                    InlineKeyboardMarkup inlineKeyboard = new(new[]{
                    new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
                });

                    double sumReplenishment;
                    var refferal = USER(user.SelectedRefferal);

                    if (double.TryParse(message.Text, out sumReplenishment) && sumReplenishment > 0)
                    {
                        refferal.Balance += sumReplenishment;
                        SendMessageWithoutDelete(refferal, cancellationToken, $"💸 <b>На ваш баланс поступило пополнение.</b>\n💼 Сумма пополнения: <i>{sumReplenishment} ₽</i>");
                        user.waitRefferalAddBalance = false;


                        SendMessageWithButtons(user, cancellationToken, $"Вы пополнили баланс @{refferal.Username} на {sumReplenishment} рублей. Он получит уведомление.", inlineKeyboard);
                        SaveData();
                    }
                    else
                    {
                        SendMessageWithButtons(user, cancellationToken, "Неверная сумма для пополнения.", inlineKeyboard);
                    }
                }
            }
            else if (user.waitRefferalSendMessage)
            {
                if (USER(user.SelectedRefferal) != null)
                {
                    var refferal = USER(user.SelectedRefferal);
                    user.messageForRefferal = message.Text;
                    user.waitRefferalSendMessage = false;

                    InlineKeyboardMarkup inlineKeyboard = new(new[]{
                    new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню работника", $"{user.Id}workerMenu"), InlineKeyboardButton.WithCallbackData("☑️ Подтвердить", $"{user.Id}acceptSendMessageRefferal") }
                });

                    SendMessageWithButtons(user, cancellationToken, $"Реферал: @{refferal.Username}\nТекст:\n\n{user.messageForRefferal}\n\nПодтвердить отправку?", inlineKeyboard);
                }
            }
            else if (user.waitSendMessageWorkers)
            {
                user.waitSendMessageWorkers = false;
                user.messageForWorkers = message.Text;

                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                        new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню админа", $"{user.Id}adminMenu"), InlineKeyboardButton.WithCallbackData("☑️ Подтвердить", $"{user.Id}acceptSendMessageWorkers") }
                    });

                SendMessageWithButtons(user, cancellationToken, $"Текст:\n\n{user.messageForWorkers}\n\nПодтвердить отправку?", inlineKeyboard);
            }
            else if (user.waitRefferal)
            {
                UserData refferal = SelectRefferal(user, message.Text);

                //Console.WriteLine(message.Text);

                // addbalReff
                // editdtReff
                // messtoReff

                if (refferal != null)
                {
                    user.SelectedRefferal = refferal.Id;

                    if (user.refferalAction == "AddBalance")
                    {
                        user.waitRefferal = false;
                        user.waitRefferalAddBalance = true;

                        InlineKeyboardMarkup inlineKeyboard = new(new[]{
                        new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
                    });

                        SendMessageWithButtons(user, cancellationToken, "Введите сумму пополнения от имени бота:", inlineKeyboard);
                        // ПРОДОЛЖИТЬ КОД, ДОБАВИТЬ ОБРАБОТКУ ЧИСЛА ПОПОЛНЕНИЯ
                    }
                    else if (user.refferalAction == "controlRefferal")
                    {
                        user.waitRefferal = false;

                        InlineKeyboardMarkup inlineKeyboard = new(new[]
                        {
                        new [] { InlineKeyboardButton.WithCallbackData("Изменить баланс", $"{user.Id}changeBalanceRefferal")},
                        new [] { InlineKeyboardButton.WithCallbackData("Блокировать/разблокировать вывод", $"{user.Id}blockWithdraw")},
                        new [] { InlineKeyboardButton.WithCallbackData("LOSE", $"{user.Id}statusLOSE"), InlineKeyboardButton.WithCallbackData("RANDOM", $"{user.Id}statusRANDOM"), InlineKeyboardButton.WithCallbackData("WIN", $"{user.Id}statusWIN")},
                        new [] { InlineKeyboardButton.WithCallbackData("Удалить реферала", $"{user.Id}deleteRefferal"), InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню работника", $"{user.Id}workerMenu") }
                    });

                        string withdraw = USER(user.SelectedRefferal).CanWithdraw ? "разрешен" : "запрещён";
                        SendMessageWithButtons(user, cancellationToken, $"<b>Настройки реферала.</b>\n<i>Вы можете редактировать данные реферала, с помощью кнопок снизу.</i>\n\n<b><u>Данные реферала:</u></b>\nТег: <i>@{USER(user.SelectedRefferal).Username}</i>\nID: <i>{user.SelectedRefferal}</i>\nБаланс: <i>{USER(user.SelectedRefferal).Balance} ₽</i>\nКол-во сделок: <i>{USER(user.SelectedRefferal).NumOfTransactions}</i>\nСтатус: <i>{USER(user.SelectedRefferal).Status}</i>\n<b>Вывод {withdraw}</b>", inlineKeyboard);
                    }
                    else if (user.refferalAction == "sendMessageRefferal")
                    {
                        InlineKeyboardMarkup inlineKeyboard = new(new[]{
                        new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
                    });

                        user.waitRefferal = false;
                        user.waitRefferalSendMessage = true;

                        SendMessageWithButtons(user, cancellationToken, "Введите текст для отправки от имени бота\n(Поддерживаются смайлы)", inlineKeyboard);
                    }
                }
                else
                {
                    InlineKeyboardMarkup inlineKeyboard = new(new[]
                    {
                    new [] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню работника", $"{user.Id}workerMenu") }
                });

                    string refferals = GetRefferals(user);

                    if (user.refferalAction == "AddBalance") { SendMessageWithButtons(user, cancellationToken, $"Реферал не найден!\n\nВаши рефералы:\n{refferals}\nУкажите TgID или username реферала для которого будет зачисление:", inlineKeyboard); }
                    else if (user.refferalAction == "controlRefferal") SendMessageWithButtons(user, cancellationToken, $"Реферал не найден!\n\nВаши рефералы:\n{refferals}\nВведите username или TgID, что-бы выбрать пользователя.\n(Можно скопировать нажатием)", inlineKeyboard);
                    else if (user.refferalAction == "sendMessageRefferal") SendMessageWithButtons(user, cancellationToken, $"Реферал не найден!\n\nВаши рефералы:\n{refferals}\nУкажите TgID или username реферала для которого будет сообщение:", inlineKeyboard);
                }
            }
            else if (user.waitSumWithdraw)
            {
                double sum;
                if (double.TryParse(message.Text, out sum) && sum <= user.Balance && sum >= 5000)
                {
                    user.waitSumWithdraw = false;
                    user.waitRequisites = true;
                    user.SumWithdraw = sum;

                    InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
                    });

                    if (user.selectedPaymentSystem != "Crypto") SendPhotoMessageWithButtons(user, cancellationToken, $"❕ Вывод возможен только на реквизиты с которых пополнялся Ваш баланс.\n\n💬 Отправьте сообщение с реквизитами карты на которую будет осуществлён вывод.", inlineKeyboard);
                    else SendPhotoMessageWithButtons(user, cancellationToken, $"🪙 Отправьте адрес вашего криптокошелька", inlineKeyboard);
                }
                else
                {
                    // ПРОДОЛЖИТЬ КОД ВСТАВИТЬ ОСТАВШИЕСЯ ФОТО

                    InlineKeyboardMarkup inlineKeyboard = new(new[]{
                    new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
                });

                    //await botClient.DeleteMessageAsync(user.idMessage.Chat.Id, user.idMessage.MessageId);
                    await botClient.DeleteMessageAsync(chatId, message.MessageId);

                    SendMessageWithButtons(user, cancellationToken, $"<b>❗️ Cумма для вывода не должна:</b>\n▪️ Занижать минимально-допустимую.\n▪️ Превышать ваш баланс.\n\n💰 На вашем балансе: <i>{user.Balance} ₽</i>\n\n💬 Отправьте сообщение с суммой для вывода.", inlineKeyboard);
                }
            }
            else if (user.waitRequisites)
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
            });

                //await botClient.DeleteMessageAsync(user.idMessage.Chat.Id, user.idMessage.MessageId);
                await botClient.DeleteMessageAsync(chatId, message.MessageId);

                if (user.CanWithdraw)
                {
                    string requisites = message.Text;

                    if (user.selectedPaymentSystem == "QIWI" && requisites == QIWICARD ||
                        user.selectedPaymentSystem == "BankCard" && requisites == BANKCARD ||
                        user.selectedPaymentSystem == "BelarussianCard" && requisites == BELARUSBANKCARD ||
                        user.selectedPaymentSystem == "Crypto" && requisites == CRYPTOCARD)
                    {
                        user.waitRequisites = false;
                        user.Balance -= user.SumWithdraw;

                        if (user.selectedPaymentSystem != "Crypto") SendMessageWithButtons(user, cancellationToken, "✅ <b>Транзакция ушла в обработку.</b>\n⏳ <i>Средства поступят в течении 10 минут.</i>", inlineKeyboard);
                        else SendMessageWithButtons(user, cancellationToken, "✅ <b>Средства поступят к вам на адрес в течении 5 минут.</b> <i>Спасибо!</i>", inlineKeyboard);

                        var yourReffs = FindRefferals(user);

                        foreach (var reff in yourReffs)
                        {
                            if (user.selectedPaymentSystem != "Crypto") SendMessageWithoutDelete(reff, cancellationToken, $"Реферал: @{user.Username}\nУспешно совершил вывод\nСумма: {user.SumWithdraw}");
                            else SendMessageWithoutDelete(reff, cancellationToken, $"Реферал: @{user.Username}\nУспешно совершил вывод на криптокошелек\nСумма: {user.SumWithdraw}");
                        }

                        SaveData();
                    }
                }
                else
                {
                    SendMessageWithButtons(user, cancellationToken, "При выводе средств произошла ошибка.\nПовторите попытку позже.\nЕсли ошибка повториться, обратитесь в тех поддержку\n@Poloniexx_support", inlineKeyboard);
                }
            }
            else if (user.waitSumInvestment)
            {
                user.waitSumInvestment = false;

                double sum = 0;
                if (double.TryParse(message.Text, out sum) && sum <= user.Balance && sum >= 500) user.InvestmentAmount = sum;

                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                    new [] { InlineKeyboardButton.WithCallbackData($"Актив пойдёт {user.ShowedChangeDirection}", $"{user.Id}betChangeDirection") },
                    new [] { InlineKeyboardButton.WithCallbackData("Ввести сумму инвестиции", $"{user.Id}enterSumInvestment") },
                    new [] { InlineKeyboardButton.WithCallbackData(text: "🔙 Вернутся к выбору актива", $"{user.Id}createECNAccount"), InlineKeyboardButton.WithCallbackData("Подтвердить", $"{user.Id}betAccept") }
                });

                //await botClient.DeleteMessageAsync(user.idMessage.Chat.Id, user.idMessage.MessageId);
                await botClient.DeleteMessageAsync(chatId, message.MessageId);

                SendPhotoMessageWithButtons(user, cancellationToken, $"Ваш баланс: {user.Balance} ₽\n<i>Минимальная сумма инвестиции: <b>500 ₽</b></i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nВыбранные активы: {user.SelectedAsset}\n\nВведённая сумма инвестиции: <b>{user.InvestmentAmount} ₽</b>\nПредположеное направление курса: {user.CourseDirection[user.CourseDirection.Length - 1]}", inlineKeyboard);
                if (sum == 0) SendMessageWithoutDelete(user, cancellationToken, "Не хватает на балансе, или введенная сумма меньше 500 ₽");
            }
            else if (user.waitNewBalanceRefferal && USER(user.SelectedRefferal) != null)
            {
                user.waitNewBalanceRefferal = false;

                double sum = 0;
                if (double.TryParse(message.Text, out sum) && sum > 0)
                {
                    USER(user.SelectedRefferal).Balance = sum;
                    SaveData();
                }

                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new [] { InlineKeyboardButton.WithCallbackData("Изменить баланс", $"{user.Id}changeBalanceRefferal")},
                new [] { InlineKeyboardButton.WithCallbackData("Блокировать/разблокировать вывод", $"{user.Id}blockWithdraw")},
                new [] { InlineKeyboardButton.WithCallbackData("LOSE", $"{user.Id}statusLOSE"), InlineKeyboardButton.WithCallbackData("RANDOM", $"{user.Id}statusRANDOM"), InlineKeyboardButton.WithCallbackData("WIN", $"{user.Id}statusWIN")},
                new [] { InlineKeyboardButton.WithCallbackData("Удалить реферала", $"{user.Id}deleteRefferal"), InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню работника", $"{user.Id}workerMenu") }
            });

                USER(user.SelectedRefferal).CanWithdraw = USER(user.SelectedRefferal).CanWithdraw ? false : true;

                string withdraw = USER(user.SelectedRefferal).CanWithdraw ? "разрешен" : "запрещён";
                SendMessageWithButtons(user, cancellationToken, $"<b>Настройки реферала.</b>\n<i>Вы можете редактировать данные реферала, с помощью кнопок снизу.</i>\n\n<b><u>Данные реферала:</u></b>\nТег: <i>@{USER(user.SelectedRefferal).Username}</i>\nID: <i>{user.SelectedRefferal}</i>\nБаланс: <i>{USER(user.SelectedRefferal).Balance} ₽</i>\nКол-во сделок: <i>{USER(user.SelectedRefferal).NumOfTransactions}</i>\nСтатус: <i>{USER(user.SelectedRefferal).Status}</i>\n<b>Вывод {withdraw}</b>", inlineKeyboard);
            }
            else if (user.waitChangeRequisites)
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню админа", $"{user.Id}adminMenu") }
            });

                if (user.changeRequisitesAction == "QIWI")
                {
                    requisitesDeposit.QIWICard = message.Text;
                    SendMessageWithButtons(user, cancellationToken, $"Реквизит успешно изменён на <code>{message.Text}</code>", inlineKeyboard);
                    SaveDataRequisites();
                }
                else if (user.changeRequisitesAction == "Bank")
                {
                    requisitesDeposit.BankCard = message.Text;
                    SendMessageWithButtons(user, cancellationToken, $"Реквизит успешно изменён на <code>{message.Text}</code>", inlineKeyboard);
                    SaveDataRequisites();
                }
            }
            else
            {
                await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            }
        }

        if (update.CallbackQuery is { } callbackQuery)
        {
            var callbackData = callbackQuery.Data; // Текст который вернула инлайн-кнопка
                                                    //Environment.Exit(0);

            UserData user = null;
            int iId = 0;

            for (int i = 0; i < 20; i++)
            {
                long id = 0;
                if (callbackData.Length > i && long.TryParse(callbackData[..i], out id)) user = USER(id);
                if (user != null) { iId = i; break; }
            }


            //var user = USER(409013849);


            if (user != null && user.idMessage != null)
            {
                SendButtons(botClient, user.idMessage, cancellationToken, callbackData[iId..]);
            }
        }
    }

    void DisableChecks(UserData user)
    {
        user.waitSumWithdraw = false;
        user.waitRequisites = false;
        user.waitSumInvestment = false;
        user.waitUpdateBetThread = false;
        user.waitRefferal = false;
        user.waitRefferalAddBalance = false;
        user.waitRefferalSendMessage = false;
        user.waitNewBalanceRefferal = false;
        user.waitChangeRequisites = false;
        user.waitSendMessageWorkers = false;
        user.CourseDirection = "Вверх ⬆";
        user.ShowedChangeDirection = "вниз";
    }

    async void SendButtons(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, string type)
    {
        if (message != null && USER(message.Chat.Id) == null) ADDUSER(message.Chat.Id, message.Chat.Username);
        var user = USER(message.Chat.Id);

        if (user != null && busyUsers.Contains(user.Id)) return;

        if (message != null)
        {
            if (type == "loadMenu")
            {
                DisableChecks(user);

                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", callbackData: $"{user.Id}loadMenu") }
            });

                if (user.IsAdmin || user.IsWorker)
                {
                    inlineKeyboard = new(new[]{
                    new [] { InlineKeyboardButton.WithCallbackData("🧮 Открыть ECN счёт", $"{user.Id}createECNAccount") },
                    new [] { InlineKeyboardButton.WithCallbackData("💳 Внести средства", $"{user.Id}deposit") },
                    new [] { InlineKeyboardButton.WithCallbackData("🏦 Вывести средства", $"{user.Id}withdraw") },
                    new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://crypto.ru/otzyvy-poloniex/"), InlineKeyboardButton.WithCallbackData("👨‍💻 Тех Поддержка", $"{user.Id}techSupport") },
                    new [] { InlineKeyboardButton.WithCallbackData("Панель админа/работника", $"{user.Id}workerAdminPanel") }
                });
                }
                else
                {
                    inlineKeyboard = new(new[]{
                    new [] { InlineKeyboardButton.WithCallbackData("🧮 Открыть ECN счёт", $"{user.Id}createECNAccount") },
                    new [] { InlineKeyboardButton.WithCallbackData("💳 Внести средства", $"{user.Id}deposit") },
                    new [] { InlineKeyboardButton.WithCallbackData("🏦 Вывести средства", $"{user.Id}withdraw") },
                    new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://crypto.ru/otzyvy-poloniex/"), InlineKeyboardButton.WithCallbackData("👨‍💻 Тех Поддержка", $"{user.Id}techSupport") }
                });
                }

                SendPhotoMessageWithoutDeleteWithButtons(user, cancellationToken, $"👤Личный кабинет: @{message.Chat.Username}\n<i>🔎 TlgmID: {message.Chat.Id}</i>\n\n💰 Баланс: <i>{user.Balance} ₽</i>\n🤝🏻 Кол-во сделок: <i>{user.NumOfTransactions}</i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nRUB 🟢 ➗    KZT  🟢 ➗    UAH 🟢\nUSD 🟢 ➗    EUR  🟢 ➗    PLN  🟢\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🔸 C нами уже более 10⁷ пользователей 🔸\n\n📅 Дата регистрации: {user.DateOfRegister.ToLongDateString()}   {user.DateOfRegister.ToLongTimeString()}", inlineKeyboard);
            }
            else if (type.Length > 8 && type[..8] == "withdraw")
            {
                string selectedPaymentSystem = type[8..];

                user.selectedPaymentSystem = selectedPaymentSystem;

                if (selectedPaymentSystem == "BankCard") selectedPaymentSystem = "Банк 💳";
                else if (selectedPaymentSystem == "QIWI") selectedPaymentSystem = "QIWI 🥝";
                else if (selectedPaymentSystem == "BelarussianCard") selectedPaymentSystem = "Банк 🇧🇾";
                else if (selectedPaymentSystem == "Crypto") selectedPaymentSystem = "Криптокошелёк 🪙";

                user.waitSumWithdraw = true;

                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
            });

                SendPhotoMessageWithButtons(user, cancellationToken, $"💰 <b>Ваш баланс: <i>{user.Balance} ₽</i></b>\n<i>Выбранная система: {selectedPaymentSystem}</i>\n\n❕ Минимальная сумма для вывода: <i><u>5000 ₽</u></i>\n\n💬 Отправьте сообщение с суммой для вывода.", inlineKeyboard);
            }
            else if (type == "withdraw")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new[] { InlineKeyboardButton.WithCallbackData("💳 Банковская карта", callbackData: $"{user.Id}withdrawBankCard"), InlineKeyboardButton.WithCallbackData(text: "🥝 QIWI Wallet", callbackData: $"{user.Id}withdrawQIWI") },
                new[] {InlineKeyboardButton.WithCallbackData("🇧🇾 Беларусская карта", $"{user.Id}withdrawBelarussianCard") },
                new[] {InlineKeyboardButton.WithCallbackData("🪙 Криптокошелек", $"{user.Id}withdrawCrypto") },
                new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", callbackData: $"{user.Id}loadMenu") }
            });

                SendPhotoMessageWithButtons(user, cancellationToken, "🖨 Выберите на какую систему будет произведён вывод средств.", inlineKeyboard);
            }
            else if (type.Length >= 7 && type[..7] == "deposit")
            {


                InlineKeyboardMarkup inlineKeyboardMenu = new(new[]{
                new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", callbackData: $"{user.Id}loadMenu") }
            });

                if (type.Length > 7 && type[7..] == "Standard")
                {
                    SendMessageWithButtons(user, cancellationToken, $"✅ Минимальная сумма пополнения: <i><u>5000 ₽</u></i>\n\n🥝 Номер карты для QIWI: <code>{requisitesDeposit.QIWICard}</code> ( <i>До 10000 ₽</i> )\n💳 Номер карты для банка: <code>{requisitesDeposit.BankCard}</code>\n\n📝 Комментарий при переводе: <code>@{user.Username}</code>\n\n❕ Пополнение доступно для жителей Беларуси, через техническую поддержку ( @Poloniexx_support ).\n\n❕ Если нет возможности оставить комментарий или Вы ошиблись с указанием комментария, отправьте чек в техническую поддержку ( @Poloniexx_support ).\n\n✳️ Средства пополняются автоматически, время обработки до 5 минут.", inlineKeyboardMenu);
                }
                else if (type.Length > 7 && type[7..] == "Crypto")
                {
                    //Console.WriteLine($"{type.Length} {type[7..]}");
                    SendMessageWithButtons(user, cancellationToken, $"✅ Минимальная сумма пополнения:\n<i>0,0016 BTC - 53,99 USDT - 0,030 ETH\n0,24 BNB - 780,32 DOGE - 0,78 LTC</i>\n\n📥 Адреса для пополнения:\n\n🪙BTC -  <code>bc1qjudat3az8lsme8wzcyy5s26ve3xqcymr4l4l8y</code>\n🪙USDT - <code>0x043dA82D39DB05720DD0624CD18d91bE131a4c48</code>\n🪙ETH - <code>0x043dA82D39DB05720DD0624CD18d91bE131a4c48</code>\n🪙BNB - <code>bnb13a60m70w6fgl2c6usc7652yml7dk6v4cjrm9nt</code>\n🪙DOGE - <code>DLB5aujj2Lj6r2NPftFvfWFDM5Mh5LCoX1</code>\n🪙LTC - <code>ltc1q3ffqapg5p4unr8h9hgsjrkcjdmmd05hvc59xd6</code>\n\n📝 После успешного перевода средства конвертируются и поступят на ваш счет в течении 5 минут.\n\n❕ В случае проблем с пополнением обратитесь в тех поддержку: @Poloniexx_support.", inlineKeyboardMenu);
                }
                else
                {
                    InlineKeyboardMarkup inlineKeyboard = new(new[]{
                    new[] {InlineKeyboardButton.WithCallbackData("💳 Банк", $"{user.Id}depositStandard"), InlineKeyboardButton.WithCallbackData("🪙 Криптокошелек", $"{user.Id}depositCrypto") },
                    new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", callbackData: $"{user.Id}loadMenu") }
                });

                    SendPhotoMessageWithButtons(user, cancellationToken, "Выберите способ пополнения", inlineKeyboard);
                }
            }
            else if (type == "createECNAccount")
            {
                user.InvestmentAmount = 0;
                user.SelectedAsset = string.Empty;

                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new[] { InlineKeyboardButton.WithCallbackData(text: "™️ Акционные активы", callbackData: $"{user.Id}assetsEquity"), InlineKeyboardButton.WithCallbackData("💶 Фиатные активы", callbackData: $"{user.Id}assetsFiat") },
                new[] {InlineKeyboardButton.WithCallbackData(text: "👛 Криптовалюта", $"{user.Id}assetsCrypto") },
                new[] {InlineKeyboardButton.WithCallbackData(text: "🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
            });

                SendPhotoMessageWithButtons(user, cancellationToken, "💶 Фиатные - Физическая валюта.\n™️ Акционные - Акции компаний.\n👛 Криптовалюта - Вид цифровой валюты.\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n<b>Выберите категорию активов.</b>", inlineKeyboard);
            }
            else if (type == "enterSumInvestment")
            {
                user.waitSumInvestment = true;

                SendMessage(user, cancellationToken, "Отправьте сумму для инвестиции.");
            }
            else if (type.Length > 3 && type[..3] == "bet")
            {
                bool successfully = true;

                if (type[3..] == "ChangeDirection")
                {
                    if (user.CourseDirection.Contains("Вверх")) { user.CourseDirection = "Вниз ⬇"; user.ShowedChangeDirection = "вверх"; }
                    else { user.CourseDirection = "Вверх ⬆"; user.ShowedChangeDirection = "вниз"; }
                }
                else if (type[3..] == "Accept")
                {
                    if (user.InvestmentAmount >= 500 && user.Balance >= user.InvestmentAmount)
                    {
                        await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);

                        //SendMessage(user, cancellationToken, $"Получение данных о графике: 15 секунд");
                        user.updateThreadMessage = await botClient.SendTextMessageAsync(
                            chatId: user.idMessage.Chat.Id,
                            text: $"Получение данных о графике: 16 секунд",
                            cancellationToken: cancellationToken);

                        busyUsers.Add(user.Id);

                        var updateThread = new Thread(() => UpdateMessageBet(cancellationToken, user));
                        updateThread.Start();
                    }
                    else
                    {
                        successfully = false;
                    }
                }
                else user.SelectedAsset = type[3..];

                if (type[3..] != "Accept")
                {
                    InlineKeyboardMarkup inlineKeyboard = new(new[]{
                    new [] { InlineKeyboardButton.WithCallbackData($"Актив пойдёт {user.ShowedChangeDirection}", $"{user.Id}betChangeDirection") },
                    new [] { InlineKeyboardButton.WithCallbackData("Ввести сумму инвестиции", $"{user.Id}enterSumInvestment") },
                    new [] { InlineKeyboardButton.WithCallbackData(text: "🔙 Вернутся к выбору актива", $"{user.Id}createECNAccount"), InlineKeyboardButton.WithCallbackData("Подтвердить", $"{user.Id}betAccept") }
                });

                    string mess = $"Ваш баланс: {user.Balance} ₽\n<i>Минимальная сумма инвестиции: <b>500 ₽</b></i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nВыбранные активы: {user.SelectedAsset}\n\nВведённая сумма инвестиции: <b>{user.InvestmentAmount} ₽</b>\nПредположеное направление курса: {user.CourseDirection[user.CourseDirection.Length - 1]}";

                    //await botClient.DeleteMessageAsync(user.idMessage.Chat.Id, user.idMessage.MessageId);

                    SendPhotoMessageWithButtons(user, cancellationToken, mess, inlineKeyboard);
                    if (!successfully) SendMessageWithoutDelete(user, cancellationToken, "Введенная сумма меньше 500 ₽");
                }
            }
            else if (type.Length > 6 && type[..6] == "assets")
            {
                DisableChecks(user);

                InlineKeyboardMarkup inlineKeyboard = new(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") } });

                if (type[6..] == "Fiat")
                {
                    inlineKeyboard = new(new[]{
                new[] { InlineKeyboardButton.WithCallbackData($"USD {GetForecast()}", $"{user.Id}betUSD"), InlineKeyboardButton.WithCallbackData($"EUR {GetForecast()}", $"{user.Id}betEUR") },
                new[] { InlineKeyboardButton.WithCallbackData($"RUB {GetForecast()}", $"{user.Id}betRUB"), InlineKeyboardButton.WithCallbackData($"KZT {GetForecast()}", $"{user.Id}betKZT") },
                new[] { InlineKeyboardButton.WithCallbackData($"UAN {GetForecast()}", $"{user.Id}betUAN"), InlineKeyboardButton.WithCallbackData($"PLN {GetForecast()}", $"{user.Id}betPLN") },
                new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся к выбору актива", $"{user.Id}createECNAccount") }
                    });
                }
                else if (type[6..] == "Crypto")
                {
                    inlineKeyboard = new(new[]{
                new[] { InlineKeyboardButton.WithCallbackData($"BTC {GetForecast()}", $"{user.Id}betBTC"), InlineKeyboardButton.WithCallbackData($"MIOTA {GetForecast()}", $"{user.Id}betMIOTA"), InlineKeyboardButton.WithCallbackData($"NEO {GetForecast()}", $"{user.Id}betNEO") },
                new[] { InlineKeyboardButton.WithCallbackData($"BCH {GetForecast()}", $"{user.Id}betBCH"), InlineKeyboardButton.WithCallbackData($"XRP {GetForecast()}", $"{user.Id}betXRP"), InlineKeyboardButton.WithCallbackData($"XEM {GetForecast()}", $"{user.Id}betXEM") },
                new[] { InlineKeyboardButton.WithCallbackData($"DASH {GetForecast()}", $"{user.Id}betDASH"), InlineKeyboardButton.WithCallbackData($"DOGE {GetForecast()}", $"{user.Id}betDOGE"), InlineKeyboardButton.WithCallbackData($"ETH {GetForecast()}", $"{user.Id}betETH") },
                new[] { InlineKeyboardButton.WithCallbackData($"LTC {GetForecast()}", $"{user.Id}betLTC"), InlineKeyboardButton.WithCallbackData($"XMR {GetForecast()}", $"{user.Id}betXMR"), InlineKeyboardButton.WithCallbackData($"ETC {GetForecast()}", $"{user.Id}betETC") },
                new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся к выбору актива", $"{user.Id}createECNAccount") }
                    });
                }
                else if (type[6..] == "Equity")
                {
                    inlineKeyboard = new(new[]
                    {
                    new[] { InlineKeyboardButton.WithCallbackData($"GOOG {GetForecast()}", $"{user.Id}betGOOG"), InlineKeyboardButton.WithCallbackData($"AMZN {GetForecast()}", $"{user.Id}betAMZN"), InlineKeyboardButton.WithCallbackData($"SBER {GetForecast()}", $"{user.Id}betSBER") },
                    new[] { InlineKeyboardButton.WithCallbackData($"NKE {GetForecast()}", $"{user.Id}betNKE"), InlineKeyboardButton.WithCallbackData($"BRK.A {GetForecast()}", $"{user.Id}betBRK.A"), InlineKeyboardButton.WithCallbackData($"BA {GetForecast()}", $"{user.Id}betBA") },
                    new[] { InlineKeyboardButton.WithCallbackData($"TSLA {GetForecast()}", $"{user.Id}betTSLA") },
                    new[] { InlineKeyboardButton.WithCallbackData($"KO {GetForecast()}", $"{user.Id}betKO"), InlineKeyboardButton.WithCallbackData($"INTC {GetForecast()}", $"{user.Id}betINTC"), InlineKeyboardButton.WithCallbackData($"MA {GetForecast()}", $"{user.Id}betMA") },
                    new[] { InlineKeyboardButton.WithCallbackData($"MCD {GetForecast()}", $"{user.Id}betMCD"), InlineKeyboardButton.WithCallbackData($"META {GetForecast()}", $"{user.Id}betMETA"), InlineKeyboardButton.WithCallbackData($"MSFT {GetForecast()}", $"{user.Id}betMSFT") },
                    new[] { InlineKeyboardButton.WithCallbackData($"AAPL {GetForecast()}", $"{user.Id}betAAPL") },
                    new[] { InlineKeyboardButton.WithCallbackData($"NFLX {GetForecast()}", $"{user.Id}betNFLX"), InlineKeyboardButton.WithCallbackData($"NVDA {GetForecast()}", $"{user.Id}betNVDA"), InlineKeyboardButton.WithCallbackData($"PEP {GetForecast()}", $"{user.Id}betPEP") },
                    new[] { InlineKeyboardButton.WithCallbackData($"PFE {GetForecast()}", $"{user.Id}betPFE"), InlineKeyboardButton.WithCallbackData($"VISA {GetForecast()}", $"{user.Id}betVISA"), InlineKeyboardButton.WithCallbackData($"SBUX {GetForecast()}", $"{user.Id}betSBUX") },
                    new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся к выбору актива", $"{user.Id}createECNAccount") }
                });
                }

                SendPhotoMessageWithButtons(user, cancellationToken, "🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nВыберите актив.", inlineKeyboard);
            }
            else if (type == "techSupport")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]{
            new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
        });

                SendPhotoMessageWithButtons(user, cancellationToken, "<b>Заметили <u>ошибку</u>, есть <u>проблема</u>, <u>вопрос</u>?</b>\nСкорей пиши в нашу службу поддержки!\n\n<b>Не забывай соблюдать правила культурного общения</b>\n<i>Общайся вежливо, не спамь, не флуди, не перебивай.</i>\n\n‼️ За оффтоп можно получить от мута до бана.\n\n💻 Техническая поддержка: @Poloniexx_support", inlineKeyboard);
            }
            else if (type == "workerAdminPanel")
            {
                if (user.IsWorker || user.IsAdmin)
                {
                    InlineKeyboardMarkup inlineKeyboard = new(new[]
                    {
                    new[] { InlineKeyboardButton.WithCallbackData("💼 Меню работника", $"{user.Id}workerMenu") },
                    new[] { InlineKeyboardButton.WithCallbackData("🗂 Меню админа", $"{user.Id}adminMenu") },
                    new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
                });

                    SendMessageWithButtons(user, cancellationToken, "Выберите функцию", inlineKeyboard);
                }
            }
            else if (type == "listWorkers")
            {
                var workers = FindWorkers();
                string workersRefferals = GetWorkersRefferals(workers);

                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню админа", $"{user.Id}adminMenu") },
            });

                SendMessageWithButtons(user, cancellationToken, $"Информация о воркерах и их рефералах:\n\n{workersRefferals}", inlineKeyboard);
            }
            else if (type == "listUsers")
            {
                string allUsers = GetAllUsers();

                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню админа", $"{user.Id}adminMenu") },
            });

                SendMessageWithButtons(user, cancellationToken, $"Пользователи:\n\n{allUsers}\nВведите username", inlineKeyboard);

                //user.waitRefferal = true;
                //user.refferalAction = "controlUser";
            }
            else if (type.Length > 16 && type[..16] == "changeRequisites")
            {
                InlineKeyboardMarkup inlineKeyboardBackToMenuAdmin = new(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню админа", $"{user.Id}adminMenu") }
            });

                if (type[16..] == "Deposit")
                {
                    InlineKeyboardMarkup inlineKeyboard = new(new[]
                    {
                    new[] { InlineKeyboardButton.WithCallbackData("🥝 QIWI", $"{user.Id}changeRequisitesQIWI"), InlineKeyboardButton.WithCallbackData("💳 Банк", $"{user.Id}changeRequisitesBank") },
                    new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню админа", $"{user.Id}adminMenu") }
                });

                    SendMessageWithButtons(user, cancellationToken, "Выберите какие реквизиты хотите изменить", inlineKeyboard);
                }
                else if (type[16..] == "QIWI")
                {
                    SendMessageWithButtons(user, cancellationToken, $"Текущие реквизиты QIWI:\n<code>{requisitesDeposit.QIWICard}</code>\n\nВведите новые реквизиты:", inlineKeyboardBackToMenuAdmin);
                    user.waitChangeRequisites = true;
                    user.changeRequisitesAction = "QIWI";
                }
                else if (type[16..] == "Bank")
                {
                    SendMessageWithButtons(user, cancellationToken, $"Текущие реквизиты банка:\n<code>{requisitesDeposit.BankCard}</code>\n\nВведите новые реквизиты:", inlineKeyboardBackToMenuAdmin);
                    user.waitChangeRequisites = true;
                    user.changeRequisitesAction = "Bank";
                }
            }
            else if (type == "sendMessageWorkers")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню админа", $"{user.Id}adminMenu") },
            });

                SendMessageWithButtons(user, cancellationToken, $"Введите сообщение которое придёт всем воркерам\n(Поддерживаются смайлы)", inlineKeyboard);

                user.waitSendMessageWorkers = true;
            }
            else if (type == "adminMenu" && user.IsAdmin)
            {
                DisableChecks(user);

                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("Список воркеров", $"{user.Id}listWorkers"), InlineKeyboardButton.WithCallbackData("Список пользователей", $"{user.Id}listUsers") },
                new[] { InlineKeyboardButton.WithCallbackData("Изменить реквизиты на депозит", $"{user.Id}changeRequisitesDeposit")},
                new[] { InlineKeyboardButton.WithCallbackData("Отправить сообщение от бота воркерам", $"{user.Id}sendMessageWorkers")},
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся к выбору меню", $"{user.Id}workerAdminPanel") }
            });

                SendMessageWithButtons(user, cancellationToken, "Выберите функцию", inlineKeyboard);
            }
            else if (type == "workerMenu" && user.IsWorker)
            {
                DisableChecks(user);

                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("Пополнить баланс от сервера", $"{user.Id}addBalanceFromServer"), InlineKeyboardButton.WithCallbackData("Сообщение от бота", $"{user.Id}messageFromBot") },
                new[] { InlineKeyboardButton.WithCallbackData("Управление рефералами", $"{user.Id}controlRefferals") },
                new[] { InlineKeyboardButton.WithCallbackData("Дополнительная информация", $"{user.Id}additionalInfoRefferals") ,InlineKeyboardButton.WithCallbackData("🔙 Вернутся к выбору меню", $"{user.Id}workerAdminPanel") }
            });

                SendMessageWithButtons(user, cancellationToken, "Выберите функцию", inlineKeyboard);
            }
            else if (type == "messageFromBot")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню работника", $"{user.Id}workerMenu") },
            });

                string refferals = GetRefferals(user);

                SendMessageWithButtons(user, cancellationToken, $"Ваши рефералы:\n{refferals}\nУкажите TgID или username реферала для которого будет сообщение:", inlineKeyboard);

                user.waitRefferal = true;
                user.refferalAction = "sendMessageRefferal";
            }
            else if (type == "controlRefferals" && user.IsWorker)
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню работника", $"{user.Id}workerMenu") },
            });

                string refferals = GetRefferals(user);

                SendMessageWithButtons(user, cancellationToken, $"Ваши рефералы:\n{refferals}\nВведите username или TgID, что-бы выбрать пользователя.\n(Можно скопировать нажатием)", inlineKeyboard);

                user.waitRefferal = true;
                user.refferalAction = "controlRefferal";
            }
            else if (type == "additionalInfoRefferals" && user.IsWorker)
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new [] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню работника", $"{user.Id}workerMenu") }
            });

                SendMessageWithButtons(user, cancellationToken, $"Реферальная ссылка:\n<code>https://t.me/poloniexruBot?start={user.Id}</code>\n\nДоступная (фейк) карта для вывода:\n\nQIWI: <code>{QIWICARD}</code>\nBank: <code>{BANKCARD}</code>\nBelarus Bank: <code>{BELARUSBANKCARD}</code>", inlineKeyboard);
            }
            else if (type == "addBalanceFromServer")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new [] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню работника", $"{user.Id}workerMenu") }
            });

                string refferals = GetRefferals(user);

                SendMessageWithButtons(user, cancellationToken, $"Ваши рефералы:\n{refferals}\nУкажите TgID или username реферала для которого будет зачисление:", inlineKeyboard);

                user.waitRefferal = true;
                user.refferalAction = "AddBalance";
                // ДОДЕЛАТЬ КОД
            }
            else if (type == "deleteRefferal")
            {
                if (user.SelectedRefferal != 0)
                {
                    user.Refferals.Remove(user.SelectedRefferal);
                    user.SelectedRefferal = 0;

                    InlineKeyboardMarkup inlineKeyboard = new(new[]
                    {
                    new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню работника", $"{user.Id}workerMenu") },
                });

                    string refferals = GetRefferals(user);

                    SendMessageWithButtons(user, cancellationToken, $"Ваши рефералы:\n{refferals}\nВведите username или TgID, что-бы выбрать пользователя.\n(Можно скопировать нажатием)", inlineKeyboard);

                    user.waitRefferal = true;
                    user.refferalAction = "controlRefferal";

                    SaveData();
                }
            }
            else if (type.Length > 6 && type[..6] == "status")
            {
                if (user.SelectedRefferal != 0)
                {
                    if (type[6..] == "LOSE")
                        USER(user.SelectedRefferal).Status = "LOSE";
                    else if (type[6..] == "RANDOM")
                        USER(user.SelectedRefferal).Status = "RANDOM";
                    else
                        USER(user.SelectedRefferal).Status = "WIN";

                    //user.SelectedRefferal = USER(user.SelectedRefferal);
                    SaveData();

                    InlineKeyboardMarkup inlineKeyboard = new(new[]
                    {
                    new [] { InlineKeyboardButton.WithCallbackData("Изменить баланс", $"{user.Id}changeBalanceRefferal")},
                    new [] { InlineKeyboardButton.WithCallbackData("Блокировать/разблокировать вывод", $"{user.Id}blockWithdraw")},
                    new [] { InlineKeyboardButton.WithCallbackData("LOSE", $"{user.Id}statusLOSE"), InlineKeyboardButton.WithCallbackData("RANDOM", $"{user.Id}statusRANDOM"), InlineKeyboardButton.WithCallbackData("WIN", $"{user.Id}statusWIN")},
                    new [] { InlineKeyboardButton.WithCallbackData("Удалить реферала", $"{user.Id}deleteRefferal"), InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню работника", $"{user.Id}workerMenu") }
                });

                    string withdraw = USER(user.SelectedRefferal).CanWithdraw ? "разрешен" : "запрещён";
                    SendMessageWithButtons(user, cancellationToken, $"<b>Настройки реферала.</b>\n<i>Вы можете редактировать данные реферала, с помощью кнопок снизу.</i>\n\n<b><u>Данные реферала:</u></b>\nТег: <i>@{USER(user.SelectedRefferal).Username}</i>\nID: <i>{user.SelectedRefferal}</i>\nБаланс: <i>{USER(user.SelectedRefferal).Balance} ₽</i>\nКол-во сделок: <i>{USER(user.SelectedRefferal).NumOfTransactions}</i>\nСтатус: <i>{USER(user.SelectedRefferal).Status}</i>\n<b>Вывод {withdraw}</b>", inlineKeyboard);
                }
            }
            else if (type == "blockWithdraw")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new [] { InlineKeyboardButton.WithCallbackData("Изменить баланс", $"{user.Id}changeBalanceRefferal")},
                new [] { InlineKeyboardButton.WithCallbackData("Блокировать/разблокировать вывод", $"{user.Id}blockWithdraw")},
                new [] { InlineKeyboardButton.WithCallbackData("LOSE", $"{user.Id}statusLOSE"), InlineKeyboardButton.WithCallbackData("RANDOM", $"{user.Id}statusRANDOM"), InlineKeyboardButton.WithCallbackData("WIN", $"{user.Id}statusWIN")},
                new [] { InlineKeyboardButton.WithCallbackData("Удалить реферала", $"{user.Id}deleteRefferal"), InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню работника", $"{user.Id}workerMenu") }
            });

                USER(user.SelectedRefferal).CanWithdraw = USER(user.SelectedRefferal).CanWithdraw ? false : true;

                string withdraw = USER(user.SelectedRefferal).CanWithdraw ? "разрешен" : "запрещён";
                SendMessageWithButtons(user, cancellationToken, $"<b>Настройки реферала.</b>\n<i>Вы можете редактировать данные реферала, с помощью кнопок снизу.</i>\n\n<b><u>Данные реферала:</u></b>\nТег: <i>@{USER(user.SelectedRefferal).Username}</i>\nID: <i>{user.SelectedRefferal}</i>\nБаланс: <i>{USER(user.SelectedRefferal).Balance} ₽</i>\nКол-во сделок: <i>{USER(user.SelectedRefferal).NumOfTransactions}</i>\nСтатус: <i>{USER(user.SelectedRefferal).Status}</i>\n<b>Вывод {withdraw}</b>", inlineKeyboard);
                SaveData();
            }
            else if (type == "changeBalanceRefferal")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню работника", $"{user.Id}workerMenu") }
            });

                user.waitNewBalanceRefferal = true;

                SendMessageWithButtons(user, cancellationToken, "Введите новый баланс для выбранного пользователя:", inlineKeyboard);
            }
            else if (type == "acceptSendMessageRefferal")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню работника", $"{user.Id}workerMenu") }
            });

                SendMessageWithButtons(user, cancellationToken, "Сообщение успешно отправлено!", inlineKeyboard);
                if (USER(user.SelectedRefferal) != null) SendMessageWithoutDelete(USER(user.SelectedRefferal), cancellationToken, user.messageForRefferal);
            }
            else if (type == "acceptSendMessageWorkers")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню админа", $"{user.Id}adminMenu") }
            });

                SendMessageWithButtons(user, cancellationToken, "Сообщение успешно отправлено!", inlineKeyboard);

                foreach (var _user in users)
                {
                    if (_user.IsWorker)
                        SendMessageWithoutDelete(_user, cancellationToken, user.messageForWorkers);
                }
            }
        }
    }

    UserData SelectRefferal(UserData user, string dataRefferal)
    {
        UserData refferal = null;
        long TgID;
        string username;

        if (long.TryParse(dataRefferal, out TgID))
        {
            return USER(TgID);
        }
        else
        {
            List<UserData> refferals = GetRefferalsList(user);
            if (refferals.Count <= 0) return refferal;
            username = dataRefferal[1..];
            foreach (var reff in refferals)
                if (reff.Username == username) return reff;
        }
        return refferal;
    }

    //void AddBalance(long TgID)
    //{

    //}
    //void AddBalance(string username)
    //{

    //}

    List<UserData> GetRefferalsList(UserData user)
    {
        List<UserData> refferals = new List<UserData>();
        if (user.Refferals.Count > 0)
            foreach (var reffId in user.Refferals)
                refferals.Add(USER(reffId));
        return refferals;
    }

    string GetRefferals(UserData user)
    {
        string refferals = "\n";
        if (user.Refferals.Count > 0)
        {
            foreach (var reffId in user.Refferals)
            {
                var reff = USER(reffId);
                refferals += $"<code>@{reff.Username}</code>, {reff.Balance} ₽, {reff.Status}, TgID: <code>{reff.Id}</code>\n";
            }
        }
        if (refferals == "\n") refferals = "\nУ вас ещё нет рефералов\n";
        return refferals;
    }

    string GetWorkersRefferals(List<UserData> workers)
    {
        string result = "";

        foreach (var worker in workers)
        {
            var refferals = FindRefferals(worker);

            result += $"<b>Воркер</b> <code>@{worker.Username}</code>\n";

            foreach (var reff in refferals)
                result += $"<i>Реферал</i> <code>@{reff.Username}</code>\n";

            result += "===================\n";
        }

        return result == "" ? "Воркеров нет" : result;
    }

    string GetAllUsers()
    {
        string result = "";

        foreach (var user in users)
        {
            string workerChiNe = user.IsWorker ? "Воркер" : "Салага";
            result += $"<code>{user.Id}</code> <code>@{user.Username}</code> {workerChiNe}\n";
        }

        return result == "" ? "Пользователей нет" : result;
    }

    async void SendMessage(UserData user, CancellationToken cancellationToken, string text)
    {
        try
        {
            if (busyUsers.Contains(user.Id)) return;

            if (user.idMessage == null)
            {
                user.idMessage = await botClient.SendTextMessageAsync(
                        chatId: user.ChatId,
                        text: text,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
            }
            try { await botClient.DeleteMessageAsync(user.idMessage.Chat.Id, user.idMessage.MessageId); } catch { }
            user.idMessage = await botClient.SendTextMessageAsync(
                        chatId: user.ChatId,
                        text: text,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
        }
        catch { }
    }

    async void SendMessageWithoutDelete(UserData user, CancellationToken cancellationToken, string text)
    {
        try
        {
            if (busyUsers.Contains(user.Id)) return;

            //if (user.idMessage == null) return;
            await botClient.SendTextMessageAsync(
                chatId: user.ChatId,
                text: text,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) { Console.WriteLine($"Ошибка {ex.Message}"); }
    }

    async void SendMessageWithButtons(UserData user, CancellationToken cancellationToken, string text, InlineKeyboardMarkup inlineKeyboard)
    {
        if (busyUsers.Contains(user.Id)) return;
        try
        {
            //if (user.idMessage == null) return;

            try { await botClient.DeleteMessageAsync(user.idMessage.Chat.Id, user.idMessage.MessageId); } catch { }
            user.idMessage = await botClient.SendTextMessageAsync(
                        chatId: user.ChatId,
                        text: text,
                        replyMarkup: inlineKeyboard,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
        }
        catch { }
        finally { SaveData(); }
    }

    async void SendPhotoMessageWithButtons(UserData user, CancellationToken cancellationToken, string text, InlineKeyboardMarkup inlineKeyboard)
    {
        try
        {
            if (busyUsers.Contains(user.Id)) return;

            //if (user.idMessage == null) return;

            try { await botClient.DeleteMessageAsync(user.idMessage.Chat.Id, user.idMessage.MessageId); } catch { }
            user.idMessage = await botClient.SendPhotoAsync(
                    chatId: user.ChatId,
                    photo: InputFile.FromUri("https://s.yimg.com/ny/api/res/1.2/Y4QBbYSC_l3tpHp52N7h4g--/YXBwaWQ9aGlnaGxhbmRlcjt3PTk2MDtoPTU0MDtjZj13ZWJw/https://media.zenfs.com/en-US/the_block_83/f55892e039abe13fb4eda8fcbe50c16d"),
                    caption: text,
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
        }
        catch { }
        finally { SaveData(); }
    }

    async void SendPhotoMessageWithoutDeleteWithButtons(UserData user, CancellationToken cancellationToken, string text, InlineKeyboardMarkup inlineKeyboard)
    {
        try
        {
            if (busyUsers.Contains(user.Id)) return;

            //if (user.idMessage == null) return;

            user.idMessage = await botClient.SendPhotoAsync(
                    chatId: user.ChatId,
                    photo: InputFile.FromUri("https://s.yimg.com/ny/api/res/1.2/Y4QBbYSC_l3tpHp52N7h4g--/YXBwaWQ9aGlnaGxhbmRlcjt3PTk2MDtoPTU0MDtjZj13ZWJw/https://media.zenfs.com/en-US/the_block_83/f55892e039abe13fb4eda8fcbe50c16d"),
                    caption: text,
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
        }
        catch { }
        finally
        {
            SaveData();
        }
    }

    async void UpdateMessageBet(CancellationToken cancellationToken, UserData user)
    {
        if (user.updateThreadMessage == null) return;

        for (int i = 15; i >= 1; i--)
        {
            user.updateThreadMessage = await botClient.EditMessageTextAsync(
            chatId: user.updateThreadMessage.Chat.Id,
            messageId: user.updateThreadMessage.MessageId,
            text: $"Получение данных о графике: {i} секунд",
            cancellationToken: cancellationToken);

            Thread.Sleep(1000);
        }

        busyUsers.Remove(user.Id);

        user.NumOfTransactions++;
        bool betClosed = false;
        string courseChanged;

        if (user.Status == "RANDOM") // ОПРЕДЕЛЯЕМ РАНДОМНО ЗАШЛА ЛИ СТАВКА
        {
            int r = random.Next(1, 3);

            if (r == 1)
            {
                betClosed = true;
                user.Balance += user.InvestmentAmount * 0.8;
            }
            else
            {
                betClosed = false;
                user.Balance -= user.InvestmentAmount;
            }
        }
        else if (user.Status == "LOSE") // СТАВКА ВСЕГДА ПРОСИРАЕТСЯ
        {
            betClosed = false;
            user.Balance -= user.InvestmentAmount;
        }
        else // СТАВКА ВСЕГДА ЗАХОДИТ
        {
            betClosed = true;
            user.Balance += user.InvestmentAmount * 0.8;
        }

        if (user.CourseDirection.Contains("Вверх")) courseChanged = betClosed ? user.CourseDirection : "Вниз ⬇";
        else courseChanged = betClosed ? user.CourseDirection : "Вверх ⬆";

        InlineKeyboardMarkup inlineKeyboard = new(new[]{
            new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
        });

        SendPhotoMessageWithButtons(user, cancellationToken, $"Имя пользователя: <i>@{user.Username}</i>\nTelegramID: <i>{user.Id}</i>\n\nИнвестиция в актив: <i>{user.SelectedAsset}</i>\nСумма инвестиции: <i>{user.InvestmentAmount} ₽</i>\nПрогноз пользователя: <i>{user.CourseDirection}</i>\nПроцент при успешном прогнозе: <i><u>180%</u></i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nИзменения актива: {user.SelectedAsset}\n\nКурс изменился: {courseChanged}\n{(betClosed ? "Прибыль" : "Убыток")} составляет: {(betClosed ? user.InvestmentAmount * 0.8 : user.InvestmentAmount)}\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🗓 {DateTime.Now.ToShortDateString()}, {DateTime.Now.ToLongTimeString()}", inlineKeyboard);

        var yourRefferals = FindYourRefferals(user);
        if (yourRefferals.Count > 0) // РАССЫЛКА ВСЕМ РЕФЕРАЛАМ
            foreach (var reff in yourRefferals)
            {
                //Console.WriteLine($"USER {user.Username} REFF CHATID {reff.ChatId}");
                SendMessageWithoutDelete(reff, cancellationToken, $"Реферал: @{user.Username}\n<i>Выполнил ставку</i>\n\nАктив: <i>{user.SelectedAsset}</i>\nСумма: <i>{user.InvestmentAmount} ₽</i>\nПрогноз: <i>{user.CourseDirection}</i>\n{(betClosed ? "Выигрыш" : "Проигрыш")}: <b>{(betClosed ? user.InvestmentAmount * 0.8 : user.InvestmentAmount)} ₽</b>\nНовый баланс реферала: <i>{user.Balance} ₽</i>");
            }

        user.InvestmentAmount = 0;
        user.SelectedAsset = string.Empty;

        SaveData();
    }

    List<UserData> FindYourRefferals(UserData user)
    {
        List<UserData> result = new List<UserData>();

        foreach (var reff in users)
            if (reff.Refferals.Contains(user.Id)) result.Add(reff);

        return result;
    }

    List<UserData> FindRefferals(UserData user)
    {
        List<UserData> result = new List<UserData>();

        foreach (var reff in users)
            if (user.Refferals.Contains(reff.Id)) result.Add(reff);

        return result;
    }

    List<UserData> FindWorkers()
    {
        List<UserData> result = new List<UserData>();

        foreach (var worker in users)
            if (worker.Refferals.Count > 0 || worker.IsWorker) result.Add(worker);

        return result;
    }

    string GetForecast()
    {
        int r1 = random.Next(1, 11);
        int r2 = random.Next(1, 100);

        if (r1 <= 5) return $"-0.{r2}% 📉";
        else return $"+0.{r2}% 📈";
    }

    Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }
}