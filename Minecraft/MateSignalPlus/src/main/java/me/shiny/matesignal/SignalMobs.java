package me.shiny.matesignal;

import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.EntityType;
import net.minecraft.world.entity.MobCategory;
import net.minecraft.world.entity.monster.cubemob.SulfurCube;

import java.util.Set;

final class SignalMobs {
    private static final Set<String> ENCOUNTERS = Set.of(
            "minecraft:happy_ghast", "minecraft:copper_golem", "minecraft:nautilus");
    private static final Set<String> MOUNTS = Set.of(
            "minecraft:camel_husk", "minecraft:zombie_nautilus", "minecraft:zombie_horse");

    static String id(EntityType<?> type) {
        return BuiltInRegistries.ENTITY_TYPE.getKey(type).toString();
    }

    static boolean supported(EntityType<?> type) {
        return type.getCategory() == MobCategory.MONSTER || ENCOUNTERS.contains(id(type));
    }

    static String state(Entity entity) {
        if (entity instanceof SulfurCube cube) {
            if (cube.isPrimed()) return "primed";
            if (cube.hasBodyItem()) return "filled";
            return cube.isBaby() ? "small" : "empty";
        }
        if (MOUNTS.contains(id(entity.getType()))) {
            return entity.getPassengers().stream().anyMatch(rider ->
                    rider.getType().getCategory() == MobCategory.MONSTER) ? "hostile_rider" : "unridden";
        }
        return "";
    }

    static boolean combatReaction(Entity entity) {
        if (entity.getType().getCategory() != MobCategory.MONSTER || entity instanceof SulfurCube) return false;
        return !MOUNTS.contains(id(entity.getType())) || state(entity).equals("hostile_rider");
    }
}
