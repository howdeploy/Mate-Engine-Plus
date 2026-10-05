package me.shiny.matesignal;

import com.google.gson.JsonObject;
import net.minecraft.client.Minecraft;
import net.minecraft.core.BlockPos;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.network.chat.Component;
import net.minecraft.world.phys.BlockHitResult;
import net.minecraft.world.phys.HitResult;

import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

final class SignalBlocks {
    static final List<String> IDS = Arrays.stream(("diamond_ore deepslate_diamond_ore diamond_block "
            + "emerald_ore deepslate_emerald_ore emerald_block ancient_debris "
            + "gold_ore deepslate_gold_ore nether_gold_ore iron_ore deepslate_iron_ore "
            + "copper_ore deepslate_copper_ore coal_ore deepslate_coal_ore "
            + "lapis_ore deepslate_lapis_ore redstone_ore deepslate_redstone_ore nether_quartz_ore "
            + "sculk sculk_vein sculk_sensor calibrated_sculk_sensor sculk_shrieker sculk_catalyst "
            + "budding_amethyst spawner trial_spawner vault").split(" "))
            .map(id -> "minecraft:" + id).toList();
    private final Map<String, Long> nextByKind = new HashMap<>();
    private BlockPos target;
    private String targetId;
    private long since, nextMessage;
    private boolean announced;

    static Component name(String id) { return Component.translatable("block." + id.replace(':', '.')); }

    void reset() {
        target = null;
        targetId = null;
        nextByKind.clear();
        nextMessage = 0;
    }

    void scan(Minecraft mc, SignalConfig.SignalData config, long now) {
        if (!config.blockMessage || mc.gui.screen() != null || !(mc.hitResult instanceof BlockHitResult hit)
                || hit.getType() != HitResult.Type.BLOCK || !mc.level.hasChunkAt(hit.getBlockPos())) {
            target = null;
            return;
        }
        BlockPos pos = hit.getBlockPos();
        String id = BuiltInRegistries.BLOCK.getKey(mc.level.getBlockState(pos).getBlock()).toString();
        if (!IDS.contains(id) || !config.allowsBlock(id)) {
            target = null;
            return;
        }
        if (!pos.equals(target) || !id.equals(targetId)) {
            target = pos.immutable();
            targetId = id;
            since = now;
            announced = false;
        }
        String kind = id.replace("minecraft:deepslate_", "minecraft:");
        if (announced || now - since < 750 || now < nextMessage || now < nextByKind.getOrDefault(kind, 0L)) return;
        JsonObject event = UdpSender.event("block_sighting");
        event.addProperty("block_id", id);
        event.addProperty("block_name", name(id).getString());
        if (UdpSender.send(event)) {
            announced = true;
            nextMessage = now + 30_000;
            nextByKind.put(kind, now + 300_000);
        }
    }
}
