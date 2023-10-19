using Newtonsoft.Json;
using System.Linq;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

TGBot telegramBot = new TGBot();
await telegramBot.Main();

class UserData
{
    public double Balance { get; set; }
    public int NumOfTransactions { get; set; }
    public DateTime DateOfRegister { get; set; }
    public string Username { get; set; }
    public long Id { get; set; }

    public UserData(long id, string username)
    {
        Balance = 0;
        NumOfTransactions = 0;
        DateOfRegister = DateTime.Now;
        Id = id;
        Username = username;
    }
}

class TGBot
{
    Message idMessage = null;
    ITelegramBotClient botClient;

    Random random = new Random();

    bool waitSumWithdraw = false;
    bool waitRequisites = false;

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

            if (message.Text == "/start")
            {
                DisableChecks();

                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new [] { InlineKeyboardButton.WithCallbackData("🧮 Открыть ECN счёт", "createECNAccount") },
                new [] { InlineKeyboardButton.WithCallbackData("💳 Внести средства", "deposit") },
                new [] { InlineKeyboardButton.WithCallbackData("🏦 Вывести средства", "withdraw") },
                new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://crypto.ru/otzyvy-poloniex/"), InlineKeyboardButton.WithCallbackData("👨‍💻 Тех Поддерджка", "techSupport") }
            });

                idMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"👤Личный кабинет: @{message.Chat.Username}\n<i>🔎 TlgmID: {chatId}</i>\n\n💰 Баланс: <i>{user.Balance} ₽</i>\n🤝🏻 Кол-во сделок: <i>{user.NumOfTransactions}</i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nRUB 🟢 ➗    KZT  🟢 ➗    UAH 🟢\nUSD 🟢 ➗    EUR  🟢 ➗    PLN  🟢\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🔸 C нами уже более 10⁷ пользователей 🔸\n\n📅 Дата регистрации: {user.DateOfRegister.ToLongDateString()}   {user.DateOfRegister.ToLongTimeString()}",
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
            else if (type[..6] == "assets")
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