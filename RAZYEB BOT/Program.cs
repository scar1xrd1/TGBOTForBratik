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

        if (message != null && message.Text == "/start")
        {
            var chatId = message.Chat.Id;
            var user = USER(chatId);            

            InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new [] { InlineKeyboardButton.WithCallbackData(text: "🧮 Открыть ECN счёт", callbackData: "createECNAccount") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "💳 Внести средства", callbackData: "11") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "🏦 Вывести средства", callbackData: "11") },
                new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://crypto.ru/otzyvy-poloniex/"), InlineKeyboardButton.WithCallbackData(text: "👨‍💻 Тех Поддерджка", callbackData: "techSupport") }
            });

            idMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"👤Личный кабинет: @{message.Chat.Username}\n<i>🔎 TlgmID: {chatId}</i>\n\n💰 Баланс: <i>{user.Balance} ₽</i>\n🤝🏻 Кол-во сделок: <i>{user.NumOfTransactions}</i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nRUB 🟢 ➗    KZT  🟢 ➗    UAH 🟢\nUSD 🟢 ➗    EUR  🟢 ➗    PLN  🟢\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🔸 C нами уже более 10⁷ пользователей 🔸\n\n📅 Дата регистрации: {user.DateOfRegister.ToLongDateString()}   {user.DateOfRegister.ToLongTimeString()}",
                replyMarkup: inlineKeyboard,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
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

    async void SendButtons(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, string type)
    {
        if (message != null && USER(message.Chat.Id) == null) ADDUSER(message.Chat.Id, message.Chat.Username); 
        var user = USER(message.Chat.Id);

        if (message != null)
        {
            if (type == "loadMenu")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new [] { InlineKeyboardButton.WithCallbackData(text: "🧮 Открыть ECN счёт", callbackData: "createECNAccount") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "💳 Внести средства", callbackData: "11") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "🏦 Вывести средства", callbackData: "11") },
                new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://crypto.ru/otzyvy-poloniex/"), InlineKeyboardButton.WithCallbackData(text: "👨‍💻 Тех Поддерджка", callbackData: "techSupport") }
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
            else if (type == "createECNAccount")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new[] { InlineKeyboardButton.WithCallbackData(text: "™️ Акционные активы", callbackData: "equityAssets"), InlineKeyboardButton.WithCallbackData(text: "💶 Фиатные активы", callbackData: "fiatAssets") },
                new[] {InlineKeyboardButton.WithCallbackData(text: "👛 Криптовалюта", callbackData:"crypto") },
                new[] {InlineKeyboardButton.WithCallbackData(text: "🔙 Вернутся в главное меню", callbackData: "loadMenu") }
            });

                await botClient.EditMessageTextAsync(
                    chatId: idMessage.Chat.Id,
                    messageId: idMessage.MessageId,
                    text: "💶 Фиатные - Физическая валюта.\n™️ Акционные - Акции компаний.\n👛 Криптовалюта - Вид цифровой валюты.\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n<b>Выберите категорию активов.</b>",
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            else if (type == "techSupport")
            {
                InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new[] {InlineKeyboardButton.WithCallbackData(text: "🔙 Вернутся в главное меню", callbackData: "loadMenu") }
            });

                await botClient.EditMessageTextAsync(
                    chatId: idMessage.Chat.Id,
                    messageId: idMessage.MessageId,
                    text: "<b>Заметили <u>ошибку</u>, есть <u>проблема</u>, <u>вопрос</u>?</b>\nСкорей пиши в нашу службу поддержки!\n\n<b>Не забывай соблюдать правила культурного общения</b>\n<i>Общайся вежливо, не спамь, не флуди, не перебивай.</i>\n\n‼️ За оффтоп можно получить от мута до бана.\n\n💻 Техническая поддержка: @Poloniexx_support",
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
        }
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