package alain.holamundo.dumblefx.API;

import java.io.IOException;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Properties;

public final class ApiConfig {
    private static final Properties PROPERTIES = load();

    private ApiConfig() { }

    private static Properties load() {
        Properties properties = new Properties();
        String explicit = System.getProperty("dumblefx.config");
        Path file = explicit == null ? Path.of("config.properties") : Path.of(explicit);
        if (explicit == null && !Files.isRegularFile(file)) {
            file = Path.of("src", "main", "resources", "config.properties");
        }
        if (explicit == null && !Files.isRegularFile(file)) {
            file = Path.of("src", "main", "resources", "alain", "holamundo", "dumblefx", "config.properties");
        }
        try {
            if (Files.isRegularFile(file)) {
                try (var reader = Files.newBufferedReader(file, StandardCharsets.UTF_8)) {
                    properties.load(reader);
                }
            } else if (explicit != null) {
                throw new IOException("No existe el archivo de configuración indicado.");
            } else {
                var resource = ApiConfig.class.getResourceAsStream("/alain/holamundo/dumblefx/config.properties");
                if (resource == null) resource = ApiConfig.class.getResourceAsStream("/config.properties");
                try (var stream = resource) {
                    if (stream != null) properties.load(new InputStreamReader(stream, StandardCharsets.UTF_8));
                }
            }
        } catch (IOException error) {
            throw new IllegalStateException("No se pudo leer config.properties.", error);
        }
        return properties;
    }

    private static String value(String name, String fallback) {
        String environment = System.getenv(name);
        return environment == null || environment.isBlank()
                ? PROPERTIES.getProperty(name, fallback).trim() : environment.trim();
    }

    public static String endpoint(String operation, String defaultPath) {
        String base = value("DUMBLEFX_API_URL", "https://dumblewindows-api.tail6bccfa.ts.net")
                .replaceAll("/+$", "");
        String url = value(operation, base + defaultPath);
        if (!url.startsWith("https://") && !url.startsWith("http://")) {
            throw new IllegalStateException("Configura una URL HTTP o HTTPS en " + operation);
        }
        return url;
    }

    public static String key() { return value("DUMBLEFX_API_KEY", ""); }
}
