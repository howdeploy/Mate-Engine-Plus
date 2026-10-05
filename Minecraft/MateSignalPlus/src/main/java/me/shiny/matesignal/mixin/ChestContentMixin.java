package me.shiny.matesignal.mixin;

import me.shiny.matesignal.MateSignal;
import net.minecraft.client.multiplayer.ClientPacketListener;
import net.minecraft.network.protocol.game.ClientboundContainerSetContentPacket;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(ClientPacketListener.class)
abstract class ChestContentMixin {
    @Inject(method = "handleContainerContent", at = @At("TAIL"))
    private void matesignal$contents(ClientboundContainerSetContentPacket packet, CallbackInfo ci) {
        MateSignal.chestContentsReceived(packet.containerId());
    }
}
