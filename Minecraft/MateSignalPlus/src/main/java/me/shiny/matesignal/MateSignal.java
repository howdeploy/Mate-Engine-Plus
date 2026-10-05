package me.shiny.matesignal;

import com.google.gson.JsonObject;
import net.fabricmc.api.ClientModInitializer;
import net.fabricmc.fabric.api.client.command.v2.ClientCommandRegistrationCallback;
import net.fabricmc.fabric.api.client.event.lifecycle.v1.ClientLifecycleEvents;
import net.fabricmc.fabric.api.client.event.lifecycle.v1.ClientTickEvents;
import net.fabricmc.fabric.api.event.client.player.ClientPlayerBlockBreakEvents;
import net.fabricmc.fabric.api.event.player.UseBlockCallback;
import net.minecraft.client.Minecraft;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.core.component.DataComponents;
import net.minecraft.core.BlockPos;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.world.damagesource.DamageSource;
import net.minecraft.world.InteractionResult;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.LivingEntity;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.entity.projectile.Projectile;
import net.minecraft.world.level.levelgen.Heightmap;
import org.slf4j.LoggerFactory;

import java.util.HashSet;
import java.util.Random;
import java.util.Set;
import java.util.UUID;

import static net.fabricmc.fabric.api.client.command.v2.ClientCommands.literal;

public final class MateSignal implements ClientModInitializer {
    private static MateSignal instance;
    private final Set<UUID> inside = new HashSet<>();
    private final Random rng = new Random();
    private final SignalEquipment equipment = new SignalEquipment();
    private final SignalBlocks blocks = new SignalBlocks();
    private final SignalActivity activity = new SignalActivity();
    private final SignalStructures structures = new SignalStructures();
    private final SignalChests chests = new SignalChests();
    private ClientLevel lastLevel;
    private LocalPlayer lastPlayer;
    private long lastClock = -1;
    private boolean lowHp, lowHunger, rainAnnounced, sleeping, drowning, dead;
    private String lastBiome;
    private String observedBiome;
    private long biomeSince;
    private long nextMob, nextBiome, nextHealth, nextCraft, nextEat, nextKill;
    private int scanTicks;

    @Override
    public void onInitializeClient() {
        instance = this;
        SignalConfig.load();
        ClientTickEvents.END_CLIENT_TICK.register(this::tick);
        ClientPlayerBlockBreakEvents.AFTER.register((level, player, pos, state) -> {
            activity.harvested(player, state, System.currentTimeMillis());
            chests.broken(pos);
        });
        UseBlockCallback.EVENT.register((player, level, hand, hit) -> {
            Minecraft mc = Minecraft.getInstance();
            if (level.isClientSide() && player == mc.player) chests.clicked(mc, hit.getBlockPos(), System.currentTimeMillis());
            return InteractionResult.PASS;
        });
        ClientLifecycleEvents.CLIENT_STOPPING.register(client -> UdpSender.close());
        ClientCommandRegistrationCallback.EVENT.register((dispatcher, registryAccess) -> dispatcher.register(
                literal("matesignal")
                        .then(literal("config").executes(context -> {
                            Minecraft.getInstance().schedule(() -> {
                                Minecraft mc = Minecraft.getInstance();
                                mc.gui.setScreen(new MateSignalConfigScreen(mc.gui.screen()));
                            });
                            return 1;
                        }))
                        .then(literal("test").executes(context -> {
                            boolean sent = UdpSender.send("time_day");
                            context.getSource().sendFeedback(SignalText.text(sent
                                    ? "matesignal.test.sent" : "matesignal.test.failed"));
                            return sent ? 1 : 0;
                        }))));
        LoggerFactory.getLogger("matesignal").info("Mate Signal Plus for Minecraft 26.3; target 127.0.0.1:32145");
    }

    private void reset(ClientLevel level, LocalPlayer player) {
        lastLevel = level;
        lastPlayer = player;
        inside.clear();
        equipment.reset();
        blocks.reset();
        activity.reset();
        structures.reset();
        chests.reset();
        lastClock = -1;
        lowHp = lowHunger = rainAnnounced = sleeping = drowning = dead = false;
        lastBiome = null;
        observedBiome = null;
        biomeSince = 0;
        nextMob = nextHealth = nextCraft = nextEat = nextKill = 0;
        nextBiome = System.currentTimeMillis() + 4_000;
        scanTicks = 0;
    }

    private void tick(Minecraft mc) {
        ClientLevel level = mc.level;
        LocalPlayer player = mc.player;
        if (level != lastLevel || player != lastPlayer) reset(level, player);
        if (level == null || player == null || mc.isPaused()) return;

        SignalConfig.SignalData config = SignalConfig.DATA;
        long now = System.currentTimeMillis();
        float hp = player.getHealth();
        boolean deadNow = player.isDeadOrDying();
        if (deadNow && !dead && config.deathMessage) UdpSender.send("death");
        dead = deadNow;
        if (deadNow) {
            lastClock = -1;
            return;
        }

        long clock = level.getOverworldClockTime();
        if (level.dimension().identifier().toString().equals("minecraft:overworld")
                && lastClock >= 0 && clock > lastClock && clock - lastClock <= 200) {
            if (config.dayMessage && crossed(lastClock, clock, 23500)) UdpSender.send("time_day");
            if (config.nightMessage && crossed(lastClock, clock, 12750)) UdpSender.send("time_night");
        }
        lastClock = clock;

        boolean lowHpNow = hp <= 6;
        if (config.lowHealthMessage && lowHpNow && (!lowHp || now >= nextHealth)) {
            JsonObject event = UdpSender.event("low_health");
            event.addProperty("hp", hp);
            UdpSender.send(event);
            nextHealth = now + 60_000;
        }
        lowHp = lowHpNow;

        int hunger = player.getFoodData().getFoodLevel();
        boolean lowHungerNow = hunger < 10;
        if (config.lowHungerMessage && lowHungerNow && !lowHunger) {
            JsonObject event = UdpSender.event("low_hunger");
            event.addProperty("hunger", hunger);
            UdpSender.send(event);
        }
        lowHunger = lowHungerNow;

        // Shelter and dry/snowy biomes do not end the world's current rain episode.
        if (!level.isRaining()) rainAnnounced = false;
        else if (!rainAnnounced && level.isRainingAt(player.blockPosition())) {
            if (config.rainStartMessage) UdpSender.send("rain_start");
            rainAnnounced = true;
        }
        boolean sleepingNow = player.isSleeping();
        if (config.sleepMessage && sleepingNow && !sleeping) UdpSender.send("sleep_start");
        sleeping = sleepingNow;

        int air = player.getAirSupply(), maxAir = player.getMaxAirSupply();
        if (!player.isUnderWater() || air > maxAir * 0.8) drowning = false;
        if (player.isUnderWater() && air <= maxAir / 2 && !drowning) {
            if (config.drowningHalfMessage) {
                JsonObject event = UdpSender.event("drowning");
                event.addProperty("air", air);
                event.addProperty("max", maxAir);
                UdpSender.send(event);
            }
            drowning = true;
        }

        if (++scanTicks >= 5) {
            scanTicks = 0;
            activity.observe(player, now);
            scanBiome(level, player, config, now);
            scanMobs(level, player, config, now);
            if (!equipment.scan(player, config, now) && !chests.scan(mc, config, structures, now) && !activity.announce(config, now)
                    && !structures.scan(mc, config, now)) blocks.scan(mc, config, now);
        }
    }

    private static boolean crossed(long last, long now, long threshold) {
        return Math.floorDiv(last - threshold, 24000) < Math.floorDiv(now - threshold, 24000);
    }

    private void scanBiome(ClientLevel level, LocalPlayer player, SignalConfig.SignalData config, long now) {
        BlockPos sample = player.blockPosition();
        if (!level.hasChunkAt(sample)) return;
        String movement = UdpSender.movement(Minecraft.getInstance());
        String dimension = level.dimension().identifier().toString();
        boolean below = false;
        if (!movement.equals("ground") && (dimension.equals("minecraft:overworld") || dimension.equals("minecraft:the_end"))) {
            int surface = level.getHeight(Heightmap.Types.MOTION_BLOCKING_NO_LEAVES, sample.getX(), sample.getZ());
            // Do not sample through cave ceilings or the Nether roof, or into the End void.
            if (surface > level.getMinY() && player.getY() >= surface + 2) {
                sample = new BlockPos(sample.getX(), surface - 1, sample.getZ());
                below = true;
            }
        }
        var key = level.getBiome(sample).unwrapKey();
        if (key.isEmpty()) return;
        String id = key.get().identifier().toString();
        if (!id.equals(observedBiome)) {
            observedBiome = id;
            biomeSince = now;
        }
        if (lastBiome == null || !config.biomeMessage || !config.allowsBiome(id)) {
            lastBiome = id;
            return;
        }
        // Keep the latest stable candidate during cooldown instead of silently consuming it.
        if (!id.equals(lastBiome) && now >= nextBiome && now - biomeSince >= 2_000) {
            JsonObject event = UdpSender.event("biome_discovery");
            event.addProperty("biome", SignalBiomes.name(id).getString());
            event.addProperty("biome_id", id);
            event.addProperty("biome_view", below ? "below" : "inside");
            if (UdpSender.send(event)) lastBiome = id;
            nextBiome = now + 60_000;
        }
    }

    private void scanMobs(ClientLevel level, LocalPlayer player, SignalConfig.SignalData config, long now) {
        if (config.mobs.isEmpty()) {
            inside.clear();
            return;
        }
        Set<UUID> current = new HashSet<>();
        Entity nearest = null;
        double nearestDistance = Double.MAX_VALUE;
        int radius = config.radius;
        for (Entity entity : level.getEntities(player, player.getBoundingBox().inflate(radius))) {
            if (!entity.isAlive() || !SignalMobs.supported(entity.getType())) continue;
            double distance = entity.distanceToSqr(player);
            if (distance > radius * radius) continue;
            String id = BuiltInRegistries.ENTITY_TYPE.getKey(entity.getType()).toString();
            if (!config.allows(id) || activity.farming(id)) continue;
            current.add(entity.getUUID());
            if (!inside.contains(entity.getUUID()) && distance < nearestDistance) {
                nearest = entity;
                nearestDistance = distance;
            }
        }
        inside.retainAll(current);
        if (nearest != null && now >= nextMob) {
            JsonObject event = entityEvent("mob_proximity", nearest);
            event.addProperty("phase", "enter");
            event.addProperty("uuid", nearest.getUUID().toString());
            event.addProperty("distance", (int) Math.sqrt(nearestDistance));
            event.addProperty("ts", now);
            if (UdpSender.send(event)) inside.add(nearest.getUUID());
            nextMob = now + 10_000;
        }
    }

    private static JsonObject entityEvent(String type, Entity entity) {
        JsonObject event = UdpSender.event(type);
        event.addProperty("id", BuiltInRegistries.ENTITY_TYPE.getKey(entity.getType()).toString());
        event.addProperty("name", entity.getType().getDescription().getString());
        event.addProperty("mob_state", SignalMobs.state(entity));
        return event;
    }

    private static boolean isLocal(Entity entity) {
        return instance != null && entity.level().isClientSide() && entity == Minecraft.getInstance().player;
    }

    public static void chestPlaced(Player player, BlockPos pos) {
        if (player != null && isLocal(player)) instance.chests.placed(Minecraft.getInstance(), pos);
    }

    public static void chestContentsReceived(int containerId) {
        Minecraft mc = Minecraft.getInstance();
        if (instance != null && mc.player != null && mc.player.containerMenu.containerId == containerId) {
            instance.chests.contentsReceived(mc.player.containerMenu);
        }
    }

    public static void crafted(Player player) {
        if (!isLocal(player) || !SignalConfig.DATA.craftingMessage) return;
        long now = System.currentTimeMillis();
        if (now < instance.nextCraft) return;
        // MateEngine already applies its own 30% crafting reaction probability.
        UdpSender.send("crafted");
        instance.nextCraft = now + 1_000;
    }

    public static void finishedFood(LivingEntity entity) {
        if (!isLocal(entity) || !SignalConfig.DATA.eatMessage || !entity.getUseItem().has(DataComponents.FOOD)) return;
        long now = System.currentTimeMillis();
        if (now < instance.nextEat) return;
        instance.nextEat = now + 800;
        if (instance.rng.nextFloat() < 0.3f) UdpSender.send("eat");
    }

    public static void mobDied(LivingEntity victim) {
        if (instance == null || !victim.level().isClientSide()
                || !SignalMobs.combatReaction(victim)) return;
        LocalPlayer player = Minecraft.getInstance().player;
        DamageSource damage = victim.getLastDamageSource();
        if (player == null || damage == null) return;
        Entity direct = damage.getDirectEntity();
        boolean ours = damage.getEntity() == player || direct == player
                || (direct instanceof Projectile projectile && projectile.getOwner() == player);
        if (!ours) return;
        long now = System.currentTimeMillis();
        String id = SignalMobs.id(victim.getType());
        instance.activity.killed(player, id, now);
        if (!SignalConfig.DATA.killMessage || instance.activity.farming(id) || now < instance.nextKill) return;
        if (instance.rng.nextFloat() < 0.3f && UdpSender.send(entityEvent("kill_confirm", victim))) {
            instance.nextKill = now + 30_000;
        }
    }
}
