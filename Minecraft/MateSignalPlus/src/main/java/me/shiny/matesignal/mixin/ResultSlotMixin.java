package me.shiny.matesignal.mixin;

import me.shiny.matesignal.MateSignal;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.inventory.ResultSlot;
import net.minecraft.world.item.ItemStack;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(ResultSlot.class)
abstract class ResultSlotMixin {
    @Inject(method = "onTake", at = @At("TAIL"))
    private void matesignal$crafted(Player player, ItemStack stack, CallbackInfo ci) {
        MateSignal.crafted(player);
    }
}
