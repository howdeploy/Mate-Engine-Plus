using System;
using System.Collections.Generic;

// Authored by howdeploy for Mate Engine Plus. Dialogue is local; no generation service is used.
internal sealed partial class MinecraftDialogue
{
    private struct Line
    {
        public string ru;
        public string en;
    }

    private static readonly Dictionary<string, Line[]> Catalog = BuildCatalog();
    private readonly Dictionary<string, int> previous = new Dictionary<string, int>();
    private readonly Random random = new Random();

    public string Pick(AvatarMinecraftMessages.McEventType type, AvatarMinecraftMessages.ProxEvent context, string fallbackLanguage)
    {
        string language = string.IsNullOrEmpty(context.language) ? fallbackLanguage : context.language;
        bool russian = (language ?? "en").StartsWith("ru", StringComparison.OrdinalIgnoreCase);
        string key = "event/" + type;
        if (type == AvatarMinecraftMessages.McEventType.ChestContents)
        {
            key = "chest/mixed";
            string reactionKey = "chest/" + context.chest_reaction;
            if (Catalog.ContainsKey(reactionKey)) key = reactionKey;
            if (context.chest_reaction == "messy" && context.chest_context == "player_placed") key = "chest/messy/player";
            if (context.chest_reaction == "mixed")
            {
                string contextKey = "chest/context/" + context.chest_context;
                if (Catalog.ContainsKey(contextKey)) key = contextKey;
            }
        }
        else if (type == AvatarMinecraftMessages.McEventType.StructureSighting)
        {
            string structureKey = "structure/" + context.structure_hint;
            if (Catalog.ContainsKey(structureKey)) key = structureKey;
        }
        else if (type == AvatarMinecraftMessages.McEventType.FarmActivity)
        {
            string activityKey = "activity/" + context.activity;
            if (Catalog.ContainsKey(activityKey)) key = activityKey;
        }
        else if (type == AvatarMinecraftMessages.McEventType.EquipmentDurability)
        {
            string severity = context.durability_threshold == 3 ? "critical" : "low";
            key = "equipment/equipment/" + severity;
            string itemKey = "equipment/" + context.equipment_kind + "/" + severity;
            if (Catalog.ContainsKey(itemKey)) key = itemKey;
            if (context.equipment_kind == "elytra" && context.movement == "elytra") key += "/air";
        }
        else if (type == AvatarMinecraftMessages.McEventType.BlockSighting)
        {
            string blockKey = "block/" + (context.block_id ?? "").Replace("minecraft:deepslate_", "minecraft:");
            if (Catalog.ContainsKey(blockKey)) key = blockKey;
        }
        else if (type == AvatarMinecraftMessages.McEventType.BiomeDiscovery && !string.IsNullOrEmpty(context.biome_id))
        {
            string id = context.biome_id;
            bool flying = !string.IsNullOrEmpty(context.movement) && context.movement != "ground";
            bool insideFlight = id == "minecraft:dripstone_caves" || id == "minecraft:lush_caves"
                || id == "minecraft:sulfur_caves" || id == "minecraft:deep_dark"
                || context.dimension == "minecraft:the_nether" || context.dimension == "minecraft:the_end"
                || id == "minecraft:the_void";
            if (context.biome_view == "below") key = "event/BiomeBelow";
            string biomeKey = ((context.biome_view == "below" || flying && insideFlight) ? "air/" : "biome/") + id;
            if (Catalog.ContainsKey(biomeKey)) key = biomeKey;
        }
        else if (type == AvatarMinecraftMessages.McEventType.Entity && !string.IsNullOrEmpty(context.id))
        {
            string mobKey = "mob/" + context.id;
            if (!string.IsNullOrEmpty(context.mob_state) && Catalog.ContainsKey(mobKey + "/" + context.mob_state))
                key = mobKey + "/" + context.mob_state;
            else if (Catalog.ContainsKey(mobKey)) key = mobKey;
        }

        if (!Catalog.TryGetValue(key, out Line[] lines) || lines.Length == 0) return "";
        string memoryKey = key + (russian ? "/ru" : "/en");
        int index;
        if (lines.Length > 1 && previous.TryGetValue(memoryKey, out int last))
        {
            index = random.Next(lines.Length - 1);
            if (index >= last) index++;
        }
        else index = random.Next(lines.Length);
        previous[memoryKey] = index;
        string text = russian ? lines[index].ru : lines[index].en;
        return text.Replace("{entity}", DisplayName(context.name, russian ? "незнакомое существо" : "an unfamiliar creature"))
            .Replace("{biome}", DisplayName(context.biome, russian ? "неизвестная местность" : "unfamiliar terrain"))
            .Replace("{item}", DisplayName(context.item_name, russian ? "снаряжение" : "equipment"))
            .Replace("{block}", DisplayName(context.block_name, russian ? "неизвестный блок" : "an unfamiliar block"));
    }

    private static string DisplayName(string name, string fallback)
    {
        if (string.IsNullOrWhiteSpace(name)) return fallback;
        return name.Substring(0, Math.Min(name.Length, 120)).Replace('<', '‹').Replace('>', '›');
    }

    private static Dictionary<string, Line[]> BuildCatalog()
    {
        var catalog = new Dictionary<string, Line[]>(StringComparer.Ordinal);
        AddEvents(catalog);
        AddBiomes(catalog);
        AddMobs(catalog);
        AddEquipmentAndBlocks(catalog);
        AddStructuresAndActivities(catalog);
        AddChests(catalog);
        return catalog;
    }

    // Each row contains the complete Russian line and its English counterpart.
    private static void Add(Dictionary<string, Line[]> catalog, string key, params string[] pairs)
    {
        var lines = new Line[pairs.Length];
        for (int i = 0; i < pairs.Length; i++)
        {
            int separator = pairs[i].IndexOf('|');
            lines[i] = new Line { ru = pairs[i].Substring(0, separator), en = pairs[i].Substring(separator + 1) };
        }
        catalog.Add(key, lines);
    }

    private static void AddEvents(Dictionary<string, Line[]> d)
    {
        Add(d, "event/DayStart",
            "Рассвет! Что будем делать сегодня?|Daybreak! What shall we do today?",
            "Начинается новый день. Может, отправимся на разведку?|A new day is beginning. Shall we go exploring?",
            "Светает. Самое время продолжить наши приключения.|It's getting light. Time to continue our adventures.",
            "Доброе утро! Проверим припасы перед дорогой?|Good morning! Shall we check our supplies before heading out?",
            "Уже утро. У меня хорошее предчувствие насчёт этого дня.|It's morning already. I have a good feeling about today.",
            "Новый день — новые планы. Строим или исследуем?|A new day, new plans. Building or exploring?",
            "Солнце встаёт. Давай найдём сегодня что-нибудь интересное.|The sun is rising. Let's find something interesting today.",
            "С добрым утром! Я готова составить тебе компанию.|Good morning! I'm ready to keep you company.");
        Add(d, "event/NightStart",
            "Темнеет. Держим факелы под рукой.|It's getting dark. Keep the torches handy.",
            "Наступает ночь. Найдём укрытие или продолжим путь?|Night is falling. Shall we find shelter or keep going?",
            "Уже ночь. Давай внимательнее смотреть по сторонам.|It's night already. Let's keep an eye on our surroundings.",
            "Солнце заходит. Если рядом кровать, можно передохнуть.|The sun is setting. If there's a bed nearby, we could get some rest.");
        Add(d, "event/LowHealth",
            "Осталось мало здоровья. Давай сначала найдём безопасное место.|You're low on health. Let's find somewhere safe first.",
            "Побереги себя! Сейчас лучше отступить и восстановиться.|Take care! It might be best to retreat and recover.",
            "Здоровья совсем немного. Есть что-нибудь для лечения?|You're running low on health. Do you have anything to heal with?",
            "Давай без лишнего риска, пока не восстановим здоровье.|Let's avoid unnecessary risks until your health is back.");
        Add(d, "event/LowHunger",
            "Пора перекусить. У нас есть еда с собой?|Time for a snack. Did we bring any food?",
            "Шкала голода опустилась. Найдём минутку поесть?|Your food bar is getting low. Shall we take a moment to eat?",
            "Не забудь поесть перед следующим приключением.|Don't forget to eat before the next adventure.",
            "Давай пополним силы. На пустой желудок далеко не уйдёшь.|Let's get your energy back. You won't get far on an empty stomach.",
            "Кажется, пора открыть наши съестные припасы.|Looks like it's time to open our food supplies.");
        Add(d, "event/Death",
            "Ох, не получилось… Я рядом. Давай немного переведём дух.|Oh, that didn't work out… I'm here. Let's take a breath.",
            "Вот незадача. Не спеши, сначала решим, что делать дальше.|That's unfortunate. No rush; let's work out what to do next.",
            "Мне жаль. Такое приключение хотелось бы закончить иначе.|I'm sorry. I wish that adventure had ended differently.",
            "Тяжёлый момент. Давай сделаем паузу, если она нужна.|That was rough. Let's take a break if you need one.");
        Add(d, "event/RainStart",
            "Пошёл дождь. Найдём крышу или прогуляемся под ним?|It's starting to rain. Shall we find a roof or walk in it?",
            "Слышишь дождь? По-своему уютно.|Hear the rain? It's rather cosy.",
            "Кажется, погода решила освежить наше путешествие.|Looks like the weather wants to freshen up our journey.");
        Add(d, "event/Drowning",
            "Воздуха осталось немного. Пора всплывать!|Your air is getting low. Time to surface!",
            "Проверь запас воздуха! Лучше сейчас сделать вдох.|Check your air! Better get a breath now.",
            "Не задерживайся под водой — воздух заканчивается.|Don't stay underwater too long; your air is running out.",
            "Давай сначала пополним воздух, а потом продолжим нырять.|Let's get some air before we keep diving.");
        Add(d, "event/Sleep",
            "Спокойной ночи. Приключения подождут до утра.|Good night. The adventures can wait until morning.",
            "Устраивайся поудобнее. Ты заслуживаешь отдыха.|Get comfortable. You deserve some rest.",
            "Пора отдохнуть. Завтра продолжим наше путешествие.|Time to rest. We'll continue our journey tomorrow.",
            "Сладких снов! Я тоже немного передохну.|Sweet dreams! I'll take a little rest too.");
        Add(d, "event/Crafting",
            "Готово! Куда пустим нашу новую вещь?|Done! What will we use our new item for?",
            "Ещё одна полезная вещь в запасе.|One more useful item to keep handy.",
            "Мне нравится наблюдать, как из материалов получается что-то новое.|I like watching materials turn into something new.",
            "Отлично, ещё одна вещь готова!|Nice, another item is ready!");
        Add(d, "event/Eat",
            "Небольшой перекус — и можно продолжать.|A little snack, and we're ready to carry on.",
            "Приятного аппетита! Надеюсь, было вкусно.|Enjoy! I hope that tasted good.",
            "Хорошо, что мы не забыли про еду.|I'm glad we remembered to eat.",
            "Вот и подкрепились. Куда теперь?|That's our snack sorted. Where to next?");
        Add(d, "event/KillConfirm",
            "Одним противником меньше. Давай осмотримся.|One less opponent. Let's look around.",
            "Справились! Только не теряй бдительность.|We did it! Just stay alert.",
            "Хорошая работа. Можно немного перевести дух.|Good job. We can take a quick breath.",
            "Этот бой позади. Проверим, что осталось вокруг?|That fight is over. Shall we check our surroundings?");
        Add(d, "event/Entity",
            "Смотри, рядом: {entity}.|Look, nearby: {entity}.",
            "Новая встреча — {entity}. Поглядим внимательнее.|A new encounter: {entity}. Let's take a closer look.",
            "Здесь есть {entity}. Будем внимательны.|There's {entity} here. Let's pay attention.",
            "Замечено поблизости: {entity}.|Spotted nearby: {entity}.",
            "Мы здесь не одни. Рядом: {entity}.|We're not alone here. Nearby: {entity}.");
        Add(d, "event/BiomeBelow",
            "Под нами биом «{biome}». Посмотрим на него с высоты!|The biome below is “{biome}”. Let's admire it from above!",
            "Пролетаем над новым биомом: «{biome}».|We're flying over a new biome: “{biome}”.",
            "Пейзаж под нами изменился. Внизу биом «{biome}».|The scenery below has changed. That biome is “{biome}”.",
            "С высоты открывается вид на биом «{biome}».|We have an aerial view of the biome “{biome}”.");
        Add(d, "event/BiomeDiscovery",
            "Новая местность: «{biome}». Осмотримся?|New terrain: “{biome}”. Shall we look around?",
            "На карте теперь другой биом: «{biome}».|We're in a different biome now: “{biome}”.",
            "Смена пейзажа! Здесь биом «{biome}».|A change of scenery! This biome is “{biome}”.",
            "Наш путь привёл в новый биом: «{biome}».|Our journey has brought us to a new biome: “{biome}”.");
    }
}
