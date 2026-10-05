package me.shiny.matesignal;

import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.core.registries.Registries;
import net.minecraft.network.chat.Component;

import java.util.ArrayList;
import java.util.List;

final class SignalBiomes {
    // The title-screen config has no world registry. In a world, use its actual biome IDs.
    private static final String VANILLA = "badlands bamboo_jungle basalt_deltas beach birch_forest cherry_grove "
            + "cold_ocean crimson_forest dappled_forest dark_forest deep_cold_ocean deep_dark deep_frozen_ocean "
            + "deep_lukewarm_ocean deep_ocean desert dripstone_caves end_barrens end_highlands end_midlands "
            + "eroded_badlands flower_forest forest frozen_ocean frozen_peaks frozen_river grove ice_spikes "
            + "jagged_peaks jungle lukewarm_ocean lush_caves mangrove_swamp meadow mushroom_fields nether_wastes "
            + "ocean old_growth_birch_forest old_growth_pine_taiga old_growth_spruce_taiga pale_garden plains river "
            + "savanna savanna_plateau small_end_islands snowy_beach snowy_plains snowy_slopes snowy_taiga "
            + "soul_sand_valley sparse_jungle stony_peaks stony_shore sulfur_caves sunflower_plains swamp taiga "
            + "the_end the_void warm_ocean warped_forest windswept_forest windswept_gravelly_hills windswept_hills "
            + "windswept_savanna wooded_badlands";

    static List<String> ids(ClientLevel level) {
        if (level != null) return level.registryAccess().lookupOrThrow(Registries.BIOME).keySet()
                .stream().map(Object::toString).sorted().toList();
        List<String> ids = new ArrayList<>();
        for (String name : VANILLA.split(" ")) ids.add("minecraft:" + name);
        return ids;
    }

    static Component name(String id) {
        return Component.translatableWithFallback("biome." + id.replace(':', '.'),
                id.substring(id.indexOf(':') + 1).replace('_', ' '));
    }
}
