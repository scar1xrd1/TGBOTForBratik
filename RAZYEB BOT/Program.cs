using Microsoft.VisualBasic;
using System.Runtime.InteropServices;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

Message idMessage = null;
ITelegramBotClient botClient;

double balance = 5000;
int numOfTransactions = 2;
DateTime dateOfRegister = DateTime.Now;

await Main();

async Task Main()
{
    botClient = new TelegramBotClient("6470974022:AAHzzroOWNOUjCjHVE55oPiC2ZEGUwG3ZQc");

    using CancellationTokenSource cts = new();

    // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
    ReceiverOptions receiverOptions = new()
    {
        AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
    };

    botClient.StartReceiving(
        updateHandler: HandleUpdateAsync,
        pollingErrorHandler: HandlePollingErrorAsync,
        receiverOptions: receiverOptions,
        cancellationToken: cts.Token        
    );

    var me = await botClient.GetMeAsync();

    Console.WriteLine($"Start listening for @{me.Username}");
    Console.ReadLine();

    cts.Cancel();
}

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    Message? message = null;
    if (update.Message != null) message = update.Message;

    if (message != null && message.Text == "/start")
    {
        InlineKeyboardMarkup inlineKeyboard = new(new[]{
            new [] { InlineKeyboardButton.WithCallbackData(text: "🧮 Открыть ECN счёт", callbackData: "createECNAccount") },
            new [] { InlineKeyboardButton.WithCallbackData(text: "💳 Внести средства", callbackData: "11") },
            new [] { InlineKeyboardButton.WithCallbackData(text: "🏦 Вывести средства", callbackData: "11") },
            new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://github.com/TelegramBots/Telegram.Bot"), InlineKeyboardButton.WithCallbackData(text: "👨‍💻 Тех Поддерджка", callbackData: "11") }
        });

        idMessage = await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"👤Личный кабинет: @{message.Chat.Username}\n<i>🔎 TlgmID: {message.Chat.Id}</i>\n\n💰 Баланс: <i>{balance} ₽</i>\n🤝🏻 Кол-во сделок: <i>{numOfTransactions}</i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nRUB 🟢 ➗    KZT  🟢 ➗    UAH 🟢\nUSD 🟢 ➗    EUR  🟢 ➗    PLN  🟢\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🔸 C нами уже более 10⁷ пользователей 🔸\n\n📅 Дата регистрации: {dateOfRegister.ToLongDateString()} {dateOfRegister.ToLongTimeString()}",
            replyMarkup: inlineKeyboard,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
 

    if (update.CallbackQuery is { } callbackQuery)
    {
        var callbackData = callbackQuery.Data; // Текст который вернула инлайн-кнопка

        if (idMessage != null)
        {
            //Console.WriteLine($"USER: {message.Chat.Username}, CALLBACKDATA: {callbackData}");

            SendButtons(botClient, idMessage, cancellationToken, callbackData);
            //if (callbackData == "createECNAccount")
            //{
                
            //}
            //else if(callbackData == "loadMenu")
            //{
            //    LoadMenu(botClient, idMessage, cancellationToken);
            //}
        }       
    } 
}

async void SendButtons(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, string type)
{
    if(message != null)
    {
        if(type == "loadMenu")
        {
            InlineKeyboardMarkup inlineKeyboard = new(new[]{
                new [] { InlineKeyboardButton.WithCallbackData(text: "🧮 Открыть ECN счёт", callbackData: "createECNAccount") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "💳 Внести средства", callbackData: "11") },
                new [] { InlineKeyboardButton.WithCallbackData(text: "🏦 Вывести средства", callbackData: "11") },
                new [] { InlineKeyboardButton.WithUrl(text: "📒 Отзывы о нас", url: "https://github.com/TelegramBots/Telegram.Bot"), InlineKeyboardButton.WithCallbackData(text: "👨‍💻 Тех Поддерджка", callbackData: "11") }
             });

            if (idMessage != null)
            {
                idMessage = await botClient.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: idMessage.MessageId,
                    text: $"👤Личный кабинет: @{message.Chat.Username}\n<i>🔎 TlgmID: {message.Chat.Id}</i>\n\n💰 Баланс: <i>{balance} ₽</i>\n🤝🏻 Кол-во сделок: <i>{numOfTransactions}</i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nRUB 🟢 ➗    KZT  🟢 ➗    UAH 🟢\nUSD 🟢 ➗    EUR  🟢 ➗    PLN  🟢\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🔸 C нами уже более 10⁷ пользователей 🔸\n\n📅 Дата регистрации: {dateOfRegister.ToLongDateString()} {dateOfRegister.ToLongTimeString()}",
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            else
            {
                idMessage = await botClient.SendTextMessageAsync(
                   chatId: message.Chat.Id,
                   text: $"👤Личный кабинет: @{message.Chat.Username}\n<i>🔎 TlgmID: {message.Chat.Id}</i>\n\n💰 Баланс: <i>{balance} ₽</i>\n🤝🏻 Кол-во сделок: <i>{numOfTransactions}</i>\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\nRUB 🟢 ➗    KZT  🟢 ➗    UAH 🟢\nUSD 🟢 ➗    EUR  🟢 ➗    PLN  🟢\n🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰🟰\n🔸 C нами уже более 10⁷ пользователей 🔸\n\n📅 Дата регистрации: {dateOfRegister.ToLongDateString()} {dateOfRegister.ToLongTimeString()}",
                   replyMarkup: inlineKeyboard,
                   parseMode: ParseMode.Html,
                   cancellationToken: cancellationToken);
            }
           
        }
        else if(type == "createECNAccount")
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