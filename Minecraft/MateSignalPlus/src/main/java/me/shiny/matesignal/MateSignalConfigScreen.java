package me.shiny.matesignal;

import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphicsExtractor;
import net.minecraft.client.gui.components.AbstractWidget;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.ContainerObjectSelectionList;
import net.minecraft.client.gui.components.CycleButton;
import net.minecraft.client.gui.components.EditBox;
import net.minecraft.client.gui.components.Tooltip;
import net.minecraft.client.gui.components.events.GuiEventListener;
import net.minecraft.client.gui.narration.NarratableEntry;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.network.chat.Component;
import net.minecraft.world.entity.EntityType;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Locale;
import java.util.function.Consumer;

public class MateSignalConfigScreen extends Screen {
    private enum Category { LOCATIONS, MOBS, EVENTS, BLOCKS, STRUCTURES }
    private enum Filter { ALL, ENABLED, DISABLED }

    private final Screen parent;
    private final SignalConfig.SignalData draft = SignalConfig.copy();
    private Category category = Category.LOCATIONS;
    private Filter filter = Filter.ALL;
    private String query = "";
    private SettingsList list;
    private boolean saveFailed;
    private int toolbarY;

    public MateSignalConfigScreen(Screen parent) {
        super(tr("title"));
        this.parent = parent;
        SignalText.reload();
    }

    private static Component tr(String key, Object... args) {
        return SignalText.text("matesignal.config." + key, args);
    }

    @Override
    protected void init() {
        list = null;
        int contentWidth = Math.min(560, width - 24);
        int left = (width - contentWidth) / 2;
        int columns = contentWidth < 440 ? 3 : Category.values().length;
        int rows = (Category.values().length + columns - 1) / columns;
        int tabWidth = (contentWidth - 4 * (columns - 1)) / columns;
        toolbarY = 38 + rows * 24;
        for (Category tab : Category.values()) {
            Button button = Button.builder(tr(tab.name().toLowerCase(Locale.ROOT)), b -> {
                category = tab;
                query = "";
                filter = Filter.ALL;
                rebuildWidgets();
            }).bounds(left + (tab.ordinal() % columns) * (tabWidth + 4), 34 + (tab.ordinal() / columns) * 24, tabWidth, 20).build();
            button.active = tab != category;
            addRenderableWidget(button);
        }

        if (category != Category.EVENTS) {
            EditBox search = new EditBox(font, left, toolbarY, contentWidth - 108, 20, tr("search"));
            search.setHint(tr("search"));
            search.setValue(query);
            search.setResponder(value -> { query = value; refreshList(); });
            addRenderableWidget(search);
            addRenderableWidget(Button.builder(filterLabel(), b -> {
                filter = Filter.values()[(filter.ordinal() + 1) % Filter.values().length];
                b.setMessage(filterLabel());
                refreshList();
            }).bounds(left + contentWidth - 100, toolbarY, 100, 20).build());
        }

        list = addRenderableWidget(new SettingsList(Minecraft.getInstance(), width,
                Math.max(26, height - toolbarY - 68), toolbarY + 28, contentWidth));
        refreshList();
        int footerWidth = (contentWidth - 8) / 2;
        addRenderableWidget(Button.builder(tr("cancel"), b -> onClose())
                .bounds(left, height - 28, footerWidth, 20).build());
        addRenderableWidget(Button.builder(tr(saveFailed ? "save_failed" : "save"), b -> {
            if (SignalConfig.save(draft)) onClose();
            else { saveFailed = true; b.setMessage(tr("save_failed")); }
        }).bounds(left + footerWidth + 8, height - 28, footerWidth, 20).build());
    }

    private Component filterLabel() { return tr("filter." + filter.name().toLowerCase(Locale.ROOT)); }

    private boolean matches(String id, Component name, boolean enabled) {
        if (filter == Filter.ENABLED && !enabled || filter == Filter.DISABLED && enabled) return false;
        String needle = query.trim().toLowerCase(Locale.ROOT);
        return id.contains(needle) || name.getString().toLowerCase(Locale.ROOT).contains(needle);
    }

    private void refreshList() {
        if (list == null) return;
        list.clearRows();
        switch (category) {
            case LOCATIONS -> {
                toggle(tr("biomes"), draft.biomeMessage, v -> draft.biomeMessage = v, tr("biomes.hint"));
                List<String> ids = new ArrayList<>(SignalBiomes.ids(Minecraft.getInstance().level));
                for (String id : draft.disabledBiomes) if (id.contains(":") && !ids.contains(id)) ids.add(id);
                ids.sort(Comparator.comparing(id -> SignalBiomes.name(id).getString()));
                for (String id : ids) {
                    Component name = SignalBiomes.name(id);
                    if (!matches(id, name, draft.allowsBiome(id))) continue;
                    toggle(name, draft.allowsBiome(id), enabled -> {
                        draft.disabledBiomes.remove(id);
                        draft.disabledBiomes.remove(id.substring(id.indexOf(':') + 1));
                        if (!enabled) draft.disabledBiomes.add(id);
                    }, tr("biome.hint", name));
                }
            }
            case MOBS -> {
                Button radius = Button.builder(tr("radius", draft.radius), b -> {}).bounds(0, 0, 100, 20).build();
                radius.active = false;
                Button minus = Button.builder(Component.literal("−"), b -> {
                    draft.radius = Math.max(3, draft.radius - 1);
                    radius.setMessage(tr("radius", draft.radius));
                }).bounds(0, 0, 20, 20).build();
                Button plus = Button.builder(Component.literal("+"), b -> {
                    draft.radius = Math.min(64, draft.radius + 1);
                    radius.setMessage(tr("radius", draft.radius));
                }).bounds(0, 0, 20, 20).build();
                list.row(minus, radius, plus);
                List<EntityType<?>> types = new ArrayList<>();
                BuiltInRegistries.ENTITY_TYPE.forEach(t -> { if (SignalMobs.supported(t)) types.add(t); });
                types.sort(Comparator.comparing(t -> t.getDescription().getString()));
                for (EntityType<?> type : types) {
                    String id = SignalMobs.id(type);
                    if (!matches(id, type.getDescription(), draft.allows(id))) continue;
                    toggle(type.getDescription(), draft.allows(id), enabled -> {
                        draft.mobs.remove(id);
                        draft.mobs.remove(id.substring(id.indexOf(':') + 1));
                        if (enabled) draft.mobs.add(id);
                    }, tr("mob.hint", type.getDescription()));
                }
            }
            case EVENTS -> {
                event("day", draft.dayMessage, v -> draft.dayMessage = v);
                event("night", draft.nightMessage, v -> draft.nightMessage = v);
                event("health", draft.lowHealthMessage, v -> draft.lowHealthMessage = v);
                event("hunger", draft.lowHungerMessage, v -> draft.lowHungerMessage = v);
                event("death", draft.deathMessage, v -> draft.deathMessage = v);
                event("rain", draft.rainStartMessage, v -> draft.rainStartMessage = v);
                event("air", draft.drowningHalfMessage, v -> draft.drowningHalfMessage = v);
                event("sleep", draft.sleepMessage, v -> draft.sleepMessage = v);
                event("craft", draft.craftingMessage, v -> draft.craftingMessage = v);
                event("eat", draft.eatMessage, v -> draft.eatMessage = v);
                event("kill", draft.killMessage, v -> draft.killMessage = v);
                event("farm", draft.farmMessage, v -> draft.farmMessage = v);
                event("crop_farm", draft.cropFarmMessage, v -> draft.cropFarmMessage = v);
                event("tree_farm", draft.treeFarmMessage, v -> draft.treeFarmMessage = v);
                event("chest", draft.chestMessage, v -> draft.chestMessage = v);
                event("chest_mess", draft.chestMessMessage, v -> draft.chestMessMessage = v);
                event("durability_low", draft.durabilityLowMessage, v -> draft.durabilityLowMessage = v);
                event("durability_critical", draft.durabilityCriticalMessage, v -> draft.durabilityCriticalMessage = v);
            }
            case BLOCKS -> {
                toggle(tr("block_reactions"), draft.blockMessage, v -> draft.blockMessage = v, tr("blocks.hint"));
                List<String> ids = new ArrayList<>(SignalBlocks.IDS);
                ids.sort(Comparator.comparing(id -> SignalBlocks.name(id).getString()));
                for (String id : ids) {
                    Component name = SignalBlocks.name(id);
                    if (!matches(id, name, draft.allowsBlock(id))) continue;
                    toggle(name, draft.allowsBlock(id), enabled -> {
                        draft.disabledBlocks.remove(id);
                        draft.disabledBlocks.remove(id.substring(id.indexOf(':') + 1));
                        if (!enabled) draft.disabledBlocks.add(id);
                    }, tr("block.hint", name));
                }
            }
            case STRUCTURES -> {
                toggle(tr("structure_reactions"), draft.structureMessage, v -> draft.structureMessage = v, tr("structures.hint"));
                for (String hint : SignalStructures.HINTS) {
                    Component name = tr("structure." + hint);
                    boolean enabled = !draft.disabledStructures.contains(hint);
                    if (!matches(hint, name, enabled)) continue;
                    toggle(name, enabled, value -> {
                        draft.disabledStructures.remove(hint);
                        if (!value) draft.disabledStructures.add(hint);
                    }, tr("structure." + hint + ".hint"));
                }
            }
        }
        list.setScrollAmount(0);
    }

    private void event(String key, boolean value, Consumer<Boolean> change) {
        toggle(tr("event." + key), value, change, tr("event." + key + ".hint"));
    }

    private void toggle(Component name, boolean value, Consumer<Boolean> change, Component hint) {
        CycleButton<Boolean> button = CycleButton.booleanBuilder(tr("enabled"), tr("disabled"), value)
                .create(0, 0, 100, 20, name, (b, v) -> change.accept(v));
        button.setTooltip(Tooltip.create(hint));
        list.row(button);
    }

    @Override
    public void onClose() { Minecraft.getInstance().gui.setScreen(parent); }

    @Override
    public void extractRenderState(GuiGraphicsExtractor g, int mx, int my, float pt) {
        g.fill(0, 0, width, height, 0xFF101010);
        super.extractRenderState(g, mx, my, pt);
        g.centeredText(font, title, width / 2, 14, 0xFFFFFFFF);
        if (category == Category.EVENTS) g.centeredText(font, tr("events.hint"), width / 2, toolbarY + 6, 0xFFAAAAAA);
    }

    private static final class SettingsList extends ContainerObjectSelectionList<Row> {
        private final int rowWidth;

        SettingsList(Minecraft mc, int width, int height, int top, int rowWidth) {
            super(mc, width, height, top, 26);
            this.rowWidth = rowWidth;
            centerListVertically = false;
        }

        void clearRows() { clearEntries(); }
        void row(AbstractWidget... widgets) { addEntry(new Row(widgets)); }
        @Override public int getRowWidth() { return rowWidth; }
    }

    private static final class Row extends ContainerObjectSelectionList.Entry<Row> {
        private final List<AbstractWidget> widgets;

        Row(AbstractWidget... widgets) { this.widgets = List.of(widgets); }

        @Override
        public void extractContent(GuiGraphicsExtractor g, int mx, int my, boolean hovered, float pt) {
            int x = getContentX();
            for (int i = 0; i < widgets.size(); i++) {
                AbstractWidget widget = widgets.get(i);
                int w = widgets.size() == 1 ? getContentWidth() : (i == 1 ? getContentWidth() - 48 : 20);
                widget.setWidth(w);
                widget.setPosition(x, getContentY());
                widget.extractRenderState(g, mx, my, pt);
                x += w + 4;
            }
        }

        @Override public List<? extends GuiEventListener> children() { return widgets; }
        @Override public List<? extends NarratableEntry> narratables() { return widgets; }
    }
}
