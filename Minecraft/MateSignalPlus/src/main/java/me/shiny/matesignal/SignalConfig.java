package me.shiny.matesignal;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import net.fabricmc.loader.api.FabricLoader;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.nio.file.AtomicMoveNotSupportedException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;

public final class SignalConfig {
    private static final Logger LOGGER = LoggerFactory.getLogger("matesignal");
    private static final Gson GSON = new GsonBuilder().setPrettyPrinting().create();
    private static final Path FILE = FabricLoader.getInstance().getConfigDir().resolve("matesignal.json");
    public static SignalData DATA = new SignalData();

    public static void load() {
        if (!Files.exists(FILE)) {
            save(DATA);
            return;
        }
        try {
            SignalData loaded = GSON.fromJson(Files.readString(FILE), SignalData.class);
            if (loaded == null) throw new IOException("Config contains null");
            loaded.normalize();
            DATA = loaded;
        } catch (Exception e) {
            LOGGER.warn("Cannot read {}; using defaults without overwriting the file", FILE, e);
        }
    }

    public static boolean save(SignalData data) {
        data.normalize();
        Path temp = FILE.resolveSibling("matesignal.json.tmp");
        try {
            Files.createDirectories(FILE.getParent());
            Files.writeString(temp, GSON.toJson(data));
            try {
                Files.move(temp, FILE, StandardCopyOption.ATOMIC_MOVE, StandardCopyOption.REPLACE_EXISTING);
            } catch (AtomicMoveNotSupportedException e) {
                Files.move(temp, FILE, StandardCopyOption.REPLACE_EXISTING);
            }
            DATA = data;
            return true;
        } catch (IOException e) {
            LOGGER.error("Cannot save Mate Signal Plus settings to {}", FILE, e);
            return false;
        }
    }

    public static SignalData copy() {
        return GSON.fromJson(GSON.toJson(DATA), SignalData.class);
    }

    public static final class SignalData {
        public int radius = 10;
        public List<String> mobs = new ArrayList<>(List.of("minecraft:creeper", "minecraft:zombie"));
        public boolean dayMessage = true;
        public boolean nightMessage = true;
        public boolean lowHealthMessage = true;
        public boolean lowHungerMessage = true;
        public boolean deathMessage = true;
        public boolean rainStartMessage = true;
        public boolean drowningHalfMessage = true;
        public boolean sleepMessage = true;
        public boolean craftingMessage = true;
        public boolean eatMessage = true;
        public boolean killMessage = true;
        public boolean biomeMessage = true;
        public List<String> disabledBiomes = new ArrayList<>();
        public boolean durabilityLowMessage = true;
        public boolean durabilityCriticalMessage = true;
        public boolean farmMessage = true;
        public boolean cropFarmMessage = true;
        public boolean treeFarmMessage = true;
        public boolean chestMessage = true;
        public boolean chestMessMessage = true;
        public boolean structureMessage = true;
        public List<String> disabledStructures = new ArrayList<>();
        public boolean blockMessage = true;
        public List<String> disabledBlocks = new ArrayList<>();

        private void normalize() {
            radius = Math.clamp(radius, 3, 64);
            if (mobs == null) mobs = new ArrayList<>(List.of("minecraft:creeper", "minecraft:zombie"));
            mobs = new ArrayList<>(mobs.stream().filter(s -> s != null && !s.isBlank())
                    .map(s -> s.trim().toLowerCase(Locale.ROOT)).distinct().toList());
            if (disabledBiomes == null) disabledBiomes = new ArrayList<>();
            disabledBiomes = new ArrayList<>(disabledBiomes.stream().filter(s -> s != null && !s.isBlank())
                    .map(s -> s.trim().toLowerCase(Locale.ROOT)).distinct().toList());
            if (disabledBlocks == null) disabledBlocks = new ArrayList<>();
            disabledBlocks = new ArrayList<>(disabledBlocks.stream().filter(s -> s != null && !s.isBlank())
                    .map(s -> s.trim().toLowerCase(Locale.ROOT)).distinct().toList());
            if (disabledStructures == null) disabledStructures = new ArrayList<>();
            disabledStructures = new ArrayList<>(disabledStructures.stream().filter(s -> s != null && !s.isBlank())
                    .map(s -> s.trim().toLowerCase(Locale.ROOT)).distinct().toList());
        }

        public boolean allows(String id) {
            return mobs.contains(id) || mobs.contains(id.substring(id.indexOf(':') + 1));
        }

        public boolean allowsBiome(String id) {
            return !disabledBiomes.contains(id) && !disabledBiomes.contains(id.substring(id.indexOf(':') + 1));
        }

        public boolean allowsBlock(String id) {
            return !disabledBlocks.contains(id) && !disabledBlocks.contains(id.substring(id.indexOf(':') + 1));
        }
    }
}
