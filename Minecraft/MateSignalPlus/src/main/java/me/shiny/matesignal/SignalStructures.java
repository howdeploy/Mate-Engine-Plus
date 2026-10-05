package me.shiny.matesignal;

import com.google.gson.JsonObject;
import net.minecraft.client.Minecraft;
import net.minecraft.core.BlockPos;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.world.phys.BlockHitResult;
import net.minecraft.world.phys.HitResult;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

final class SignalStructures {
    static final List<String> HINTS = List.of("archaeology", "trial_chambers", "ancient_city", "stronghold",
            "bastion", "fortress", "end_city", "monument", "abandoned_camp", "village", "mineshaft",
            "desert_pyramid", "jungle_temple", "woodland_mansion", "shipwreck", "dungeon", "igloo",
            "swamp_hut", "pillager_outpost", "ruined_portal", "ocean_ruins", "trail_ruins",
            "nether_fossil", "buried_treasure");
    private record Seen(BlockPos pos, long time) {}
    private final Map<String, Seen> visible = new HashMap<>();
    private final Map<String, Long> nextByHint = new HashMap<>();
    private BlockPos target;
    private long since, nextMessage, quietUntil;

    void reset() {
        visible.clear();
        nextByHint.clear();
        target = null;
        nextMessage = quietUntil = 0;
    }

    boolean scan(Minecraft mc, SignalConfig.SignalData config, long now) {
        if ((!config.structureMessage && !config.chestMessage) || mc.gui.screen() != null) return false;
        visible.values().removeIf(seen -> now - seen.time() > 20_000
                || mc.player.position().distanceToSqr(seen.pos().getX() + 0.5, seen.pos().getY() + 0.5, seen.pos().getZ() + 0.5) > 64);
        if (!(mc.hitResult instanceof BlockHitResult hit) || hit.getType() != HitResult.Type.BLOCK
                || mc.player.getEyePosition().distanceToSqr(hit.getLocation()) > 36
                || !mc.level.hasChunkAt(hit.getBlockPos())) {
            target = null;
            return now < quietUntil;
        }
        BlockPos pos = hit.getBlockPos();
        String id = BuiltInRegistries.BLOCK.getKey(mc.level.getBlockState(pos).getBlock()).toString();
        // Evidence is only a block the crosshair actually hit nearby, never a volume/structure scan.
        visible.put(id, new Seen(pos.immutable(), now));
        if (!config.structureMessage) return false;
        if (!pos.equals(target)) { target = pos.immutable(); since = now; }
        String biome = mc.level.getBiome(pos).unwrapKey().map(key -> key.identifier().toString()).orElse("");
        String hint = infer(id, mc.level.dimension().identifier().toString(), biome);
        if (hint == null || config.disabledStructures.contains(hint)) return now < quietUntil;
        if (now - since >= 750 && now >= nextMessage && now >= nextByHint.getOrDefault(hint, 0L)) {
            JsonObject event = UdpSender.event("structure_sighting");
            event.addProperty("structure_hint", hint);
            if (UdpSender.send(event)) {
                nextMessage = now + 90_000;
                nextByHint.put(hint, now + 600_000);
                quietUntil = now + 8_000;
            }
        }
        // A selected structure reaction replaces the block comment for the same visible clue.
        return true;
    }

    private boolean saw(String id) { return visible.containsKey("minecraft:" + id); }

    private boolean sawAny(String... ids) {
        for (String id : ids) if (saw(id)) return true;
        return false;
    }

    private boolean sawSuffix(String suffix) {
        return visible.keySet().stream().anyMatch(id -> vanillaSuffix(id, suffix));
    }

    private static boolean vanillaSuffix(String id, String suffix) {
        return id.startsWith("minecraft:") && id.endsWith(suffix);
    }

    private static boolean is(String id, String... blocks) {
        for (String block : blocks) if (id.equals("minecraft:" + block)) return true;
        return false;
    }

    private boolean shipwreckClues(String biome) {
        return coastal(biome) && sawSuffix("_planks")
                && visible.keySet().stream().anyMatch(SignalStructures::woodenTrapdoor)
                && (sawSuffix("_stairs") || sawSuffix("_slab"));
    }

    private static boolean woodenTrapdoor(String id) {
        return vanillaSuffix(id, "_trapdoor") && !id.equals("minecraft:iron_trapdoor")
                && !id.endsWith("copper_trapdoor");
    }

    private static boolean coastal(String biome) { return biome.contains("ocean") || biome.endsWith("beach"); }

    String chestHint(Minecraft mc, BlockPos chest, long now) {
        visible.values().removeIf(seen -> now - seen.time() > 20_000 || seen.pos().distSqr(chest) > 64);
        if (saw("spawner") && saw("mossy_cobblestone")) return "dungeon";
        String biome = mc.level.getBiome(chest).unwrapKey().map(key -> key.identifier().toString()).orElse("");
        String dimension = mc.level.dimension().identifier().toString();
        if (dimension.equals("minecraft:overworld") && shipwreckClues(biome)) return "shipwreck";
        for (String id : visible.keySet()) if (infer(id, dimension, biome) != null) return "structure";
        return "unknown";
    }

    private String infer(String id, String dimension, String biome) {
        if (id.equals("minecraft:trial_spawner") || id.equals("minecraft:vault")) return "trial_chambers";
        if (id.equals("minecraft:reinforced_deepslate")) return "ancient_city";
        if (id.equals("minecraft:end_portal_frame")) return "stronghold";
        if (dimension.equals("minecraft:the_nether")) {
            if (id.equals("minecraft:gilded_blackstone")) return "bastion";
            if ((id.equals("minecraft:nether_bricks") || id.equals("minecraft:nether_brick_fence"))
                    && saw("nether_bricks") && saw("nether_brick_fence")) return "fortress";
            if (is(id, "bone_block", "soul_sand", "soul_soil") && saw("bone_block")
                    && sawAny("soul_sand", "soul_soil")) return "nether_fossil";
        }
        if (dimension.equals("minecraft:the_end")
                && (id.startsWith("minecraft:purpur_") || id.equals("minecraft:end_stone_bricks"))
                && saw("end_stone_bricks") && (saw("purpur_block") || saw("purpur_pillar"))) return "end_city";
        if (dimension.equals("minecraft:overworld")
                && (id.equals("minecraft:prismarine_bricks") || id.equals("minecraft:dark_prismarine") || id.equals("minecraft:sea_lantern"))
                && saw("sea_lantern") && saw("prismarine_bricks") && saw("dark_prismarine")) return "monument";
        if (is(id, "obsidian", "crying_obsidian", "netherrack")
                && (dimension.equals("minecraft:overworld") || dimension.equals("minecraft:the_nether"))
                && saw("obsidian") && saw("crying_obsidian") && saw("netherrack")) return "ruined_portal";
        if (dimension.equals("minecraft:overworld")) {
            if (is(id, "campfire", "cobweb", "straw_bed", "chest", "barrel")
                    && saw("campfire") && saw("cobweb") && sawAny("straw_bed", "chest", "barrel")) return "abandoned_camp";
            if (is(id, "bell", "dirt_path") && saw("bell") && saw("dirt_path")) return "village";
            if (is(id, "rail", "cobweb", "oak_fence", "dark_oak_fence") && saw("rail")
                    && saw("cobweb") && sawAny("oak_fence", "dark_oak_fence")) return "mineshaft";
            if (is(id, "spawner", "mossy_cobblestone") && saw("spawner") && saw("mossy_cobblestone")) return "dungeon";
            if (is(id, "blue_terracotta", "orange_terracotta", "sandstone", "cut_sandstone", "chiseled_sandstone")
                    && saw("blue_terracotta") && saw("orange_terracotta")
                    && sawAny("sandstone", "cut_sandstone", "chiseled_sandstone")) return "desert_pyramid";
            if (is(id, "mossy_cobblestone", "tripwire_hook", "dispenser") && biome.contains("jungle")
                    && saw("mossy_cobblestone") && saw("tripwire_hook") && saw("dispenser")) return "jungle_temple";
            if (is(id, "dark_oak_planks", "birch_planks", "red_carpet") && saw("dark_oak_planks")
                    && saw("birch_planks") && saw("red_carpet")) return "woodland_mansion";
            if (is(id, "dark_oak_log", "birch_planks", "dark_oak_fence", "white_wall_banner")
                    && saw("dark_oak_log") && saw("birch_planks") && saw("dark_oak_fence")
                    && saw("white_wall_banner")) return "pillager_outpost";
            if (is(id, "snow_block", "red_bed", "furnace") && saw("snow_block")
                    && saw("red_bed") && saw("furnace")) return "igloo";
            if (is(id, "spruce_planks", "crafting_table", "cauldron", "water_cauldron", "potted_brown_mushroom")
                    && biome.contains("swamp") && saw("spruce_planks") && saw("crafting_table")
                    && sawAny("cauldron", "water_cauldron") && saw("potted_brown_mushroom")) return "swamp_hut";
            if ((is(id, "suspicious_gravel", "mud_bricks", "bricks") || vanillaSuffix(id, "terracotta"))
                    && saw("suspicious_gravel") && sawSuffix("terracotta")
                    && sawAny("mud_bricks", "bricks")) return "trail_ruins";
            if (coastal(biome) && (is(id, "suspicious_sand", "suspicious_gravel", "sandstone", "stone_bricks", "mossy_cobblestone"))
                    && (saw("suspicious_sand") && saw("sandstone")
                    || saw("suspicious_gravel") && sawAny("stone_bricks", "mossy_cobblestone"))) return "ocean_ruins";
            if ((vanillaSuffix(id, "_planks") || woodenTrapdoor(id) || vanillaSuffix(id, "_stairs") || vanillaSuffix(id, "_slab"))
                    && shipwreckClues(biome)) return "shipwreck";
            // ponytail: visible chest + shore material is only a possible cache, never proof of buried loot.
            if (is(id, "chest", "sand", "gravel") && coastal(biome) && saw("chest")
                    && sawAny("sand", "gravel") && !shipwreckClues(biome)) return "buried_treasure";
        }
        if (is(id, "suspicious_sand", "suspicious_gravel")) return "archaeology";
        return null;
    }
}
