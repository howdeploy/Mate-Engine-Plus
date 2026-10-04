using System.Collections.Generic;

internal sealed partial class MinecraftDialogue
{
    private static void AddChests(Dictionary<string, Line[]> d)
    {
        Add(d, "chest/empty",
            "Пусто. Зато есть место для будущих находок.|Empty. At least there's room for future finds.",
            "Здесь пока ничего нет. Двигаемся дальше?|There's nothing here right now. Shall we move on?",
            "Этот сундук пуст. Запомним, что здесь можно оставить припасы.|This chest is empty. We could leave some supplies here.",
            "Ни одной вещи. Иногда самое ценное в сундуке — свободное место.|Not a single item. Sometimes the most valuable thing in a chest is the space.");
        Add(d, "chest/full",
            "Сундук почти заполнен. Для новых находок скоро понадобится ещё один.|This chest is nearly full. We'll need another one for new finds soon.",
            "Свободных ячеек совсем немного. Может, подготовим ещё место для хранения?|There are very few empty slots. Shall we make some more storage space?",
            "Запасов много! Только места в этом сундуке почти не осталось.|That's a lot of supplies! There's hardly any room left in this chest.",
            "Кажется, сундук уже на пределе вместимости. Куда складываем следующую добычу?|Looks like this chest is almost at capacity. Where shall we put the next haul?");
        Add(d, "chest/valuables",
            "О, здесь есть ценные вещи! Давай не оставим их без внимания.|Oh, there are valuable items here! Let's not overlook them.",
            "В этом сундуке есть кое-что ценное. Проверим, что пригодится в дороге?|There's something valuable in this chest. Shall we see what might help on our travels?",
            "Среди содержимого есть настоящие сокровища. Хорошо, что заглянули.|There are some real treasures among these items. I'm glad we took a look.",
            "Вот это запас! Ценные вещи лучше держать там, где их легко найти.|Quite a stash! It's best to keep valuables somewhere easy to remember.");
        Add(d, "chest/stockpile",
            "Здесь хороший запас одних и тех же материалов. Готовимся к большому проекту?|There's a good stock of the same materials here. Preparing for a big project?",
            "Судя по содержимому, этот сундук выделили под конкретные ресурсы. Удобно.|Judging by the contents, this chest is dedicated to particular resources. Handy.",
            "Много похожих стопок. Похоже, запасы пополняются основательно.|Lots of similar stacks. Looks like some serious stocking up.",
            "Приятно, когда нужный ресурс собран в одном месте. Не придётся долго искать.|It's nice to have a resource all in one place. No need for a long search.");
        Add(d, "chest/mixed",
            "Посмотрим, что здесь есть. Может, найдётся что-нибудь для наших планов.|Let's see what's in here. Maybe there's something for our plans.",
            "Самое время сверить припасы с тем, что нам нужно.|A good time to compare these supplies with what we need.",
            "В этом сундуке что-то есть. Разберёмся, что стоит взять с собой?|There are some items in this chest. Shall we work out what to take with us?",
            "Заглянем в запасы. Вдруг нужная вещь уже здесь?|Let's check the supplies. Maybe the item we need is already here.");
        Add(d, "chest/messy",
            "Кажется, этот сундук пора разобрать: тут всё вперемешку, а часть стопок можно объединить.|Looks like this chest could use some sorting: it's all mixed up, and some stacks could be combined.",
            "В этом сундуке маленький склад всего на свете. Немного порядка освободит место.|This chest is a little warehouse of everything. Some sorting would free up space.",
            "Столько разных вещей! Если собрать неполные стопки вместе, искать станет проще.|So many different items! Combining the partial stacks would make things easier to find.",
            "Похоже на творческий беспорядок. Начнём с объединения одинаковых вещей?|Looks like creative chaos. Shall we start by combining matching items?");
        Add(d, "chest/messy/player",
            "В твоём сундуке, кажется, завёлся творческий беспорядок. Разложим вещи?|Looks like creative chaos has moved into your chest. Shall we sort things out?",
            "Ты сюда складываешь всё подряд? Несколько стопок можно объединить — станет просторнее.|Do you put a bit of everything in here? We could combine a few stacks to make more room.",
            "У тебя тут целый музей случайных находок. Может, немного приберёмся?|You've got a whole museum of random finds in here. Shall we tidy up a little?",
            "Если нужная вещь потерялась, я бы искала в этом сундуке. После небольшой уборки.|If something's gone missing, I'd look in this chest. After a little tidying up.");
        Add(d, "chest/context/player_placed",
            "Заглянем в сундук, который ты поставил. Что из запасов пригодится сейчас?|Let's check the chest you placed. Which supplies would be useful now?",
            "Проверим наши запасы. Хорошо, когда есть куда вернуться за нужными вещами.|Let's check our supplies. It's nice to have somewhere to return for what we need.",
            "Вот и пригодилось место для хранения. Посмотрим, что у нас под рукой.|That storage space came in handy. Let's see what we have available.",
            "Сундук уже не пустует. Что берём с собой, а что оставляем здесь?|The chest isn't empty anymore. What shall we take, and what shall we leave here?");
        Add(d, "chest/context/shipwreck",
            "Похоже, это сундук с затонувшего корабля. Посмотрим, что в нём осталось.|This looks like a chest from a shipwreck. Let's see what's left inside.",
            "Эти доски напоминают обломки корабля. Интересно, что лежит в сундуке?|Those planks look like parts of a wreck. I wonder what's in the chest.",
            "Если это корабельный сундук, у его вещей могла быть долгая история. Осмотрим содержимое?|If this is a ship's chest, its contents might have quite a history. Shall we take a look?",
            "Судя по тому, что мы видели вокруг, здесь мог быть корабль. Проверим находку.|Judging by what we saw around here, this could have been a ship. Let's check our find.");
        Add(d, "chest/context/dungeon",
            "Спаунер и замшелые стены рядом… Похоже, сундук из сокровищницы. Что внутри?|A spawner and mossy walls nearby… Looks like a dungeon chest. What's inside?",
            "Кажется, нашли сундук в сокровищнице. Посмотрим содержимое, не теряя бдительности.|Looks like we've found a dungeon chest. Let's check the contents and stay alert.",
            "Мы только что видели спаунер. Проверим сундук и не будем забывать об окружении.|We just saw a spawner. Let's check the chest and keep an eye on our surroundings.",
            "Это место похоже на сокровищницу. Любопытно, какие вещи здесь лежат.|This place looks like a dungeon. I'm curious about the items in here.");
        Add(d, "chest/context/structure",
            "Вокруг были интересные следы постройки. Посмотрим, что хранится в сундуке.|We saw some interesting signs of a structure nearby. Let's see what's in the chest.",
            "Похоже, исследование привело нас к сундуку. Проверим содержимое?|Looks like our exploring has led us to a chest. Shall we check the contents?",
            "Здесь есть что осмотреть, и сундук тоже заслуживает внимания.|There's plenty to look at here, and this chest deserves a look too.",
            "После того, что мы увидели вокруг, особенно любопытно заглянуть в этот сундук.|After what we saw nearby, I'm especially curious to look inside this chest.");
    }
}
