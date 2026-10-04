using System.Collections.Generic;

internal sealed partial class MinecraftDialogue
{
    private static void AddNewMobs(Dictionary<string, Line[]> d)
    {
        Add(d, "mob/minecraft:bogged",
            "Болотник рядом. Осторожнее с его отравленными стрелами.|Bogged nearby. Watch out for its poison arrows.",
            "Смотри, болотник! За мхом прячется опасный стрелок.|Look, a bogged! A dangerous archer beneath that moss.",
            "Рядом болотник. Лучше встретить его со щитом.|Bogged nearby. Better face it with a shield.",
            "Болотник! Поищем укрытие, прежде чем он выстрелит.|A bogged! Let's find cover before it shoots.");
        Add(d, "mob/minecraft:breeze",
            "Вихрь рядом. Не дай ему сбить тебя с удобной позиции.|Breeze nearby. Don't let it knock you out of position.",
            "Смотри, вихрь! Здесь может стать очень ветрено.|Look, a breeze! Things could get very windy.",
            "Рядом вихрь. У края лучше не задерживаться.|Breeze nearby. Best not linger near an edge.",
            "Вихрь! Следим, куда нас может отбросить.|A breeze! Watch where it might knock us.");
        Add(d, "mob/minecraft:creaking",
            "Скрипун рядом. Пока смотришь на него, он не двигается.|Creaking nearby. It won't move while you're looking at it.",
            "Смотри, скрипун! Не поворачивайся к нему спиной без плана.|Look, a creaking! Don't turn your back without a plan.",
            "Рядом скрипун. Если он связан с сердцем, одних ударов будет мало.|Creaking nearby. If it's linked to a heart, hitting it won't be enough.",
            "Скрипун! Держи его в поле зрения, пока выбираешь путь.|A creaking! Keep it in view while choosing a path.");
        Add(d, "mob/minecraft:parched",
            "Пустынник рядом. Поищем укрытие от его стрел.|Parched nearby. Let's find cover from its arrows.",
            "Смотри, пустынник! Солнце ему не мешает.|Look, a parched! The sun doesn't bother it.",
            "Рядом пустынник. Щит сейчас очень пригодится.|Parched nearby. A shield would be very useful now.",
            "Пустынник! Не стой на открытом месте.|A parched! Don't stand out in the open.");
        Add(d, "mob/minecraft:sulfur_cube",
            "О, серный куб! Какой забавный. Засунем в него что-нибудь?|Oh, a sulfur cube! How funny. Shall we put something inside it?",
            "Смотри, серный куб! Интересно, как он изменится от разных блоков.|Look, a sulfur cube! I wonder how different blocks will change it.",
            "Рядом серный куб. Можно устроить маленький опыт с обычным блоком.|Sulfur cube nearby. We could try a little experiment with an ordinary block.",
            "Серный куб! Так и хочется рассмотреть этого попрыгуна поближе.|A sulfur cube! I want a closer look at this bouncer.");
        d.Add("mob/minecraft:sulfur_cube/empty", d["mob/minecraft:sulfur_cube"]);
        Add(d, "mob/minecraft:sulfur_cube/small",
            "Маленький серный куб! Сначала подрастим его слизью?|A little sulfur cube! Shall we grow it with slimeballs first?",
            "Смотри, какой крошечный серный куб. Ему бы немного слизи.|Look at that tiny sulfur cube. It could use some slimeballs.",
            "Этот серный куб ещё мал для блока. Начнём со слизи.|This sulfur cube is still too small for a block. Let's start with slimeballs.",
            "Крошечный серный попрыгун! Можно помочь ему вырасти.|A tiny sulfur bouncer! We could help it grow.");
        Add(d, "mob/minecraft:sulfur_cube/filled",
            "Серный куб уже носит что-то внутри! Посмотрим, как он себя ведёт.|The sulfur cube already has something inside! Let's see how it behaves.",
            "Смотри, у серного куба есть начинка. Получился ходячий эксперимент.|Look, that sulfur cube has a filling. A walking experiment.",
            "Внутри серного куба уже есть блок. Интересно, что изменилось?|There's already a block inside the sulfur cube. I wonder what changed?",
            "Этот серный куб уже с сюрпризом. Давай сначала понаблюдаем.|This sulfur cube already has a surprise inside. Let's watch first.");
        Add(d, "mob/minecraft:sulfur_cube/primed",
            "У серного куба зажжён TNT! Отойдём подальше.|The sulfur cube has lit TNT inside! Let's back away.",
            "Осторожно, этот серный куб сейчас взорвётся!|Careful, this sulfur cube is about to explode!",
            "Эксперимент стал взрывоопасным. Быстрее от серного куба!|The experiment turned explosive. Get away from the sulfur cube!",
            "Серный куб с горящим TNT! Наблюдать лучше издалека.|A sulfur cube with burning TNT! Better watch from a distance.");
        Add(d, "mob/minecraft:camel_husk",
            "Верблюд-кадавр рядом. Какая необычная встреча!|Camel husk nearby. What an unusual encounter!",
            "Смотри, верблюд-кадавр. Сам по себе он не ищет драки.|Look, a camel husk. On its own, it isn't looking for a fight.",
            "Рядом верблюд-кадавр без враждебного всадника. Можно присмотреться.|A camel husk without a hostile rider nearby. We can take a look.",
            "Верблюд-кадавр! Совсем необычный спутник для пустынного путешествия.|A camel husk! An unusual companion for a desert journey.");
        Add(d, "mob/minecraft:camel_husk/hostile_rider",
            "На верблюде-кадавре враждебный всадник. Держим дистанцию!|Hostile rider on a camel husk. Keep your distance!",
            "Верблюд-кадавр с опасной компанией! Лучше не стоять на пути.|Camel husk with dangerous company! Best stay out of its way.",
            "Смотри, на верблюде-кадавре нежить. Сначала найдём укрытие.|Look, undead riding a camel husk. Let's find cover first.",
            "Всадник на верблюде-кадавре нам не друг. Поищем безопасный обход.|That camel husk rider isn't a friend. Let's find a safe way around.");
        Add(d, "mob/minecraft:zombie_horse",
            "Лошадь-зомби рядом. Необычный скакун!|Zombie horse nearby. An unusual steed!",
            "Смотри, лошадь-зомби. Без враждебного всадника она не ищет боя.|Look, a zombie horse. Without a hostile rider, it isn't looking for a fight.",
            "Рядом лошадь-зомби. У нашего путешествия появился мрачный колорит.|Zombie horse nearby. Our journey just got a spooky touch.",
            "Лошадь-зомби! Интересно посмотреть на неё поближе.|A zombie horse! I'd like a closer look.");
        Add(d, "mob/minecraft:zombie_horse/hostile_rider",
            "На лошади-зомби враждебный всадник! Следи за его приближением.|Hostile rider on a zombie horse! Watch it approach.",
            "Лошадь-зомби несёт опасного пассажира. Поищем укрытие.|That zombie horse carries a dangerous passenger. Let's find cover.",
            "Конная нежить рядом! Оставим себе место для манёвра.|Mounted undead nearby! Leave room to manoeuvre.",
            "Смотри, всадник на лошади-зомби. Лучше не встречать его вплотную.|Look, a rider on a zombie horse. Best not meet it up close.");
        Add(d, "mob/minecraft:zombie_nautilus",
            "Наутилус-зомби рядом. Какой необычный морской житель!|Zombie nautilus nearby. What an unusual sea creature!",
            "Смотри, наутилус-зомби. Без враждебного всадника встреча спокойнее.|Look, a zombie nautilus. A calmer meeting without a hostile rider.",
            "Рядом наутилус-зомби. Давай понаблюдаем, не нападая.|Zombie nautilus nearby. Let's watch without attacking.",
            "Наутилус-зомби! У подводного мира свои удивительные обитатели.|A zombie nautilus! The underwater world has fascinating residents.");
        Add(d, "mob/minecraft:zombie_nautilus/hostile_rider",
            "На наутилусе-зомби враждебный всадник. Под водой стало опаснее.|Hostile rider on a zombie nautilus. The water just got more dangerous.",
            "Наутилус-зомби с опасным пассажиром! Держим путь к поверхности свободным.|Zombie nautilus with a dangerous passenger! Keep a clear route to the surface.",
            "Рядом подводный всадник. Не забывай про воздух и укрытие.|Underwater rider nearby. Remember your air and cover.",
            "Смотри, наутилус-зомби несёт нежить. Лучше не подплывать вплотную.|Look, a zombie nautilus carrying undead. Best not swim too close.");
        Add(d, "mob/minecraft:happy_ghast",
            "Счастливый гаст! Вот на ком хочется прокатиться по небу.|A happy ghast! Now that's someone I'd like to fly with.",
            "Смотри, счастливый гаст. Какой уютный воздушный спутник!|Look, a happy ghast. Such a cosy flying companion!",
            "Рядом счастливый гаст. Устроим неспешную воздушную прогулку?|Happy ghast nearby. Shall we take a leisurely flight?",
            "Этот гаст явно настроен дружелюбно. Так и хочется махнуть ему.|This ghast looks friendly. I want to wave at it.");
        Add(d, "mob/minecraft:copper_golem",
            "Медный голем рядом! Маленький помощник с большими делами.|Copper golem nearby! A little helper with a lot to do.",
            "Смотри, медный голем. Наведём порядок в сундуках вместе с ним?|Look, a copper golem. Shall we organise our chests with it?",
            "Рядом медный голем. У него такой деловитый вид!|Copper golem nearby. It looks so industrious!",
            "Медный голем! Хорошая компания для нашего склада.|A copper golem! Good company for our storage room.");
        Add(d, "mob/minecraft:nautilus",
            "Наутилус рядом! Какой любопытный морской сосед.|Nautilus nearby! What a curious sea neighbour.",
            "Смотри, наутилус. Познакомимся спокойно, без ударов?|Look, a nautilus. Shall we say hello peacefully, without hitting it?",
            "Рядом наутилус. С таким спутником подводное путешествие интереснее.|Nautilus nearby. A companion like that makes underwater travel more interesting.",
            "Наутилус! Давай немного понаблюдаем за ним.|A nautilus! Let's watch it for a while.");
    }
}
