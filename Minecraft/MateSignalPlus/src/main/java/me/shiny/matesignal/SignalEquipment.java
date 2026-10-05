package me.shiny.matesignal;

import com.google.gson.JsonObject;
import net.minecraft.client.player.LocalPlayer;
import net.minecraft.core.component.DataComponents;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.tags.ItemTags;
import net.minecraft.world.entity.EquipmentSlot;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

final class SignalEquipment {
    private static final List<EquipmentSlot> SLOTS = List.of(EquipmentSlot.MAINHAND, EquipmentSlot.OFFHAND,
            EquipmentSlot.HEAD, EquipmentSlot.CHEST, EquipmentSlot.LEGS, EquipmentSlot.FEET);
    private final List<Wear> items = new ArrayList<>();
    private long nextMessage;

    private static final class Wear {
        final ItemStack identity;
        double lowest;
        boolean lowAnnounced, criticalAnnounced;

        Wear(ItemStack stack) { identity = stack.copy(); }
    }

    void reset() { items.clear(); nextMessage = 0; }

    private Wear track(ItemStack stack) {
        for (Wear item : items) {
            if (ItemStack.matchesIgnoringComponents(item.identity, stack, type -> type == DataComponents.DAMAGE)) return item;
        }
        Wear item = new Wear(stack);
        item.lowest = 1;
        items.add(item);
        return item;
    }

    private static double remaining(ItemStack stack) {
        return Math.max(0, stack.getMaxDamage() - stack.getDamageValue()) / (double) stack.getMaxDamage();
    }

    boolean scan(LocalPlayer player, SignalConfig.SignalData config, long now) {
        Set<Wear> present = new HashSet<>();
        for (Wear item : items) item.lowest = 1;
        // Keep warning memory when an item moves between hands, armour slots and the backpack.
        // ponytail: indistinguishable copies share warning memory; unique item IDs would require server support.
        for (int i = 0; i < player.getInventory().getContainerSize(); i++) {
            ItemStack stack = player.getInventory().getItem(i);
            if (!stack.isDamageableItem() || stack.getMaxDamage() <= 0) continue;
            Wear item = track(stack);
            present.add(item);
            item.lowest = Math.min(item.lowest, remaining(stack));
        }
        items.removeIf(item -> !present.contains(item));
        for (Wear item : items) {
            // Hysteresis prevents Mending around a threshold from spamming warnings.
            if (item.lowest > 0.25) item.lowAnnounced = false;
            if (item.lowest > 0.05) item.criticalAnnounced = false;
        }
        // Leave the warning on screen before less urgent block/farm comments.
        if (now < nextMessage) return true;
        ItemStack candidate = null;
        Wear warning = null;
        EquipmentSlot candidateSlot = null;
        double least = 2;
        int threshold = 0;
        for (EquipmentSlot slot : SLOTS) {
            ItemStack stack = player.getItemBySlot(slot);
            if (!stack.isDamageableItem() || stack.getMaxDamage() <= 0) continue;
            Wear item = track(stack);
            double left = remaining(stack);
            int level = left <= 0.03 && config.durabilityCriticalMessage && !item.criticalAnnounced ? 3
                    : left <= 0.20 && config.durabilityLowMessage && !item.lowAnnounced ? 20 : 0;
            if (level == 0 || left >= least) continue;
            candidate = stack;
            candidateSlot = slot;
            warning = item;
            least = left;
            threshold = level;
        }
        if (candidate == null) return false;
        JsonObject event = UdpSender.event("equipment_durability");
        event.addProperty("item_id", BuiltInRegistries.ITEM.getKey(candidate.getItem()).toString());
        event.addProperty("item_name", candidate.getHoverName().getString());
        event.addProperty("equipment_kind", kind(candidate, candidateSlot));
        event.addProperty("equipment_slot", candidateSlot.getName());
        event.addProperty("durability_remaining", Math.max(0, candidate.getMaxDamage() - candidate.getDamageValue()));
        event.addProperty("durability_max", candidate.getMaxDamage());
        event.addProperty("durability_threshold", threshold);
        if (!UdpSender.send(event)) return false;
        warning.lowAnnounced = true;
        if (threshold == 3) warning.criticalAnnounced = true;
        nextMessage = now + 3_000;
        return true;
    }

    private static String kind(ItemStack stack, EquipmentSlot slot) {
        if (stack.is(Items.ELYTRA)) return "elytra";
        if (stack.is(ItemTags.SWORDS)) return "sword";
        if (stack.is(ItemTags.PICKAXES)) return "pickaxe";
        if (slot.isArmor()) return slot.getName();
        return "equipment";
    }
}
