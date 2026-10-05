package me.shiny.matesignal.mixin;

import me.shiny.matesignal.MateSignal;
import net.minecraft.world.entity.EntityEvent;
import net.minecraft.world.entity.LivingEntity;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(LivingEntity.class)
abstract class LivingEntityMixin {
    @Inject(method = "completeUsingItem", at = @At(value = "INVOKE",
            target = "Lnet/minecraft/world/item/ItemStack;finishUsingItem(Lnet/minecraft/world/level/Level;Lnet/minecraft/world/entity/LivingEntity;)Lnet/minecraft/world/item/ItemStack;"))
    private void matesignal$finishedFood(CallbackInfo ci) {
        MateSignal.finishedFood((LivingEntity) (Object) this);
    }

    @Inject(method = "handleEntityEvent", at = @At("HEAD"))
    private void matesignal$mobDied(byte event, CallbackInfo ci) {
        if (event == EntityEvent.DEATH) MateSignal.mobDied((LivingEntity) (Object) this);
    }
}
