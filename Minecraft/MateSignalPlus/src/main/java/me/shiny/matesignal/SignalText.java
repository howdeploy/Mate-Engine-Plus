package me.shiny.matesignal;

import net.minecraft.client.Minecraft;
import net.minecraft.locale.Language;
import net.minecraft.network.chat.Component;
import net.minecraft.resources.Identifier;
import org.slf4j.LoggerFactory;

import java.io.InputStream;
import java.util.HashMap;
import java.util.Locale;
import java.util.Map;

final class SignalText {
    private static final Map<String, String> strings = new HashMap<>();
    private static String loadedLanguage;

    static Component text(String key, Object... args) {
        String language = Minecraft.getInstance().options.languageCode.toLowerCase(Locale.ROOT).replace('-', '_');
        if (!language.equals(loadedLanguage)) {
            strings.clear();
            load("en_us");
            if (!language.equals("en_us")) load(language);
            loadedLanguage = language;
        }
        Object[] values = new Object[args.length];
        for (int i = 0; i < args.length; i++) values[i] = args[i] instanceof Component c ? c.getString() : args[i];
        // Resolve this menu's dictionary explicitly: a client's UI locale may differ from its game option.
        String template = strings.getOrDefault(key, key);
        return Component.literal(values.length == 0 ? template : String.format(Locale.ROOT, template, values));
    }

    static void reload() { loadedLanguage = null; }

    private static void load(String language) {
        var resource = Minecraft.getInstance().getResourceManager().getResource(
                Identifier.fromNamespaceAndPath("matesignal", "lang/" + language + ".json"));
        try (InputStream stream = resource.isPresent() ? resource.get().open()
                : SignalText.class.getResourceAsStream("/assets/matesignal/lang/" + language + ".json")) {
            if (stream != null) Language.loadFromJson(stream, strings::put);
        } catch (Exception e) {
            LoggerFactory.getLogger("matesignal").warn("Cannot load menu language {}", language, e);
        }
    }
}
