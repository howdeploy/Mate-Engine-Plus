package me.shiny.matesignal;

import com.google.gson.JsonObject;
import net.minecraft.client.Minecraft;
import net.minecraft.core.BlockPos;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.world.inventory.AbstractContainerMenu;
import net.minecraft.world.inventory.ChestMenu;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.level.block.ChestBlock;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.level.block.state.properties.ChestType;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;

final class SignalChests {
    private static final Set<String> VALUABLES = Set.of("minecraft:diamond", "minecraft:diamond_block",
            "minecraft:netherite_ingot", "minecraft:netherite_scrap", "minecraft:ancient_debris",
            "minecraft:enchanted_golden_apple", "minecraft:enchanted_book", "minecraft:elytra",
            "minecraft:totem_of_undying", "minecraft:heavy_core", "minecraft:mace", "minecraft:heart_of_the_sea");
    private final Map<BlockPos, String> placed = new LinkedHashMap<>();
    private final Map<BlockPos, Long> nextByChest = new LinkedHashMap<>();
    private AbstractContainerMenu activeMenu, syncedMenu;
    private BlockPos pendingPos, openedPos, chestKey;
    private String origin = "unknown";
    private long pendingAt, openedAt, nextMessage;
    private boolean handled;

    void reset() {
        placed.clear();
        nextByChest.clear();
        activeMenu = syncedMenu = null;
        pendingPos = openedPos = chestKey = null;
        nextMessage = 0;
    }

    void placed(Minecraft mc, BlockPos pos) {
        BlockState state = mc.level.getBlockState(pos);
        if (!(state.getBlock() instanceof ChestBlock)) return;
        placed.put(pos.immutable(), BuiltInRegistries.BLOCK.getKey(state.getBlock()).toString());
        trim(placed);
    }

    void broken(BlockPos pos) { placed.remove(pos); nextByChest.remove(pos); }

    void clicked(Minecraft mc, BlockPos pos, long now) {
        pendingPos = null;
        if (mc.level.getBlockState(pos).getBlock() instanceof ChestBlock) {
            pendingPos = pos.immutable();
            pendingAt = now;
        }
    }

    void contentsReceived(AbstractContainerMenu menu) { syncedMenu = menu; }

    boolean scan(Minecraft mc, SignalConfig.SignalData config, SignalStructures structures, long now) {
        AbstractContainerMenu menu = mc.player.containerMenu;
        if (menu != activeMenu) {
            activeMenu = menu;
            openedAt = now;
            handled = false;
            openedPos = null;
            if (menu instanceof ChestMenu && pendingPos != null && now - pendingAt <= 10_000
                    && mc.level.hasChunkAt(pendingPos)) {
                BlockState state = mc.level.getBlockState(pendingPos);
                if (state.getBlock() instanceof ChestBlock) {
                    openedPos = pendingPos;
                    BlockPos other = state.getValue(ChestBlock.TYPE) == ChestType.SINGLE ? openedPos
                            : ChestBlock.getConnectedBlockPos(openedPos, state);
                    chestKey = openedPos.asLong() < other.asLong() ? openedPos : other;
                    String id = BuiltInRegistries.BLOCK.getKey(state.getBlock()).toString();
                    origin = id.equals(placed.get(openedPos)) || id.equals(placed.get(other)) ? "player_placed"
                            : structures.chestHint(mc, openedPos, now);
                }
            }
            // Do not discard a click while still waiting for its server-opened menu.
            if (menu instanceof ChestMenu) pendingPos = null;
        }
        if (!config.chestMessage || handled || openedPos == null || menu != syncedMenu
                || !(menu instanceof ChestMenu chest) || mc.gui.screen() == null || now - openedAt < 750) return false;
        int size = chest.getContainer().getContainerSize();
        if (size != 27 && size != 54) return false;
        handled = true;
        if (now < nextMessage || now < nextByChest.getOrDefault(chestKey, 0L)) return false;

        int occupied = 0, mergeable = 0;
        boolean valuables = false;
        Set<String> types = new HashSet<>();
        List<StackGroup> groups = new ArrayList<>();
        for (int i = 0; i < size; i++) {
            ItemStack stack = chest.getContainer().getItem(i);
            if (stack.isEmpty()) continue;
            occupied++;
            String id = BuiltInRegistries.ITEM.getKey(stack.getItem()).toString();
            types.add(id);
            valuables |= VALUABLES.contains(id);
            if (!stack.isStackable()) continue;
            StackGroup group = null;
            for (StackGroup existing : groups) {
                if (ItemStack.isSameItemSameComponents(existing.sample, stack)) { group = existing; break; }
            }
            if (group == null) { group = new StackGroup(stack); groups.add(group); }
            group.count += stack.getCount();
            group.slots++;
        }
        for (StackGroup group : groups) {
            int maximum = group.sample.getMaxStackSize();
            if (maximum > 0) mergeable += group.slots - (group.count + maximum - 1) / maximum;
        }
        boolean messy = size == 54 && occupied >= 36 && types.size() >= 18 && mergeable >= 4
                && (origin.equals("unknown") || origin.equals("player_placed"));
        String reaction = occupied == 0 ? "empty" : messy && config.chestMessMessage ? "messy"
                : occupied * 10 >= size * 9 ? "full" : valuables ? "valuables"
                : occupied >= 9 && types.size() <= 3 ? "stockpile" : "mixed";
        JsonObject event = UdpSender.event("chest_contents");
        event.addProperty("chest_context", origin);
        event.addProperty("chest_reaction", reaction);
        event.addProperty("chest_slots", size);
        event.addProperty("chest_occupied", occupied);
        event.addProperty("chest_types", types.size());
        event.addProperty("chest_mergeable_slots", Math.max(0, mergeable));
        if (!UdpSender.send(event)) return false;
        nextMessage = now + 90_000;
        nextByChest.put(chestKey, now + 600_000);
        trim(nextByChest);
        return true;
    }

    private static final class StackGroup {
        final ItemStack sample;
        int count, slots;
        StackGroup(ItemStack stack) { sample = stack; }
    }

    private static void trim(Map<BlockPos, ?> memory) {
        while (memory.size() > 512) memory.remove(memory.keySet().iterator().next());
    }
}
