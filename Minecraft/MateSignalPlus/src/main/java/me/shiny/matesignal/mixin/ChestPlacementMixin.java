package me.shiny.matesignal.mixin;

import me.shiny.matesignal.MateSignal;
import net.minecraft.world.InteractionResult;
import net.minecraft.world.item.BlockItem;
import net.minecraft.world.item.context.BlockPlaceContext;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(BlockItem.class)
abstract class ChestPlacementMixin {
    @Inject(method = "place", at = @At("RETURN"))
    private void matesignal$placed(BlockPlaceContext context, CallbackInfoReturnable<InteractionResult> ci) {
        if (ci.getReturnValue().consumesAction()
                && context.getLevel().getBlockState(context.getClickedPos()).is(((BlockItem) (Object) this).getBlock())) {
            MateSignal.chestPlaced(context.getPlayer(), context.getClickedPos());
        }
    }
}
