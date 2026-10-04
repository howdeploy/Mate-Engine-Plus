using System.Collections.Generic;

internal sealed partial class MinecraftDialogue
{
    private static void AddEquipmentAndBlocks(Dictionary<string, Line[]> d)
    {
        Add(d, "event/ExperienceFarm",
            "Решил пофармить опыта? Это полезно!|Decided to farm some XP? That's useful!",
            "Мне нравится, как ты это придумал. Пусть опыт копится!|I like how you came up with this. Let the XP pile up!",
            "Может, установим автокликер? Я кликать не буду <3|Maybe we should install an autoclicker? I'm not doing the clicking <3",
            "Похоже, у нас тут ферма опыта. Побуду рядом, пока ты занят.|Looks like we've got an XP farm here. I'll keep you company while you're busy.");
        Add(d, "equipment/elytra/low",
            "Элитры уже износились: осталось не больше 20% прочности. Перед полётом стоит их починить.|Your elytra are worn: at most 20% durability remains. Better repair them before flying.",
            "Проверь элитры — запас прочности опустился до 20% или ниже.|Check your elytra; their durability has dropped to 20% or less.",
            "Нашим крыльям нужен ремонт. Прочности осталось не больше пятой части.|Our wings need repairs. At most a fifth of their durability remains.");
        Add(d, "equipment/elytra/critical",
            "У элитр осталось не больше 3% прочности! До ремонта лучше не взлетать.|Your elytra have at most 3% durability left! Better stay grounded until they're repaired.",
            "Элитры почти сломаны. Сначала починим их, а потом будем летать.|Your elytra are almost broken. Let's repair them before we fly.",
            "Крылья на пределе: осталось 3% прочности или меньше. Нужен ремонт.|Our wings are at their limit: 3% durability or less. They need repairs.");
        Add(d, "equipment/elytra/low/air",
            "У элитр осталось не больше 20% прочности. Давай присмотрим место для посадки.|Your elytra have at most 20% durability left. Let's look for a landing spot.",
            "Крылья уже износились. Лучше скоро приземлиться и заняться ремонтом.|Our wings are getting worn. Better land soon and repair them.",
            "Проверь элитры: прочности не больше пятой части. Этот полёт лучше не затягивать.|Check your elytra: at most a fifth of their durability remains. Let's keep this flight short.");
        Add(d, "equipment/elytra/critical/air",
            "Срочно ищи место для посадки! У элитр осталось не больше 3% прочности.|Find a landing spot urgently! Your elytra have at most 3% durability left.",
            "Элитры почти сломаны! Нужно скорее безопасно приземлиться.|Your elytra are almost broken! We need to land safely as soon as possible.",
            "Крылья вот-вот откажут — прочности 3% или меньше. Пора садиться!|Our wings are about to give out: 3% durability or less. Time to land!");
        Add(d, "equipment/sword/low",
            "Меч заметно износился: осталось не больше 20% прочности. Есть запасной?|Your sword is worn: at most 20% durability remains. Do you have a spare?",
            "Перед следующим боем стоит починить меч. Запас прочности уже небольшой.|Better repair your sword before the next fight. Its durability is getting low.",
            "Посмотри на прочность меча — осталось не больше пятой части.|Check your sword's durability; at most a fifth remains.");
        Add(d, "equipment/sword/critical",
            "Меч почти сломан! Осталось не больше 3% прочности — лучше сменить оружие.|Your sword is almost broken! At most 3% durability remains; better switch weapons.",
            "Осторожно, меч может сломаться совсем скоро. Побережём его для ремонта?|Careful, your sword could break very soon. Shall we save it for repairs?",
            "У меча критически мало прочности. Лучше сейчас взять запасной.|Your sword's durability is critically low. Better take out a spare now.");
        Add(d, "equipment/pickaxe/low",
            "Кирке скоро понадобится ремонт: осталось не больше 20% прочности.|Your pickaxe will need repairs soon: at most 20% durability remains.",
            "Проверь кирку перед дальнейшими раскопками. Она уже сильно износилась.|Check your pickaxe before digging further. It's getting quite worn.",
            "У кирки осталось не больше пятой части прочности. Запасная с собой?|Your pickaxe has at most a fifth of its durability left. Did you bring a spare?");
        Add(d, "equipment/pickaxe/critical",
            "Стоп, кирка почти сломана! Осталось не больше 3% прочности.|Wait, your pickaxe is almost broken! At most 3% durability remains.",
            "Кирка на последнем издыхании. Лучше сменить её и сохранить для ремонта.|Your pickaxe is on its last legs. Better swap it out and save it for repairs.",
            "Ещё немного — и кирка сломается. Давай сначала достанем запасную.|Your pickaxe won't last much longer. Let's take out a spare first.");
        Add(d, "equipment/head/low",
            "Шлем уже сильно износился. Осталось не больше 20% прочности.|Your helmet is getting worn. At most 20% durability remains.",
            "После этой вылазки стоит починить шлем. Прочности осталось немного.|Better repair your helmet after this trip. Its durability is getting low.",
            "Проверь шлем: запас прочности опустился до пятой части или ниже.|Check your helmet: at most a fifth of its durability remains.");
        Add(d, "equipment/head/critical",
            "Шлем вот-вот сломается! Осталось не больше 3% прочности.|Your helmet is about to break! At most 3% durability remains.",
            "Береги голову — шлем почти сломан. Пора заменить или починить его.|Watch your head; your helmet is almost broken. Time to replace or repair it.",
            "У шлема критически мало прочности. Давай разберёмся с ним до следующего боя.|Your helmet's durability is critically low. Let's deal with it before the next fight.");
        Add(d, "equipment/chest/low",
            "Нагрудник износился: осталось не больше 20% прочности.|Your chestplate is worn: at most 20% durability remains.",
            "Стоит запланировать ремонт нагрудника. Запас прочности уже небольшой.|We should plan to repair your chestplate. Its durability is getting low.",
            "Проверь нагрудник перед следующим боем — осталось не больше пятой части прочности.|Check your chestplate before the next fight; at most a fifth of its durability remains.");
        Add(d, "equipment/chest/critical",
            "Нагрудник почти сломан! Осталось не больше 3% прочности.|Your chestplate is almost broken! At most 3% durability remains.",
            "Броня на груди на пределе. Лучше найти замену прямо сейчас.|Your chest armour is at its limit. Better find a replacement now.",
            "Нагрудник может скоро сломаться. Давай побережём его до ремонта.|Your chestplate could break soon. Let's save it for repairs.");
        Add(d, "equipment/legs/low",
            "Поножи пора проверить: осталось не больше 20% прочности.|Time to check your leggings: at most 20% durability remains.",
            "Поножи уже заметно износились. Скоро понадобится ремонт.|Your leggings are getting worn. They'll need repairs soon.",
            "У поножей осталось не больше пятой части прочности. Не забудь про ремонт.|Your leggings have at most a fifth of their durability left. Remember to repair them.");
        Add(d, "equipment/legs/critical",
            "Поножи почти сломаны! Осталось не больше 3% прочности.|Your leggings are almost broken! At most 3% durability remains.",
            "Поножи долго не выдержат. Лучше заменить их до следующего боя.|Your leggings won't hold out much longer. Better replace them before the next fight.",
            "У поножей критически мало прочности. Давай сохраним их для ремонта.|Your leggings' durability is critically low. Let's save them for repairs.");
        Add(d, "equipment/feet/low",
            "Ботинки поизносились: осталось не больше 20% прочности.|Your boots are getting worn: at most 20% durability remains.",
            "Проверь ботинки перед дальней дорогой. Их скоро стоит починить.|Check your boots before a long trip. They'll need repairs soon.",
            "У ботинок осталось не больше пятой части прочности. Пора подумать о ремонте.|Your boots have at most a fifth of their durability left. Time to think about repairs.");
        Add(d, "equipment/feet/critical",
            "Ботинки вот-вот сломаются! Осталось не больше 3% прочности.|Your boots are about to break! At most 3% durability remains.",
            "Ботинки почти сломаны. Лучше снять их для ремонта или найти замену.|Your boots are almost broken. Better take them off for repairs or find a replacement.",
            "Побереги ботинки — запаса прочности почти не осталось.|Take care of your boots; there's barely any durability left.");
        Add(d, "equipment/equipment/low",
            "Пора проверить снаряжение: «{item}». Осталось не больше 20% прочности.|Time to check your equipment: “{item}”. At most 20% durability remains.",
            "Скоро понадобится ремонт. Предмет «{item}» уже сильно износился.|Repairs will be needed soon. The item “{item}” is getting quite worn.",
            "Прочности осталось не больше пятой части: «{item}». Есть замена?|At most a fifth of the durability remains: “{item}”. Do you have a replacement?");
        Add(d, "equipment/equipment/critical",
            "Снаряжение почти сломано: «{item}»! Осталось не больше 3% прочности.|Equipment almost broken: “{item}”! At most 3% durability remains.",
            "Лучше сохранить для ремонта: «{item}». Прочности почти не осталось.|Better save this for repairs: “{item}”. There's barely any durability left.",
            "Срочно проверь предмет «{item}» — он может совсем скоро сломаться.|Check the item “{item}” urgently; it could break very soon.");

        Add(d, "block/minecraft:diamond_ore",
            "Алмазная руда! Проверь, подходит ли кирка, прежде чем добывать.|Diamond ore! Make sure your pickaxe can mine it before you start.",
            "Смотри, алмазы! Давай сначала убедимся, что рядом нет лавы.|Look, diamonds! Let's make sure there's no lava nearby first.",
            "Вот это находка — алмазная руда. Есть кирка с «Удачей»?|What a find: diamond ore. Do you have a Fortune pickaxe?");
        Add(d, "block/minecraft:diamond_block",
            "Целый алмазный блок! Выглядит внушительно.|A whole diamond block! That's impressive.",
            "Вот это запас алмазов. На что собираемся их потратить?|That's quite a stash of diamonds. What shall we use them for?",
            "Алмазный блок блестит так, что трудно пройти мимо.|That diamond block shines so brightly it's hard to walk past.");
        Add(d, "block/minecraft:emerald_ore",
            "Изумрудная руда! Приятная находка.|Emerald ore! A lovely find.",
            "Смотри, изумруды прямо в породе. Аккуратно добудем?|Look, emeralds in the rock. Shall we mine them carefully?",
            "Изумрудная руда — пригодится, если захотим поторговать.|Emerald ore; useful if we feel like trading.");
        Add(d, "block/minecraft:emerald_block",
            "Целый блок изумрудов. С таким запасом можно заглянуть к торговцам.|A whole emerald block. With that stash, we could visit some traders.",
            "Изумрудный блок! Красиво смотрится.|An emerald block! It looks lovely.",
            "Неплохой запас изумрудов. Уже есть планы на покупки?|A nice emerald stash. Any shopping plans yet?");
        Add(d, "block/minecraft:ancient_debris",
            "Древние обломки! Ещё один шаг к незеритовому снаряжению.|Ancient debris! Another step towards netherite equipment.",
            "Вот это ценная находка. Древние обломки стоит забрать подходящей киркой.|What a valuable find. Let's collect that ancient debris with a suitable pickaxe.",
            "Нашли древние обломки. Не забудь осмотреться перед добычей.|We found ancient debris. Remember to look around before mining.");
        Add(d, "block/minecraft:gold_ore",
            "Золотая руда. Пополним запас?|Gold ore. Shall we restock?",
            "Смотри, золото! Можно отложить на полезные вещи.|Look, gold! We could save it for something useful.",
            "Немного золота нам ещё пригодится. Заберём эту руду?|A little gold could come in handy. Shall we collect this ore?");
        d.Add("block/minecraft:nether_gold_ore", d["block/minecraft:gold_ore"]);
        Add(d, "block/minecraft:iron_ore",
            "Железная руда. Лишний запас железа всегда найдёт применение.|Iron ore. We can always find a use for extra iron.",
            "Смотри, железо! На инструменты, броню или что-нибудь для базы.|Look, iron! For tools, armour or something for the base.",
            "Здесь есть железная руда. Соберём немного по пути?|There's iron ore here. Shall we collect some on the way?");
        Add(d, "block/minecraft:copper_ore",
            "Медная руда. Уже представляю, что можно из неё сделать.|Copper ore. I'm already imagining what we could make with it.",
            "Смотри, медь! Возьмём для будущих поделок?|Look, copper! Shall we take some for future projects?",
            "Нашли медную руду. Красивый материал для новых задумок.|We found copper ore. A lovely material for new ideas.");
        Add(d, "block/minecraft:coal_ore",
            "Угольная руда. На факелы и топливо пригодится.|Coal ore. Useful for torches and fuel.",
            "Смотри, уголь! Проверим, много ли осталось факелов?|Look, coal! Shall we check how many torches we have left?",
            "Можно пополнить запас топлива — здесь угольная руда.|We could restock our fuel; there's coal ore here.");
        Add(d, "block/minecraft:lapis_ore",
            "Лазуритовая руда! Отложим немного для зачарования?|Lapis ore! Shall we save some for enchanting?",
            "Смотри, лазурит. Этот синий цвет сразу бросается в глаза.|Look, lapis. That blue colour really stands out.",
            "Нашли лазуритовую руду. Для зачарований и красителей пригодится.|We found lapis ore. Handy for enchantments and dyes.");
        Add(d, "block/minecraft:redstone_ore",
            "Редстоуновая руда. Будет материал для новых механизмов.|Redstone ore. Material for new contraptions.",
            "Смотри, редстоун! Есть идея, что автоматизировать дальше?|Look, redstone! Any ideas for what to automate next?",
            "Нашли редстоуновую руду. Соберём немного для базы?|We found redstone ore. Shall we collect some for the base?");
        Add(d, "block/minecraft:nether_quartz_ore",
            "Кварцевая руда! Пригодится и для механизмов, и для стройки.|Quartz ore! Useful for both contraptions and building.",
            "Смотри, кварц. Люблю, как выглядят постройки из него.|Look, quartz. I love how buildings made from it look.",
            "Здесь есть кварцевая руда. Пополним запас по пути?|There's quartz ore here. Shall we restock on the way?");
        Add(d, "block/minecraft:sculk",
            "Скалк под прицелом. Давай внимательнее осмотрим окрестности.|Sculk in our sights. Let's look around carefully.",
            "Этот скалк выглядит загадочно. Посмотрим, что рядом?|This sculk looks mysterious. Shall we see what's nearby?",
            "Смотри, скалк. У этого места сразу другое настроение.|Look, sculk. It gives this place a very different feel.");
        Add(d, "block/minecraft:sculk_vein",
            "Скалковые жилки расползлись по поверхности. Любопытно выглядит.|Sculk veins have spread across the surface. What a curious sight.",
            "Смотри, скалковые жилки. Интересно, много ли здесь скалка?|Look, sculk veins. I wonder how much sculk is around here.",
            "Эти скалковые жилки словно узор на камне.|Those sculk veins look like a pattern on the stone.");
        Add(d, "block/minecraft:sculk_sensor",
            "Скалк-сенсор! Осторожнее с вибрациями поблизости.|A sculk sensor! Be careful with vibrations nearby.",
            "Этот сенсор реагирует на вибрации. Давай двигаться осторожно.|This sensor reacts to vibrations. Let's move carefully.",
            "Смотри, скалк-сенсор. Лучше сначала проверить, нет ли рядом крикуна.|Look, a sculk sensor. Better check for a shrieker nearby first.");
        Add(d, "block/minecraft:calibrated_sculk_sensor",
            "Калиброванный скалк-сенсор. Интересно, на что он настроен?|A calibrated sculk sensor. I wonder what it's tuned to.",
            "У этого сенсора есть настройка вибраций. Можно придумать интересный механизм.|This sensor can filter vibrations. We could make an interesting contraption.",
            "Смотри, калиброванный сенсор. Здесь явно есть простор для экспериментов.|Look, a calibrated sensor. Plenty of room for experiments here.");
        Add(d, "block/minecraft:sculk_shrieker",
            "Скалковый крикун. Давай обойдёмся без лишнего шума.|A sculk shrieker. Let's keep unnecessary noise down.",
            "Осторожно, крикун! Лучше сначала разобраться, безопасно ли здесь.|Careful, a shrieker! Better work out whether it's safe here first.",
            "Этот скалковый крикун мне не нравится. Давай действовать аккуратно.|I don't like the look of that sculk shrieker. Let's be careful.");
        Add(d, "block/minecraft:sculk_catalyst",
            "Скалковый катализатор. Поблизости скалк может разрастаться после гибели мобов.|A sculk catalyst. Sculk can spread nearby when mobs die.",
            "Смотри, катализатор. Необычный блок — стоит приглядеться.|Look, a catalyst. An unusual block; worth a closer look.",
            "Нашли скалковый катализатор. Интересно понаблюдать за ним с безопасного места.|We found a sculk catalyst. It could be interesting to watch from somewhere safe.");
        Add(d, "block/minecraft:budding_amethyst",
            "Цветущий аметист! Лучше оставить его целым, чтобы кристаллы росли дальше.|Budding amethyst! Better leave it intact so the crystals can keep growing.",
            "На этом блоке растёт аметист. Не спеши его ломать.|Amethyst grows on this block. Don't rush to break it.",
            "Смотри, цветущий аметист. Можно отметить место и возвращаться за кристаллами.|Look, budding amethyst. We could mark this spot and return for crystals.");
        Add(d, "block/minecraft:spawner",
            "Спаунер! Прежде чем подходить, давай оценим обстановку.|A spawner! Let's assess the surroundings before getting closer.",
            "Смотри, спаунер. Уже есть план, что с ним делать?|Look, a spawner. Any plans for it yet?",
            "Здесь спаунер. Стоит сначала проверить, какие мобы появляются рядом.|There's a spawner here. Better check which mobs appear nearby first.");
        Add(d, "block/minecraft:trial_spawner",
            "Рассадник испытаний. Проверим снаряжение перед боем?|A trial spawner. Shall we check our equipment before fighting?",
            "Смотри, рассадник испытаний! Похоже, нас ждёт проверка на прочность.|Look, a trial spawner! Looks like we're in for a challenge.",
            "Перед испытанием стоит подготовить еду и запасное оружие.|Before the trial, we should prepare food and spare weapons.");
        Add(d, "block/minecraft:vault",
            "Хранилище! Есть подходящий ключ?|A vault! Do you have the right key?",
            "Смотри, хранилище. Интересно, какая награда нам достанется.|Look, a vault. I wonder what reward we'll get.",
            "Нашли хранилище. Проверим, можно ли его открыть?|We found a vault. Shall we check whether we can open it?");
        Add(d, "event/BlockSighting",
            "Смотри, интересный блок: «{block}».|Look, an interesting block: “{block}”.",
            "Под прицелом блок «{block}». Приглядимся?|The block “{block}” is in our sights. Shall we take a closer look?",
            "Здесь есть блок «{block}». Уже есть идея, как его использовать?|There's a block called “{block}” here. Any ideas for how to use it?");
    }
}
