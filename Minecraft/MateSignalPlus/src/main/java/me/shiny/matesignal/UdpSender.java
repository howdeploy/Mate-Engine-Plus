package me.shiny.matesignal;

import com.google.gson.JsonObject;
import net.minecraft.client.Minecraft;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.InetSocketAddress;
import java.nio.charset.StandardCharsets;

public final class UdpSender {
    private static final Logger LOGGER = LoggerFactory.getLogger("matesignal");
    private static final InetSocketAddress TARGET = new InetSocketAddress("127.0.0.1", 32145);
    private static DatagramSocket socket;
    private static long lastError;

    public static JsonObject event(String type) {
        JsonObject json = new JsonObject();
        json.addProperty("type", type);
        Minecraft mc = Minecraft.getInstance();
        json.addProperty("language", mc.options.languageCode);
        if (mc.level != null) json.addProperty("dimension", mc.level.dimension().identifier().toString());
        json.addProperty("movement", movement(mc));
        return json;
    }

    static String movement(Minecraft mc) {
        if (mc.player == null) return "ground";
        if (mc.player.isFallFlying()) return "elytra";
        if (mc.player.getAbilities().flying) return "flying";
        if (mc.player.isPassenger() && mc.player.getRootVehicle().isFlyingVehicle()) return "mounted_flight";
        return "ground";
    }

    public static boolean send(String type) {
        return send(event(type));
    }

    public static synchronized boolean send(JsonObject event) {
        try {
            if (socket == null || socket.isClosed()) socket = new DatagramSocket();
            byte[] bytes = event.toString().getBytes(StandardCharsets.UTF_8);
            socket.send(new DatagramPacket(bytes, bytes.length, TARGET));
            return true;
        } catch (IOException e) {
            close();
            long now = System.currentTimeMillis();
            if (now - lastError > 60_000) {
                LOGGER.warn("Cannot send Mate Signal Plus event to {}", TARGET, e);
                lastError = now;
            }
            return false;
        }
    }

    public static synchronized void close() {
        if (socket != null) socket.close();
        socket = null;
    }
}
