using System.Collections.Generic;

internal sealed partial class MinecraftDialogue
{
    private static void AddMobs(Dictionary<string, Line[]> d)
    {
        Add(d, "mob/minecraft:creeper",
            "Крипер рядом! Давай оставим между нами побольше места.|Creeper nearby! Let's give it plenty of space.",
            "Смотри, крипер. Только без взрывных знакомств.|Look, a creeper. Let's avoid an explosive introduction.",
            "Кажется, у нас зелёный гость. Не подпускай крипера близко.|Looks like we have a green visitor. Keep that creeper away.",
            "Осторожно, крипер! Нашим постройкам такая компания ни к чему.|Careful, a creeper! Our builds don't need that kind of company.");
        Add(d, "mob/minecraft:zombie",
            "Рядом зомби. Проверим, не идёт ли следом ещё кто-нибудь.|Zombie nearby. Let's check if more are following.",
            "Зомби решил составить нам компанию. Обойдём его?|A zombie wants to join us. Shall we go around?",
            "Впереди зомби. Держи оружие под рукой.|Zombie ahead. Keep a weapon handy.",
            "Мы здесь не одни — рядом бродит зомби.|We're not alone. There's a zombie wandering nearby.");
        Add(d, "mob/minecraft:skeleton",
            "Рядом скелет. Поищем укрытие от стрел.|Skeleton nearby. Let's find cover from its arrows.",
            "Скелет с луком! Лучше не стоять на открытом месте.|A skeleton with a bow! Best not stand in the open.",
            "Смотри, скелет. Щит сейчас пригодится.|Look, a skeleton. A shield could help now.",
            "У нас костлявый стрелок поблизости. Двигайся осторожно.|A bony archer nearby. Move carefully.");
        Add(d, "mob/minecraft:spider",
            "Рядом паук. У него слишком много ног для тихой встречи.|Spider nearby. Too many legs for a quiet encounter.",
            "Смотри, паук. Не забудь, что он умеет лазать по стенам.|Look, a spider. Remember it can climb walls.",
            "Паук поблизости. Понаблюдаем, прежде чем приближаться.|Spider nearby. Let's watch before getting closer.",
            "Восьминогая компания! Лучше оставим пауку немного места.|Eight-legged company! Let's give the spider some room.");
        Add(d, "mob/minecraft:cave_spider",
            "Пещерный паук! Его укусы могут отравить.|Cave spider! Its bites can poison you.",
            "Осторожно, маленький пещерный паук. Размер здесь обманчив.|Careful, a little cave spider. Its size is deceptive.",
            "Рядом пещерный паук. Давай не подпускать его к себе.|Cave spider nearby. Let's keep it away.",
            "Пещерный паук пролезает в узкие места. Выбирай укрытие внимательно.|Cave spiders fit through tight gaps. Choose cover carefully.");
        Add(d, "mob/minecraft:enderman",
            "Эндермен рядом. Лучше не смотреть ему в глаза.|Enderman nearby. Best not look into its eyes.",
            "Смотри… хотя нет, с эндерменом лучше без зрительного контакта.|Look… actually, best avoid eye contact with an enderman.",
            "Рядом эндермен. Пусть спокойно занимается своими блоками.|Enderman nearby. Let's leave it to its blocks.",
            "Высокий гость поблизости. Не будем провоцировать эндермена.|A tall visitor nearby. Let's not provoke the enderman.");
        Add(d, "mob/minecraft:endermite",
            "Эндермит! Такой маленький, а неприятностей хватает.|An endermite! So small, yet so troublesome.",
            "Смотри под ноги — рядом эндермит.|Watch your feet. There's an endermite nearby.",
            "Маленький эндермит решил вмешаться в наше путешествие.|A little endermite has decided to interrupt our journey.",
            "Рядом шевелится эндермит. Лучше не пропускать его из виду.|An endermite is scuttling nearby. Let's keep an eye on it.");
        Add(d, "mob/minecraft:slime",
            "Слизень рядом! Какие упругие прыжки.|Slime nearby! Such bouncy jumps.",
            "Смотри, слизень. Размер у этих прыгунов имеет значение.|Look, a slime. Size matters with these bouncers.",
            "Рядом слизень. Не будем торопиться в его объятия.|Slime nearby. Let's not rush into a hug.",
            "Зелёный попрыгун! После большого слизня могут остаться маленькие.|A green bouncer! A big slime can leave smaller ones behind.");
        Add(d, "mob/minecraft:magma_cube",
            "Магмовый куб! Этого попрыгуна лучше обходить.|Magma cube! Best give this bouncer a wide berth.",
            "Рядом магмовый куб. Держись подальше от края.|Magma cube nearby. Stay away from the edge.",
            "Смотри, магмовый куб. Прыгает бодро, встречает неласково.|Look, a magma cube. Lively jumps, unfriendly greetings.",
            "Магмовый куб поблизости. Не дадим его прыжкам застать нас врасплох.|Magma cube nearby. Let's not let its jumps catch us off guard.");
        Add(d, "mob/minecraft:blaze",
            "Ифрит рядом. Поищем укрытие от огня.|Blaze nearby. Let's find cover from the fire.",
            "Смотри, ифрит! Огнестойкость сейчас была бы кстати.|Look, a blaze! Fire resistance would be handy now.",
            "Поблизости ифрит. Не задерживайся на линии огня.|Blaze nearby. Don't linger in its line of fire.",
            "Ифрит! Давай сначала выберем удобное место для боя.|A blaze! Let's choose a good place to fight first.");
        Add(d, "mob/minecraft:ghast",
            "Гаст рядом. Следи за огненными шарами!|Ghast nearby. Watch for fireballs!",
            "Смотри, гаст. Под открытым небом Незера бывает неспокойно.|Look, a ghast. The Nether's open spaces can get lively.",
            "У нас летающий сосед. Поищем укрытие от гаста.|We have a flying neighbour. Let's find cover from the ghast.",
            "Гаст поблизости! Над пропастью лучше не отвлекаться.|Ghast nearby! Don't get distracted over a drop.");
        Add(d, "mob/minecraft:witch",
            "Ведьма рядом. Держимся подальше от её зелий.|Witch nearby. Keep clear of her potions.",
            "Смотри, ведьма. У неё наверняка свой план на эту встречу.|Look, a witch. She probably has her own plan for this encounter.",
            "Поблизости ведьма. Неприятные зелья лучше не ловить.|Witch nearby. Best not catch any nasty potions.",
            "Ведьма! Продумай подход, прежде чем сближаться.|A witch! Plan your approach before closing in.");
        Add(d, "mob/minecraft:drowned",
            "Утопленник рядом. Под водой стоит держаться настороже.|Drowned nearby. Stay alert underwater.",
            "Смотри, утопленник. Проверим, нет ли у него трезубца.|Look, a drowned. Let's check whether it has a trident.",
            "В воде появилась нежелательная компания — утопленник.|Unwanted company in the water: a drowned.",
            "Рядом утопленник. Не забывай про воздух во время боя.|Drowned nearby. Remember your air while fighting.");
        Add(d, "mob/minecraft:husk",
            "Кадавр рядом. От его удара можно проголодаться ещё сильнее.|Husk nearby. Its hit can leave you even hungrier.",
            "Смотри, кадавр. Солнце от него не спасёт.|Look, a husk. Sunlight won't save us from it.",
            "Рядом пустынная нежить — кадавр. Держим дистанцию.|Desert undead nearby: a husk. Keep some distance.",
            "Кадавр! Лучше не давать ему подобраться вплотную.|A husk! Best not let it get too close.");
        Add(d, "mob/minecraft:stray",
            "Зимогор рядом. Его стрелы могут замедлить.|Stray nearby. Its arrows can slow you down.",
            "Смотри, зимогор. Лучше найти укрытие до первого выстрела.|Look, a stray. Best find cover before the first shot.",
            "Рядом зимогор! Щит и свободный путь назад пригодятся.|Stray nearby! A shield and a clear way back could help.",
            "Холодный приём от зимогора нам ни к чему. Обойдём?|We don't need a frosty welcome from that stray. Go around?"
        );
        Add(d, "mob/minecraft:phantom",
            "Фантом рядом! Посматривай вверх.|Phantom nearby! Keep looking up.",
            "Смотри, фантом. Возможно, пора найти кровать.|Look, a phantom. Perhaps it's time to find a bed.",
            "Летающий гость! От фантома удобно укрыться под крышей.|A flying visitor! A roof is handy cover from a phantom.",
            "Фантом поблизости. Не дадим ему зайти со спины.|Phantom nearby. Let's not let it get behind us.");
        Add(d, "mob/minecraft:silverfish",
            "Чешуйница! Смотри под ноги.|Silverfish! Watch your feet.",
            "Рядом чешуйница. Маленькая, но назойливая.|Silverfish nearby. Small, but persistent.",
            "Смотри, чешуйница. В камнях может скрываться ещё компания.|Look, a silverfish. More company might be hiding in the stone.",
            "Чешуйница поблизости. Давай не устраивать суету в узком проходе.|Silverfish nearby. Let's avoid a scramble in a narrow passage.");
        Add(d, "mob/minecraft:guardian",
            "Страж рядом. Поищем укрытие от его луча.|Guardian nearby. Let's find cover from its beam.",
            "Смотри, страж! Под водой у нас серьёзный сосед.|Look, a guardian! Serious company underwater.",
            "Рядом страж. Не забывай про воздух и путь к поверхности.|Guardian nearby. Remember your air and route to the surface.",
            "Страж! Лучше прервать его прицеливание укрытием.|A guardian! Best break its line of sight with cover.");
        Add(d, "mob/minecraft:elder_guardian",
            "Древний страж! Мы подобрались к серьёзному противнику.|An elder guardian! We've found a serious opponent.",
            "Рядом древний страж. Путь назад должен оставаться свободным.|Elder guardian nearby. Keep our way back clear.",
            "Древний страж поблизости. К подводному бою лучше подготовиться.|Elder guardian nearby. Best prepare for an underwater fight.",
            "Смотри, древний страж. Следи и за здоровьем, и за воздухом.|Look, an elder guardian. Watch both health and air.");
        Add(d, "mob/minecraft:shulker",
            "Шалкер рядом. Его снаряды могут поднять тебя в воздух.|Shulker nearby. Its projectiles can lift you into the air.",
            "Смотри, шалкер. Заранее подумай, куда потом приземляться.|Look, a shulker. Think ahead about where you'll land.",
            "Рядом шалкер! У края острова его левитация особенно опасна.|Shulker nearby! Its levitation is especially dangerous near an edge.",
            "Эта коробочка умеет огрызаться. Осторожнее с шалкером.|That little box bites back. Careful with the shulker.");
        Add(d, "mob/minecraft:pillager",
            "Разбойник рядом. Берегись арбалета.|Pillager nearby. Watch out for the crossbow.",
            "Смотри, разбойник! Лучше не оставаться на открытом месте.|Look, a pillager! Best not stay in the open.",
            "Рядом разбойник. Проверим, один ли он.|Pillager nearby. Let's check whether it's alone.",
            "Разбойник поблизости. Щит сейчас пригодится.|Pillager nearby. A shield could come in handy.");
        Add(d, "mob/minecraft:vindicator",
            "Поборник рядом. С его топором лучше не спорить вблизи.|Vindicator nearby. Best not argue with that axe up close.",
            "Смотри, поборник! Оставим себе место для отступления.|Look, a vindicator! Leave room to retreat.",
            "Рядом поборник. Не подпускай его вплотную.|Vindicator nearby. Don't let it get too close.",
            "Поборник! В узком коридоре такая встреча особенно неприятна.|A vindicator! An especially unpleasant meeting in a narrow corridor.");
        Add(d, "mob/minecraft:evoker",
            "Вызыватель рядом. Следи за его колдовством.|Evoker nearby. Watch its spells.",
            "Смотри, вызыватель! Встреча может быстро стать шумной.|Look, an evoker! This meeting could get lively quickly.",
            "Рядом вызыватель. Не стой на месте, когда появятся клыки.|Evoker nearby. Keep moving when the fangs appear.",
            "Вызыватель! Лучше не давать ему спокойно призывать помощников.|An evoker! Best not let it summon helpers undisturbed.");
        Add(d, "mob/minecraft:vex",
            "Вредина рядом! Стена от него не спасёт.|Vex nearby! A wall won't keep it out.",
            "Смотри, вредина. Маленький, летающий и очень настойчивый.|Look, a vex. Small, flying and very persistent.",
            "Рядом вредина. Следи за ним даже возле укрытия.|Vex nearby. Keep track of it even near cover.",
            "Вредина! Не дадим этому малышу сбить нас с толку.|A vex! Let's not let this little thing throw us off.");
        Add(d, "mob/minecraft:ravager",
            "Разоритель рядом! У этой махины лучше не стоять на пути.|Ravager nearby! Best not stand in that beast's way.",
            "Смотри, разоритель. Поищем надёжную позицию.|Look, a ravager. Let's find a solid position.",
            "Рядом разоритель. Нам понадобится пространство.|Ravager nearby. We're going to need room.",
            "Разоритель! Сначала наметим путь для отступления.|A ravager! Let's plan a retreat first.");
        Add(d, "mob/minecraft:piglin",
            "Пиглин рядом. Есть на тебе что-нибудь из золотой брони?|Piglin nearby. Are you wearing any gold armour?",
            "Смотри, пиглин. Золото может помочь завязать знакомство.|Look, a piglin. Gold could help with introductions.",
            "Рядом пиглин. Не будем трогать его ценности.|Piglin nearby. Let's leave its valuables alone.",
            "Пиглин! Если обстановка спокойная, можно подумать об обмене.|A piglin! If things are calm, we could consider bartering.");
        Add(d, "mob/minecraft:piglin_brute",
            "Жестокий пиглин! Золотая броня его не успокоит.|Piglin brute! Gold armour won't calm this one down.",
            "Рядом жестокий пиглин. Лучше сразу выбрать путь назад.|Piglin brute nearby. Best choose a way back now.",
            "Смотри, жестокий пиглин. С ним обмен не получится.|Look, a piglin brute. No bartering with this one.",
            "Жестокий пиглин поблизости. Держись подальше от его топора.|Piglin brute nearby. Stay clear of its axe.");
        Add(d, "mob/minecraft:zombified_piglin",
            "Зомбифицированный пиглин рядом. Не будем его трогать.|Zombified piglin nearby. Let's leave it alone.",
            "Смотри, зомбифицированный пиглин. Мирно разойдёмся?|Look, a zombified piglin. Shall we pass peacefully?",
            "Рядом зомбифицированный пиглин. Лишний удар может привлечь всю компанию.|Zombified piglin nearby. One stray hit could draw in the whole group.",
            "Пусть зомбифицированный пиглин идёт своей дорогой, а мы — своей.|Let the zombified piglin go its way, and we'll go ours.");
        Add(d, "mob/minecraft:hoglin",
            "Хоглин рядом. Не стой у края — он может отбросить.|Hoglin nearby. Stay away from edges; it can knock you back.",
            "Смотри, хоглин! Лучше дать ему побольше места.|Look, a hoglin! Best give it plenty of room.",
            "Рядом хоглин. Эти клыки выглядят убедительно.|Hoglin nearby. Those tusks make a convincing argument.",
            "Хоглин! Поищем маршрут без тесного знакомства.|A hoglin! Let's find a route without a close introduction.");
        Add(d, "mob/minecraft:zoglin",
            "Зоглин рядом. Дружбы от него лучше не ждать.|Zoglin nearby. Best not expect friendship.",
            "Смотри, зоглин! Держим дистанцию.|Look, a zoglin! Keep some distance.",
            "Рядом зоглин. Очень неспокойная компания.|Zoglin nearby. Very restless company.",
            "Зоглин поблизости. Выберем место, где есть куда отойти.|Zoglin nearby. Let's pick somewhere with room to back away.");
        Add(d, "mob/minecraft:wither_skeleton",
            "Скелет-иссушитель рядом. Его мечом лучше не получать.|Wither skeleton nearby. Best not get hit by its sword.",
            "Смотри, скелет-иссушитель! Эффект иссушения нам ни к чему.|Look, a wither skeleton! We don't need the Wither effect.",
            "Рядом скелет-иссушитель. Не подпускай его к себе.|Wither skeleton nearby. Keep it away.",
            "Скелет-иссушитель! Давай сначала выберем удобный проход.|A wither skeleton! Let's choose a suitable passage first.");
        Add(d, "mob/minecraft:wither",
            "Иссушитель рядом! Это уже серьёзный бой.|Wither nearby! This is a serious fight.",
            "Смотри, иссушитель. Надеюсь, подготовка была основательной.|Look, a wither. I hope we're well prepared.",
            "Рядом иссушитель. Держись подальше от ценных построек.|Wither nearby. Keep away from valuable builds.",
            "Иссушитель! Следим за здоровьем и оставляем путь для отступления.|A wither! Watch your health and leave room to retreat.");
        Add(d, "mob/minecraft:ender_dragon",
            "Дракон Энда рядом! Вот это встреча.|Ender dragon nearby! What an encounter.",
            "Смотри, дракон! Не забывай следить за землёй под ногами.|Look, the dragon! Remember to watch the ground beneath your feet.",
            "Дракон поблизости. Лучше не стоять в его дыхании.|Dragon nearby. Best not stand in its breath.",
            "Рядом дракон Энда. Давай действовать спокойно и внимательно.|Ender dragon nearby. Let's stay calm and focused.");
        Add(d, "mob/minecraft:warden",
            "Хранитель рядом. Тише… лучше отступить.|Warden nearby. Quiet… better retreat.",
            "Хранитель! Давай без резких движений и лишнего шума.|A warden! Let's avoid sudden moves and unnecessary noise.",
            "Рядом хранитель. Это плохой момент для геройства.|Warden nearby. This is a bad time for heroics.",
            "Тихо, хранитель поблизости. Найдём путь подальше от него.|Quiet, a warden is close. Let's find a way away from it.");
        Add(d, "mob/minecraft:zombie_villager",
            "Зомби-житель рядом. Его ведь можно вылечить.|Zombie villager nearby. We could cure it.",
            "Смотри, зомби-житель. Может, дадим ему второй шанс?|Look, a zombie villager. Shall we give it a second chance?",
            "Рядом зомби-житель. Для лечения пригодятся слабость и золотое яблоко.|Zombie villager nearby. Weakness and a golden apple could help cure it.",
            "Зомби-житель! Сначала стоит найти безопасное место для лечения.|A zombie villager! First, let's find somewhere safe for the cure.");
        Add(d, "mob/minecraft:giant",
            "Гигант! Вот это размер.|A giant! Look at the size of it.",
            "Рядом гигант. Такого соседа трудно не заметить.|Giant nearby. Hard to miss a neighbour like that.",
            "Смотри, гигант! Мы попали в необычное приключение.|Look, a giant! This is an unusual adventure.",
            "Гигант поблизости. Посмотрим на него с удобного расстояния.|Giant nearby. Let's admire it from a comfortable distance.");
        Add(d, "mob/minecraft:illusioner",
            "Иллюзор рядом. Тут легко запутаться.|Illusioner nearby. Things could get confusing.",
            "Смотри, иллюзор! Не будем верить первому впечатлению.|Look, an illusioner! Let's not trust first impressions.",
            "Рядом иллюзор. Следи за тем, откуда летят стрелы.|Illusioner nearby. Watch where the arrows come from.",
            "Иллюзор! Сначала разберёмся, что происходит вокруг.|An illusioner! Let's work out what's happening around us.");
        AddNewMobs(d);
    }
}
