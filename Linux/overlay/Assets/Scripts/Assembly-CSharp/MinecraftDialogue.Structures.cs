using System.Collections.Generic;

internal sealed partial class MinecraftDialogue
{
    private static void AddStructuresAndActivities(Dictionary<string, Line[]> d)
    {
        Add(d, "activity/crops",
            "Собираем урожай? Приятно видеть результат своих трудов.|Harvesting crops? It's nice to see your hard work pay off.",
            "Запасы пополняются! Хорошее занятие между приключениями.|Our supplies are growing! A lovely task between adventures.",
            "Мне нравится этот спокойный ритм: собирать урожай и строить планы.|I like this peaceful rhythm: harvesting crops and making plans.",
            "Похоже, сегодня занимаемся хозяйством. Я составлю тебе компанию.|Looks like we're doing some farming today. I'll keep you company.");
        Add(d, "activity/wood",
            "Заготавливаем древесину? На будущие постройки пригодится.|Gathering wood? It'll come in handy for future builds.",
            "Запас дерева растёт. Уже придумал, что будем строить?|Our wood supply is growing. Any ideas for what to build?",
            "Рубка идёт полным ходом. Не забудь оставить саженцы для новых деревьев.|The logging is in full swing. Remember to save saplings for new trees.",
            "Мне нравится, когда материалы накапливаются заранее. Потом можно спокойно строить.|I like stocking up on materials ahead of time. Then we can build at our leisure.");
        Add(d, "structure/archaeology",
            "Смотри, подозрительный блок! Похоже, здесь можно устроить раскопки. Кисточка с собой?|Look, a suspicious block! Looks like we could do some archaeology here. Did you bring a brush?",
            "Здесь есть что исследовать кисточкой. Только не ломай этот блок киркой.|There's something here to investigate with a brush. Just don't break that block with a pickaxe.",
            "Кажется, нашли место для раскопок. Интересно, что сохранилось внутри?|Looks like we've found a dig site. I wonder what's preserved inside?",
            "Этот блок отличается от соседних. Давай аккуратно почистим его кисточкой.|This block looks different from its neighbours. Let's carefully brush it.");
        Add(d, "structure/trial_chambers",
            "Похоже, мы в камерах испытаний! Проверим, готовы ли мы к бою?|Looks like we're in trial chambers! Shall we check whether we're ready to fight?",
            "Похоже на камеры испытаний. Проверим еду и снаряжение перед дальнейшим осмотром.|This looks like trial chambers. Let's check our food and equipment before exploring further.",
            "Кажется, здесь нас ждут испытания и награды. Давай действовать без спешки.|Looks like challenges and rewards await here. Let's take our time.");
        Add(d, "structure/ancient_city",
            "Укреплённый глубинный сланец… Похоже, это древний город. Давай потише.|Reinforced deepslate… This looks like an ancient city. Let's keep quiet.",
            "Кажется, мы добрались до древнего города. Сначала осмотримся, не поднимая шума.|Looks like we've reached an ancient city. Let's look around quietly first.",
            "Этот укреплённый сланец похож на часть древнего города. Осторожнее с вибрациями.|That reinforced deepslate looks like part of an ancient city. Be careful with vibrations.");
        Add(d, "structure/stronghold",
            "Рамка портала в Край! Похоже, нашли портальную комнату крепости.|An End portal frame! Looks like we've found a stronghold's portal room.",
            "Смотри, рамки портала. Давай сначала проверим, безопасно ли здесь.|Look, portal frames. Let's make sure it's safe here first.",
            "Кажется, путь в Край уже близко. Проверим, всё ли у нас готово?|Looks like the way to the End is close now. Shall we check whether we're ready?");
        Add(d, "structure/bastion",
            "Позолоченный чернокамень. Похоже, это остатки бастиона — осмотримся осторожно.|Gilded blackstone. Looks like bastion remnants; let's look around carefully.",
            "Этот позолоченный чернокамень напоминает о бастионах. Лучше не терять бдительность.|That gilded blackstone reminds me of bastions. Better stay alert.",
            "Возможно, мы у бастиона. Давай сначала оценим обстановку, а потом искать добычу.|We might be at a bastion. Let's assess the situation before looking for loot.");
        Add(d, "structure/fortress",
            "Незерские кирпичи и ограды — похоже, мы нашли крепость.|Nether bricks and fences; looks like we've found a fortress.",
            "Эти постройки похожи на незерскую крепость. Будем внимательны на переходах.|These buildings look like a Nether fortress. Let's be careful on the walkways.",
            "Кажется, это незерская крепость. Проверим снаряжение перед разведкой?|This seems to be a Nether fortress. Shall we check our equipment before exploring?");
        Add(d, "structure/end_city",
            "Пурпур рядом с кирпичами Края — похоже, это город Края!|Purpur alongside End stone bricks; looks like an End city!",
            "Эта архитектура похожа на город Края. Давай внимательно осмотримся.|This architecture looks like an End city. Let's take a careful look around.",
            "Кажется, добрались до города Края. На высоте лучше никуда не спешить.|Looks like we've reached an End city. Better take it slowly up high.");
        Add(d, "structure/monument",
            "Призмарин и морские фонари — похоже, это подводная крепость.|Prismarine and sea lanterns; looks like an ocean monument.",
            "Эти блоки напоминают подводную крепость. Проверь запас воздуха перед осмотром.|These blocks suggest an ocean monument. Check your air before exploring.",
            "Кажется, мы у подводной крепости. Давай сначала подготовимся к исследованию.|Looks like we're at an ocean monument. Let's prepare before exploring.");
        Add(d, "structure/abandoned_camp",
            "Костёр, вещи и паутина… Похоже, кто-то давно оставил этот лагерь. Осмотримся?|A campfire, belongings and cobwebs… Looks like someone left this camp a while ago. Shall we look around?",
            "Кажется, нашли заброшенный лагерь. Интересно, кто здесь останавливался?|Looks like we've found an abandoned camp. I wonder who stayed here?",
            "Это место похоже на старую стоянку путешественников. Давай приглядимся к деталям.|This place looks like an old travellers' campsite. Let's take a closer look at the details.");
        Add(d, "structure/village",
            "Колокол и тропинки — похоже, мы в деревне. Можно сделать небольшую остановку.|A bell and paths; looks like we're in a village. We could take a little break.",
            "Кажется, добрались до деревни. Интересно, чем здесь торгуют?|Looks like we've reached a village. I wonder what they trade here?",
            "Похоже на деревенскую площадь. Осмотримся и запомним дорогу обратно.|This looks like a village square. Let's look around and remember the way back.");
        Add(d, "structure/mineshaft",
            "Рельсы и паутина среди деревянных опор… Похоже на заброшенную шахту.|Rails and cobwebs among wooden supports… This looks like an abandoned mineshaft.",
            "Кажется, нашли старую шахту. Давай отмечать повороты, чтобы не заблудиться.|Looks like we've found an old mineshaft. Let's mark the turns so we don't get lost.",
            "Эти рельсы будто ведут в старые выработки. Факелов хватит на разведку?|These rails seem to lead into old mine workings. Do we have enough torches to explore?");
        Add(d, "structure/desert_pyramid",
            "Синяя и оранжевая терракота в песчанике — похоже, это пустынная пирамида. Осторожнее с ловушками.|Blue and orange terracotta in sandstone; this looks like a desert pyramid. Watch out for traps.",
            "Кажется, нашли пустынный храм. Давай смотреть под ноги и не копать прямо вниз.|Looks like we've found a desert temple. Let's watch our step and avoid digging straight down.",
            "Этот узор напоминает пустынную пирамиду. Сначала осмотрим пол, потом будем искать сокровища.|That pattern reminds me of a desert pyramid. Let's inspect the floor before looking for treasure.");
        Add(d, "structure/jungle_temple",
            "Замшелые стены и растяжка… Похоже на храм в джунглях. Тут лучше не спешить.|Mossy walls and a tripwire… This looks like a jungle temple. Better take it slowly.",
            "Кажется, нашли храм среди джунглей. Вижу признаки ловушки — давай разберёмся с ней сначала.|Looks like we've found a temple in the jungle. I see signs of a trap; let's deal with that first.",
            "Это место похоже на старый храм. Раздатчик и растяжка явно заслуживают внимания.|This place looks like an old temple. That dispenser and tripwire certainly deserve a closer look.");
        Add(d, "structure/woodland_mansion",
            "Тёмный дуб, берёзовый пол и красный ковёр… Похоже на лесной особняк.|Dark oak, a birch floor and red carpet… This looks like a woodland mansion.",
            "Кажется, мы в лесном особняке. Проверим комнаты постепенно, чтобы не пропустить опасность.|Looks like we're in a woodland mansion. Let's check the rooms one at a time for danger.",
            "Эта обстановка напоминает особняк. Давай запомним, откуда пришли.|These furnishings remind me of a mansion. Let's remember where we came in.");
        Add(d, "structure/shipwreck",
            "Доски и люки у моря… Похоже на обломки корабля. Осмотрим их поближе?|Planks and trapdoors by the sea… These look like the remains of a ship. Shall we take a closer look?",
            "Кажется, нашли затонувший корабль. Если полезем под воду, следи за воздухом.|Looks like we've found a shipwreck. If we go underwater, keep an eye on your air.",
            "Эта деревянная конструкция похожа на старый корабль. Интересно, что от него осталось?|That wooden structure looks like an old ship. I wonder what's left of it?");
        Add(d, "structure/dungeon",
            "Спавнер рядом с замшелым булыжником — похоже на сокровищницу. Сначала обезопасим комнату.|A spawner beside mossy cobblestone; this looks like a monster room. Let's make it safe first.",
            "Кажется, нашли комнату со спавнером. Заглянуть в сундуки можно после подготовки.|Looks like we've found a spawner room. We can check the chests once we're ready.",
            "Это место напоминает старую сокровищницу. Со спавнером лучше обращаться осторожно.|This place looks like an old dungeon. Better be careful around that spawner.");
        Add(d, "structure/igloo",
            "Снежные стены, кровать и печь — похоже на иглу. Уютное убежище от холода.|Snow walls, a bed and a furnace; this looks like an igloo. A cosy shelter from the cold.",
            "Кажется, нашли иглу. Небольшой домик, а всё самое нужное под рукой.|Looks like we've found an igloo. A little home with the essentials close at hand.",
            "Это снежное жилище похоже на иглу. Сделаем короткую остановку?|This snow dwelling looks like an igloo. Shall we take a short break?");
        Add(d, "structure/swamp_hut",
            "Котёл и гриб в горшке посреди болота… Похоже на хижину ведьмы.|A cauldron and a potted mushroom in the swamp… This looks like a witch hut.",
            "Кажется, нашли ведьмину хижину. Давай сначала убедимся, что входить безопасно.|Looks like we've found a witch hut. Let's make sure it's safe to enter first.",
            "Эта обстановка напоминает хижину ведьмы. Любопытно, но осторожность не помешает.|These furnishings remind me of a witch hut. Interesting, but a little caution won't hurt.");
        Add(d, "structure/pillager_outpost",
            "Тёмный дуб, берёзовые доски и флаг — похоже на аванпост разбойников. Осмотримся осторожно.|Dark oak, birch planks and a banner; this looks like a pillager outpost. Let's look around carefully.",
            "Кажется, добрались до аванпоста. Проверим снаряжение перед дальнейшим осмотром.|Looks like we've reached an outpost. Let's check our equipment before exploring further.",
            "Эта постройка напоминает аванпост разбойников. Лучше заранее продумать путь отхода.|This building looks like a pillager outpost. Better plan a way out before going further.");
        Add(d, "structure/ruined_portal",
            "Обсидиан, плачущий обсидиан и незерак — похоже на разрушенный портал.|Obsidian, crying obsidian and netherrack; this looks like a ruined portal.",
            "Кажется, нашли остатки портала. Плачущий обсидиан для рабочей рамки не подойдёт.|Looks like we've found the remains of a portal. Crying obsidian won't work in a functional frame.",
            "Это место похоже на руины портала. Осмотрим окружение перед ремонтом?|This place looks like the ruins of a portal. Shall we look around before repairing it?");
        Add(d, "structure/ocean_ruins",
            "Подозрительный блок среди кладки у моря… Похоже на подводные руины. Кисточка пригодится.|A suspicious block among masonry by the sea… This looks like ocean ruins. A brush could come in handy.",
            "Кажется, нашли морские руины. Если будем исследовать их под водой, не забывай про воздух.|Looks like we've found ocean ruins. If we explore underwater, remember to watch your air.",
            "Эта кладка напоминает старые морские руины. Давай аккуратно исследуем подозрительные блоки.|This masonry looks like old ocean ruins. Let's carefully investigate the suspicious blocks.");
        Add(d, "structure/trail_ruins",
            "Терракота, кирпичи и подозрительный гравий — похоже на руины троп. Достаём кисточку?|Terracotta, bricks and suspicious gravel; this looks like trail ruins. Shall we get the brush out?",
            "Кажется, нашли руины троп. Подозрительный гравий лучше чистить, а не ломать.|Looks like we've found trail ruins. Better brush the suspicious gravel instead of breaking it.",
            "Эта старая кладка похожа на руины троп. Интересно, какую историю мы здесь откопаем?|This old masonry looks like trail ruins. I wonder what story we'll uncover here?");
        Add(d, "structure/nether_fossil",
            "Костные блоки в Незере… Похоже на древние останки. Интересно, кому они принадлежали?|Bone blocks in the Nether… This looks like an ancient fossil. I wonder what it belonged to?",
            "Кажется, нашли незерские останки. Даже в таком месте сохранились следы прошлого.|Looks like we've found a Nether fossil. Even here, traces of the past remain.",
            "Эти кости похожи на незерскую окаменелость. Давай рассмотрим их поближе.|These bones look like a Nether fossil. Let's take a closer look.");
        Add(d, "structure/buried_treasure",
            "Сундук среди песка и гравия у моря. Возможно, это чей-то зарытый клад — заглянем?|A chest among sand and gravel by the sea. It might be someone's buried treasure; shall we look inside?",
            "Смотри, сундук у берега! Может, тайник, а может, просто чьи-то запасы.|Look, a chest by the shore! It could be a hidden cache, or just someone's supplies.",
            "Этот сундук похож на прибрежный тайник. По одному виду не угадаешь, что внутри.|That chest looks like a coastal cache. You can't tell what's inside just by looking at it.");
        Add(d, "event/StructureSighting",
            "Похоже, здесь есть что исследовать. Давай осмотримся.|Looks like there's something to explore here. Let's look around.",
            "Интересная находка прямо перед нами. Приглядимся?|An interesting find right in front of us. Shall we take a closer look?",
            "Это место заслуживает внимания. Посмотрим поближе.|This place deserves some attention. Let's take a closer look.");
    }
}
