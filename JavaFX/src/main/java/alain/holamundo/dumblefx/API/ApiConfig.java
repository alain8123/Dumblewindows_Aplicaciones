package alain.holamundo.dumblefx.API;

import alain.holamundo.dumblefx.I18n.I18n;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Properties;

public final class ApiConfig {
    private static class Loaded {
        private static final Properties PROPERTIES = load();
    }

    private ApiConfig() { }

    public static Path configurationFile() {
        String explicit = System.getProperty("dumblefx.config");
        if (explicit != null) return Path.of(explicit).toAbsolutePath();
        return Path.of("src", "main", "resources", "alain", "holamundo", "dumblefx", "config.properties")
                .toAbsolutePath();
    }

    private static InputStream bundledConfiguration() {
        InputStream resource = ApiConfig.class.getResourceAsStream("/alain/holamundo/dumblefx/config.properties");
        return resource == null ? ApiConfig.class.getResourceAsStream("/config.properties") : resource;
    }

    public static boolean hasConfiguration() throws IOException {
        if (Files.exists(configurationFile())) return true;
        if (System.getProperty("dumblefx.config") != null) return false;
        try (InputStream resource = bundledConfiguration()) {
            return resource != null;
        }
    }

    public static void saveInitialConfiguration(Properties properties) throws IOException {
        saveInitialConfiguration(properties, "");
    }

    public static void saveInitialConfiguration(Properties properties, String examples) throws IOException {
        Path file = configurationFile();
        Files.createDirectories(file.getParent());
        Path temporary = Files.createTempFile(file.getParent(), ".dumblefx-config-", ".tmp");
        try {
            try (var writer = Files.newBufferedWriter(temporary, StandardCharsets.UTF_8)) {
                properties.store(writer, "DumbleFX\n" + examples);
            }
            Files.move(temporary, file);
        } finally {
            Files.deleteIfExists(temporary);
        }
    }

    private static Properties load() {
        Properties properties = new Properties();
        String explicit = System.getProperty("dumblefx.config");
        Path file = configurationFile();
        try {
            if (Files.isRegularFile(file)) {
                try (var reader = Files.newBufferedReader(file, StandardCharsets.UTF_8)) {
                    properties.load(reader);
                }
            } else if (explicit != null) {
                throw new IOException(I18n.text("config.missing"));
            } else {
                try (var stream = bundledConfiguration()) {
                    if (stream != null) properties.load(new InputStreamReader(stream, StandardCharsets.UTF_8));
                }
            }
        } catch (IOException error) {
            throw new IllegalStateException(I18n.text("config.read"), error);
        }
        return properties;
    }

    private static String value(String name, String fallback) {
        String environment = System.getenv(name);
        return environment == null || environment.isBlank()
                ? Loaded.PROPERTIES.getProperty(name, fallback).trim() : environment.trim();
    }

    public static String endpoint(String operation, String defaultPath) {
        String base = value("DUMBLEFX_API_URL", "https://dumblewindows-api.tail6bccfa.ts.net")
                .replaceAll("/+$", "");
        String url = value(operation, base + defaultPath);
        if (url.isBlank()) url = base + defaultPath;
        if (!url.startsWith("https://") && !url.startsWith("http://")) {
            throw new IllegalStateException(I18n.text("config.url", operation));
        }
        return url;
    }

    public static String key() { return value("DUMBLEFX_API_KEY", ""); }
}
