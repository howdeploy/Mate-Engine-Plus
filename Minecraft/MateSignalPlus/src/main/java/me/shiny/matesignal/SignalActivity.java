package me.shiny.matesignal;

import com.google.gson.JsonObject;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.world.phys.Vec3;
import net.minecraft.tags.BlockTags;
import net.minecraft.world.level.block.Blocks;
import net.minecraft.world.level.block.CocoaBlock;
import net.minecraft.world.level.block.CropBlock;
import net.minecraft.world.level.block.NetherWartBlock;
import net.minecraft.world.level.block.state.BlockState;

import java.util.ArrayDeque;
import java.util.HashMap;
import java.util.Map;

final class SignalActivity {
    private record Kill(String id, long time) {}
    private final ArrayDeque<Kill> kills = new ArrayDeque<>();
    private Vec3 anchor;
    private float lastHealth = Float.NaN;
    private String farmMob;
    private long encounterSince, nextMessage;
    private String harvestKind;
    private Vec3 harvestAnchor;
    private int harvestCount;
    private long harvestSince, lastHarvest;

    void reset() {
        clearEncounter();
        lastHealth = Float.NaN;
        nextMessage = 0;
        harvestKind = null;
        harvestCount = 0;
    }

    private void clearEncounter() {
        kills.clear();
        anchor = null;
        farmMob = null;
    }

    void observe(LocalPlayer player, long now) {
        float health = player.getHealth();
        if (health <= 6 || health < lastHealth || anchor != null && anchor.distanceToSqr(player.position()) > 64) {
            clearEncounter();
            if (health <= 6 || health < lastHealth) { harvestKind = null; harvestCount = 0; }
        }
        lastHealth = health;
        while (!kills.isEmpty() && now - kills.peekFirst().time() > 45_000) kills.removeFirst();
        farmMob = null;
        if (kills.isEmpty()) anchor = null;
        if (kills.size() < 12 || now - encounterSince < 15_000) return;
        Map<String, Integer> counts = new HashMap<>();
        for (Kill kill : kills) counts.merge(kill.id(), 1, Integer::sum);
        for (var entry : counts.entrySet()) {
            if (entry.getValue() * 5 >= kills.size() * 4) {
                farmMob = entry.getKey();
                break;
            }
        }
    }

    void killed(LocalPlayer player, String id, long now) {
        observe(player, now);
        if (player.getHealth() <= 6) return;
        if (anchor == null) {
            anchor = player.position();
            encounterSince = now;
        }
        kills.addLast(new Kill(id, now));
        // Bound memory even when a farm kills hundreds of mobs per second.
        while (kills.size() > 256) kills.removeFirst();
        observe(player, now);
    }

    boolean farming(String mobId) { return farmMob != null && farmMob.equals(mobId); }

    void harvested(LocalPlayer player, BlockState state, long now) {
        if (player.getAbilities().instabuild || player.getHealth() <= 6) return;
        String kind = null;
        if (state.getBlock() instanceof CropBlock crop && crop.isMaxAge(state)
                || state.is(Blocks.NETHER_WART) && state.getValue(NetherWartBlock.AGE) == NetherWartBlock.MAX_AGE
                || state.is(Blocks.COCOA) && state.getValue(CocoaBlock.AGE) == CocoaBlock.MAX_AGE
                || state.is(Blocks.SUGAR_CANE) || state.is(Blocks.BAMBOO)
                || state.is(Blocks.MELON) || state.is(Blocks.PUMPKIN)) kind = "crops";
        else if (state.is(BlockTags.LOGS)) kind = "wood";
        if (kind == null) return;
        if (!kind.equals(harvestKind) || now - lastHarvest > 15_000
                || harvestAnchor == null || harvestAnchor.distanceToSqr(player.position()) > 144) {
            harvestKind = kind;
            harvestAnchor = player.position();
            harvestSince = now;
            harvestCount = 0;
        }
        harvestCount++;
        lastHarvest = now;
    }

    boolean announce(SignalConfig.SignalData config, long now) {
        if (now < nextMessage) return false;
        JsonObject event;
        if (farmMob != null && config.farmMessage) {
            event = UdpSender.event("xp_farm");
            event.addProperty("id", farmMob);
        } else if (now - lastHarvest <= 10_000 && now - harvestSince >= 3_000
                && ("crops".equals(harvestKind) && config.cropFarmMessage && harvestCount >= 6
                || "wood".equals(harvestKind) && config.treeFarmMessage && harvestCount >= 12)) {
            event = UdpSender.event("farm_activity");
            event.addProperty("activity", harvestKind);
        } else return false;
        if (!UdpSender.send(event)) return false;
        nextMessage = now + 180_000;
        return true;
    }
}
