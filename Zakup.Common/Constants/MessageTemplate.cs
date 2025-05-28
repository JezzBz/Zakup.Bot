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

    public const string AddChannelAliasRequest = "Теперь напишите метку канала (любое одно слово)";

    public const string ChannelAliasStateNotification = "Метка должна быть одним словом и не длиннее 15 символов. Пожалуйста, введите снова.";

    public const string FirstPostDecline = "**Без проблем\\!** вы всегда сможете сделать это через меню канала";
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
    public const string WriteButtonText = "Введите текст для кнопки";
    
    public const string WriteAliasForPost = "Теперь придумайте метку для этого поста (слово или число для поиска и идентификации)";
    public const string PostTitleEmptyError = "Метка креатива не может быть пустой. Попробуйте снова.";
    public const string TitleTooLongError = "❌Слишком длинная метка (более 30 символов). Попробуйте снова, но короче.";
    public const string BadSymbolsError = "❌Метка содержит недопустимые символы.\n" +
                                            "Разрешены буквы (латиница, кириллица), цифры и пробелы.";
    
}