using System.Globalization;
using Zakup.Common.DTO.Zakup;

namespace Zakup.WebHost.Constants;

public static class MessageTemplate
{
    public const string Welcome =  "🌟 *Добро пожаловать в наш робот для закупов!* 🌟\n\n" +
                                   
                                   "Что вы сможете делать?\n" +
                                   "🚀 Создавать рекламный пост *прямо в диалоге с человеком*\n" +
                                   "🔎 Получать *детальный анализ* входящего трафика\n" +
                                   "💎 Определять *источники* ваших лидов\n" +
                                   "🤖 *Автоматически* одобрять заявки в канал\n" +
                                   "🔥* И многое другое!*\n\n" +
                                   "Чтобы начать работу:\n" +

                                   "1️⃣ *Добавьте бота администратором в свой канал с правом на добавление участников.*\n" +
                                   "2️⃣ *Перешлите любой пост из канала в этот чат – так мы удостоверимся, что всё готово.*\n\n" +

                                   "🎯 Добавьте ваш первый креатив и выведите свои рекламные кампании на новый уровень! 🔥\n\n" +
                                   "📚 Если у вас возникнут вопросы, *обязательно загляните в раздел «Помощь»* – там вы найдёте подробные инструкции. А если потребуется дополнительная поддержка, *смело обращайтесь к @gandalftg*.";


    private const string Menu = "*🤖 Добро пожаловать!*\n" +
                                "За сегодня *создано закупов* на сумму *{0}* / *Оплачено* на *{1}*\n\n" +
                                "🟢 Подписалось* на все ваши каналы за сегодня:{2}\n\n" +
                                "🔴 Отписалось* со всех ваших каналов за сегодня: {3}";

    public const string CreateFirstPostRequest = "Вы хотите добавить первый рекламный пост для вашего канала?";
    public const string AutoApproveAcctuallyEnabled = "Автоприём заявок уже включен!";

    public const string WriteAutoApproveMinutes =
        "Введите, через сколько минут нужно автоматически принимать заявки на вступление в канал";

    public const string InvalidApproveTimeError = "Пожалуйста, введите корректное число минут (положительное целое число).";
    public const string LabelChanged = "Метка успешно изменена";
    public const string ChannelAdmins = "Ваши администраторы:";
    public const string AdminNotFound = "Администратор не найден.";
    public const string AdminRemoved = "Администратор удалён";
    public const string ChooseZakupChannel = "Выберите канал для закупа:";
    public const string ChannelsLimitError = "Вы достигли лимита каналов. Удалите какой-нибудь или обратитесь к администратору для повышения привилегий";
    public const string ChooseZakupDate = "📅Введите планируемую дату выхода поста (в формате дд.мм.гггг ЧЧ:ММ) по Москве.";
    public const string ZakupPrice = "💸Введите цену (число)";
    public const string PublicChannelNotification = "🤔Ваш канал публичный. Сделайте его приватным, чтобы появилась возможность создавать ссылки со вступлением по заявке.";
    public const string ChooseLinkType = "Выберите тип публичности ссылки:";
    public const string ZakupChannelAlias = "Перешлите пост из канала, где вас опубликуют, или напишите источник трафика так, *чтобы Вам было понятно*";
    public const string BadZakupChannelAlias = "❌Перешлите сообщение из площадки размещения или напишите название платформы до 60 символов";
    public const string BadZakupDateError = "Не удалось прочитать дату и время, проверьте форматирование и попробуйте снова. (дд.мм.гггг ЧЧ:ММ) по Москве";
    public const string NoZakupsForThisChannel = "Нет размещений для выбранного канала.";
    public const string ChooseZakup = "Выберите размещение:";

    public const string AddAdminRequest =
        "Пожалуйста, перешлите сообщение от пользователя, которого вы хотите добавить в качестве администратора.";

    public const string AdminForwardEmptyError =
        "Пожалуйста, перешлите сообщение от пользователя, которого вы хотите добавить в качестве администратора.";

    public const string IsAlreadyAdmin = "Пользователь уже является администратором этого канала.";
    public const string AdminCreated = "Пользователь успешно добавлен в качестве администратора.";
    public const string ZakupActions = "Выберите действие для закупа:";
    public static string PriceSaved = "✅Цена сохранена";
    public const string ChannelDeleted = "Канал удалён";
    public const string DeleteChannelAlert = "Вы уверены? Канал, вся статистика и все креативы будут удалены, остановится обработка вступлений." +
                                             "\n" +
                                             "Поиск пользователя по юзернейму останется доступным";

    public const string ZakupNotFound = "Размещение не найдено.";
    public const string ChooseZakupCreative = "Выберите креатив для закупа:";

    private const string YourLink =
        "⚡️Ваша ссылка - {0} , используйте её для привлечения аудитории и её дальнейшего анализа через бота.";

    public const string ZakupPayed = "Оплачено: Да✅";
    public const string ZakupNotPayed = "Оплачено: Нет❌";
    public const string DeleteZakupAlert = "Вы действительно хотите удалить размещение?";
    public const string SendMuteRequest = "Пришлите id пользователя или перешлите сообщение от него, чтобы выдать мут.";
    public const string MarkedAsScam = "Канал отмечен как мошеннический!";
    public const string UserUnMuted = "Пользователь размучен!";
    public const string ChannelScamRemoved = "Метка скама снята!";
    public const string SheetNotExist = "У вас нет Гугл Таблицы. Попробуйте добавить канал или обратитесь к @gandalfTG.";
    public static string GoogleSheetsText(string sheetId) =>$"*♻️ ДЛЯ ОБНОВЛЕНИЯ ДАННЫХ НАЖМИТЕ НА КНОПКУ НИЖЕ*\n\nВот ваша таблица: [Перейти](https://docs.google.com/spreadsheets/d/{sheetId})";

    public static string MuteRequestConfirm(string userName) => $"Вы уверены, что хотите замутить пользователя [{userName}] и очистить историю его оценок?";

    public static string ScamRequestConfirm(string channelName)
    {
        return string.IsNullOrEmpty(channelName) ? $"Вы уверены, что хотите отметить канал как скамерский?" : $"Вы уверены, что хотите отметить канал [{channelName}] как скамерский?";
    }

    public static string ZakupSummaryMessage(string channelTitle, decimal price, DateTime? date, string? adPostTitle, bool isPaid) => $"""
                                                                                                                                       🔥 Запланировано размещение для [{channelTitle}]
                                                                                                                                       💸Цена: {price}
                                                                                                                                       📅Дата: {date?.ToString("dd.MM.yyyy HH:mm") ?? "Не установлено"}
                                                                                                                                       Креатив: {adPostTitle ?? "Просто ссылка"}
                                                                                                                                       """ + "\n"
                                                                                                                                           + (isPaid ? ZakupPayed : ZakupNotPayed);
    
    public static string YouLinkMessage(string link)
    {
        return string.Format(YourLink, link);
    }
    public static string GetMenu(decimal zakupPrice = 0, decimal payedPrice = 0, long subscriptions = 0,
        long unsubscriptions = 0)
    {
        return string.Format(Menu, zakupPrice, payedPrice, subscriptions, unsubscriptions);
    }

    public const string ChannelList = "Список каналов:";

    public const string AddChannelRequest =
        "Добавьте бота в ваш канал как администратора (с правом на приглашение участников), а *затем перешлите сообщение из него в этот чат*";
    
    public static string ChannelCreatedMessage(string alias) => $"Канал уже добавлен! Теперь вы можете приступить к работе с ним. Метка канала: {alias}";

    public static string ChannelRestoredMessage(string alias) => $"Канал восстановлен! Метка: {alias}";

    public const string ChannelRestoredNeedAlias = "Канал восстановлен! Теперь введите метку (одно слово)";
    public const string DataRefreshed = "Данные обновлены";

    public const string AddChannelAliasRequest = "Теперь напишите метку канала (любое одно слово)";

    public const string ChannelAliasStateNotification = "Метка должна быть одним словом и не длиннее 15 символов. Пожалуйста, введите снова.";

    public const string FirstPostDecline = "**Без проблем\\!** вы всегда сможете сделать это через меню канала";
    public const string MemberNotFound = "Пользователь не обнаржуен ни в одном из ваших каналов";
    public const string CreativeInstructionText = "⚛️ Установка креатива" +
                                                  "\nОтправьте или перешлите мне рекламный пост." +
                                                  "\nМожно прикрепить фотографии/гифки/видео." +
                                                  "\nДля будущей генерации постов в нём должны быть ссылки, при создании закупа бот автоматически заменит их на новые." +
                                                  "\nСсылки вида @channel бот скрытно делает уникальными" +
                                                  "\nИли напишите /cancel для отмены";

    public const string BadFormat =
        "Не поддерживаемое форматирование! Возможно, вы используете цитаты, но они пока не поддерживаются. Уберите цитаты и повторите попытку.";

    public const string CaptionTooLong =
        "Подпись к медиафайлу слишком длинная! Максимальная длина подписи — 1022 символов. Сократите пост и пришлите его ещё раз.";

    public const string MessageTooLong =
        "Текст слишком длинный! Максимальная длина поста — 4095 символов. Сократите пост и пришлите его ещё раз.";

    public const string FileTooHeavy =
        "Файл слишком большой для обработки. Пожалуйста, отправьте файл меньшего размера.";

    public const string EmptyTextError =
        "Ошибка: текст поста не может быть пустым или слишком коротким (минимум 2 символа). Пожалуйста, добавьте текст и попробуйте снова.";

    public const string AddButtonQuestion = "Добавить кнопку?";
    public const string PostGenerationProcess = "Генерация поста...";
    public const string WriteButtonText = "Введите текст для кнопки";
    
    public const string WriteAliasForPost = "Теперь придумайте метку для этого поста (слово или число для поиска и идентификации)";
    public const string PostTitleEmptyError = "Метка креатива не может быть пустой. Попробуйте снова.";
    public const string TitleTooLongError = "❌Слишком длинная метка (более 30 символов). Попробуйте снова, но короче.";
    public const string BadSymbolsError = "❌Метка содержит недопустимые символы.\n" +
                                            "Разрешены буквы (латиница, кириллица), цифры и пробелы.";

    public const string AdPostDeleted = "Креатив удалён!";
    public const string PdpCheckRequest = "Перешлите мне сообщение из канала, где ранее была опубликована реклама. Подробнее о сверке подписчиков тут: https://t.me/easyzakup/18";

    public const string ChannelNotFound = "Канал не найден в базе данных.";
    public const string PDPBotMessageError = "Вы переслали сообщение от бота. Пожалуйста, перешлите сообщение из канала, в котором ранее размещалась реклама.";
    public const string PDPBadMessageError = "Перешлите сюда пост из канала, в котором ранее размещалась реклама.";
    private const string PDPRequestNotification = "Вам поступил запрос на сверку подписчиков для размещения {0}.\nБот проанализирует, сколько из вступивших подписчиков действительно являются подписчиками канала и пришлёт вам отчёт.\n\nЕсли вы подтверждаете, нажмите кнопку ниже.";

    private const string PdpVerificationResult =
        "Сверка завершена: из {0} вступивших, {1} ({2}%) являются подписчиками канала.";

    public static string PdpVerificationResultMessage(long total, long verifiedCount, double percentage)
    {
        return string.Format(PdpVerificationResult,total,verifiedCount,$"{percentage:F2}");
    }
    public static string PDPRequestNotificationMessage(Guid zakupId)
    {
        return string.Format(PDPRequestNotification, zakupId);
    }
    public const string PDPRequestSent =
        "Запрос на сверку подписчиков отправлен администраторам. Ожидайте подтверждения.";
    public const string PDPNoBotInChannels = "Не удалось получить список администраторов канала. Бот должен быть добавлен в канал как администратор с правом на пригласительные ссылки.\n\n Перешлите сообщение из канала когда он будет добавлен";

    public const string PDPError =
        "Не удалось отправить запрос на сверку подписчиков ни одному администратору. Убедитесь, что хотя бы один из администраторов запустил бота командой /start.";
    public const string PDPNoChatWithAdmins =
        "Ошибка: у бота нет открытого чата ни с одним из администраторов проверяемого канала.\n\nХотя бы один из администраторов должен написать что-нибудь нашему боту, чтобы мы смогли запросить у него подтверждение проверки.";

    private const string AutoApproveIsEnabled =
        "✅ **Автоприём заявок:** ВКЛЮЧЕН. Задержка {0} минут.";
    
    public const string PremiumEmogiText =  "Для того, чтобы получить готовый рекламный пост* с премиум emoji* вы должны вызвать бота через inline *в переписке с ботом*(если вызывать в диалоге с людьми будут обычные смайлики), т.е. написав запрос в этот чат, как на скрине.\n\n" +
                                             "Не обязательно заполнять все данные через меню, как вы сейчас это сделали. Достаточно просто написать в диалоге с этим ботом *@zakup_robot Понятное вам название канала, где вас опубликуют Стоимость(только число) Метка канала(вы её установили ранее)* и выберите ваш крео";
    
    public static string AutoApproveEnabled(long minutes) => string.Format(AutoApproveIsEnabled, minutes);

    public const string AutoApproveDisabled = "❌ **Автоприём заявок:** ВЫКЛЮЧЕН.";
    public const string AutoApproveWasDisabled = "✅ Автоприём заявок отключён.";

    public const string WriteNewLabel = "Введите новую метку (одно слово не длиннее 15 символов, без #$!().)";

    private const string AdminInfoTemplate = "📊 Статистика для {0}:\n\n" +
                                     "💸 Сумма закупов: {1} руб.\n" +
                                     "📈 Средняя цена закупа: {2} руб.\n" +
                                     "📅 Добавлен: {3}\n" +
                                     "🔢 Количество закупов: {4}\n";

    public static string AdminInfo(string adminKey, decimal totalSum, decimal averagePrice, string addDateFormatted,
        long zakupsCount)
    {
        return string.Format(AdminInfoTemplate, adminKey, totalSum, averagePrice, addDateFormatted, zakupsCount);
    }

    public const string BotIsNotAdminError =
        "Бот не администратор в этом канале. Добавьте его как администратора и попробуйте ещё раз.";

    public const string BotIsNotMemberError =
        "Бот не участник этого канала. Убедитесь, что добавили меня в качестве администратора и попробуйте ещё раз.";

    public const string MessageForwardedFromBotError =
        "Вы переслали сообщение от бота. Пожалуйста, перешлите сообщение из канала.";

    public const string ForwardPostFromChannelRequest = "Перешлите сюда пост из канала, куда вы меня добавили";
    public const string CantSendFileError = "Извините, произошла ошибка при отправке файла. Пожалуйста, обратитесь к администратору.";
    public const string CurrentSubscribers = "🔥 Текущие подписчики:";
    public const string UnSubscribedMembers = "❌ Отписавшиеся пользователи:";
    public const string NoSubscribeData = "Нет данных о вступивших и отписавшихся пользователях.";
    
    public static readonly HelpMessageTemplate Help = new HelpMessageTemplate();

    private const string ZakupStatistc = "📅 *Дата размещения:* {0}" +
                                        "\n" +
                                        "💼 *Платформа:* {1} " +
                                        "\n" +
                                        "💰 *Цена:* {2} руб\\." +
                                        "\n" +
                                        "\n" +
                                        "👥 *Всего подписчиков:* {3}" +
                                        "\n" +
                                        "📌 *Оставшихся в канале:* {4}" +
                                        "\n" +
                                        "💸 *Цена за подписчика:* {5}" +
                                        "\n" +
                                        "💎 *Цена за оставшегося:* {6}" +
                                        "\n" +
                                        "🛒 *Количество клиентов:* {7}" +
                                        "\n" +
                                        "💬 *Количество комментаторов:* {8}";

    public static string ZakupStatisticMessage(PlacementStatisticDTO data)
    {
        var costPerSubscriber = data.TotalSubscribers > 0 
            ? data.Price / data.TotalSubscribers 
            : 0;
        var costPerRemainingSubscriber = data.RemainingSubscribers > 0 
            ? data.Price / data.RemainingSubscribers
            : 0;
        
        return string.Format(ZakupStatistc,
            EscapeMarkdownV2(data.PlaceDate.ToString("dd.MM.yyyy")),
            EscapeMarkdownV2(data.Platform),
            EscapeMarkdownV2(data.Price.ToString(CultureInfo.InvariantCulture)),
            EscapeMarkdownV2(data.TotalSubscribers.ToString()),
            EscapeMarkdownV2(data.RemainingSubscribers.ToString()),
            EscapeMarkdownV2(costPerSubscriber.ToString(CultureInfo.InvariantCulture)),
            EscapeMarkdownV2(costPerRemainingSubscriber.ToString(CultureInfo.InvariantCulture)),
            EscapeMarkdownV2(data.ClientsCount.ToString()),
            EscapeMarkdownV2(data.CommentersCount.ToString())
            );
    }
    
    private static string EscapeMarkdownV2(string text)
    {
        if (text == null) return "";
        
        // Сначала экранируем обратные слеши, чтобы при замене других символов они не потерялись
        text = text.Replace("\\", "\\\\");
        
        var charactersToEscape = new char[] { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
        foreach (var character in charactersToEscape)
        {
            text = text.Replace(character.ToString(), "\\" + character);
        }
        return text;
    }

    public static string UserMuted(long userId, long feedbacksCount) =>
        $"Пользователь [{userId}] замучен! Удалено {feedbacksCount} отзывов.";
}

public  class HelpMessageTemplate
{
    public const string ChooseQuestion = "Выберите интересующий вас вопрос из списка ниже:";

    public const string ForWhatBot = "Для чего бот?";

    public const string ForWhatBotResponse =
        "👋Привет, бот ещё *проходит обкатку*, поэтому каждое замечание по работе или пожелание для нас на вес золота!\n" +
        "\n" +
        "\"Вот правда: что-то не понимаете или не работает — *напишите на @gandalfTG*" +
        "\n" +
        "Пару минут вашего времени поможет сделать сервис лучше. *Обещаю, если нюанс важный — приступим к доработке сразу же.* Если нужно будет — созвонимся с вами и всё расскажем, бесплатно!" +
        "\n" +
        "Также вы можете найти описание функций в новостном канале сервиса — https://t.me/easyzakup";
    
    
    public const string HowToCreateZakup = "Как создать размещение?";
    public const string HowToCreateZakupResponse = "⚠️ Раздел находится на технических работах. Весь функционал описан тут https://t.me/easyzakup";

    public const string HowToTrackResult = "Как отслеживать результат?";
    public const string HowToTrackResultResponse = "⚠️ Раздел находится на технических работах. Весь функционал описан тут https://t.me/easyzakup";

    public const string HowToTrackLead = "Отслеживание лидов";
    public const string HowToTrackLeadResponse = "⚠️ Раздел находится на технических работах. Весь функционал описан тут https://t.me/easyzakup";
    public const string HelpAnOffer = "Публичная оферта";
    public const string AnotherQuestion = "Другой вопрос";
    public const string AnotherQuestionResponse = "Напишите на @gandalftg и мы ответим на все ваши вопросы";
    
   
    //Важно соблюдать индексы вопросов и ответов или сделать класс вопрос-ответ и собрать в один массив
    public readonly List<string> Questions = new List<string>{ForWhatBot, HowToCreateZakup, HowToTrackResult, HowToTrackLead, AnotherQuestion};
    public readonly List<string> Responses = new List<string>{ForWhatBotResponse, HowToCreateZakupResponse, HowToTrackResultResponse, HowToTrackLeadResponse, AnotherQuestionResponse};
}