package Dumblewindows.javafx.dumblefx.API;

import Dumblewindows.javafx.dumblefx.i18n.I18n;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.AtomicMoveNotSupportedException;
import java.nio.file.StandardCopyOption;
import java.util.Properties;

/**
 * Resuelve la configuración de conexión a partir del entorno y de un archivo de propiedades.
 * Las variables de entorno no vacías tienen prioridad. La propiedad dumblefx.config
 * permite elegir un archivo externo; sin ella se usa la ruta del proyecto o un recurso incluido.
 *
 * @author Alain Paunero
 */
public final class ApiConfig {
    /** Propiedades publicadas como una instantánea tras leer o guardar la configuración. */
    private static volatile Properties cachedProperties;

    /**
     * Obtiene la instantánea activa y permite reintentar si una lectura anterior falló.
     *
     * @return propiedades de conexión compartidas, sin modificarlas
     */
    private static synchronized Properties properties() {
        if (cachedProperties == null) cachedProperties = load();
        return cachedProperties;
    }

    /**
     * Impide crear instancias de esta utilidad.
     */
    private ApiConfig() { }

    /**
     * Determina la ruta absoluta del archivo externo de configuración.
     *
     * @return ruta explícita de dumblefx.config o ruta predeterminada del proyecto
     */
    public static Path configurationFile() {
        String explicit = System.getProperty("dumblefx.config");
        if (explicit != null) return Path.of(explicit).toAbsolutePath();
        return Path.of("src", "main", "resources", "Dumblewindows", "javafx", "dumblefx", "config.properties")
                .toAbsolutePath();
    }

    /**
     * Busca una configuración incluida en el classpath, primero en el paquete y luego en la raíz.
     *
     * @return flujo del recurso encontrado, o null si no existe
     */
    private static InputStream bundledConfiguration() {
        InputStream resource = ApiConfig.class.getResourceAsStream("/Dumblewindows/javafx/dumblefx/config.properties");
        return resource == null ? ApiConfig.class.getResourceAsStream("/config.properties") : resource;
    }

    /**
     * Comprueba la existencia de un archivo o recurso, sin validar su contenido.
     * Una ruta explícita inexistente no permite recurrir a la configuración empaquetada.
     *
     * @return true si se encuentra la configuración correspondiente
     * @throws IOException si no se puede leer o guardar la configuración
     */
    public static boolean hasConfiguration() throws IOException {
        if (Files.exists(configurationFile())) return true;
        if (System.getProperty("dumblefx.config") != null) return false;
        try (InputStream resource = bundledConfiguration()) {
            return resource != null;
        }
    }

    /**
     * Guarda la configuración inicial en UTF-8 mediante un archivo temporal.
     * No reemplaza un archivo de destino existente y elimina el temporal al terminar.
     *
     * @param properties propiedades de conexión que se van a guardar
     * @throws IOException si no se puede leer o guardar la configuración
     */
    public static void saveInitialConfiguration(Properties properties) throws IOException {
        saveInitialConfiguration(properties, "");
    }

    /**
     * Guarda la configuración inicial en UTF-8 mediante un archivo temporal.
     * No reemplaza un archivo de destino existente y elimina el temporal al terminar.
     *
     * @param properties propiedades de conexión que se van a guardar
     * @param examples comentarios de ayuda para las propiedades opcionales
     * @throws IOException si no se puede leer o guardar la configuración
     */
    public static void saveInitialConfiguration(Properties properties, String examples) throws IOException {
        writeConfiguration(properties, examples, false);
    }

    /**
     * Lee una copia editable del archivo, conservando también propiedades desconocidas.
     * Las variables de entorno no se copian: mantienen su prioridad al realizar peticiones.
     *
     * @return propiedades actuales del archivo o recurso empaquetado
     * @throws IllegalStateException si no se puede leer la configuración
     */
    public static Properties editableConfiguration() {
        return load();
    }

    /**
     * Sustituye el archivo de configuración y actualiza la conexión sin reiniciar.
     * Intenta un reemplazo atómico; si no está soportado usa un reemplazo normal.
     *
     * @param properties propiedades completas que se guardarán
     * @param examples comentarios de ayuda para las URLs opcionales
     * @throws IOException si no se puede escribir o reemplazar el archivo
     */
    public static void saveConfiguration(Properties properties, String examples) throws IOException {
        writeConfiguration(properties, examples, true);
    }

    /**
     * Escribe primero un temporal y publica la instantánea solo tras guardar correctamente.
     *
     * @param properties propiedades que se guardarán en UTF-8
     * @param examples comentarios de ayuda
     * @param replace true para reemplazar; false para proteger un archivo existente
     * @throws IOException si falla la escritura o el movimiento del archivo
     */
    private static synchronized void writeConfiguration(Properties properties, String examples, boolean replace) throws IOException {
        Path file = configurationFile();
        Files.createDirectories(file.getParent());
        Path temporary = Files.createTempFile(file.getParent(), ".dumblefx-config-", ".tmp");
        try {
            try (var writer = Files.newBufferedWriter(temporary, StandardCharsets.UTF_8)) {
                properties.store(writer, "DumbleFX\n" + examples);
            }
            if (replace) {
                try {
                    Files.move(temporary, file, StandardCopyOption.ATOMIC_MOVE, StandardCopyOption.REPLACE_EXISTING);
                } catch (AtomicMoveNotSupportedException unsupported) {
                    Files.move(temporary, file, StandardCopyOption.REPLACE_EXISTING);
                }
            } else {
                Files.move(temporary, file);
            }
            Properties snapshot = new Properties();
            snapshot.putAll(properties);
            cachedProperties = snapshot;
        } finally {
            Files.deleteIfExists(temporary);
        }
    }

    /**
     * Lee las propiedades del archivo o del recurso empaquetado.
     * Prioriza el archivo externo; un error de lectura se comunica como IllegalStateException.
     *
     * @return propiedades cargadas; vacías si no hay archivo ni recurso predeterminado
     * @throws IllegalStateException si la configuración no puede utilizarse
     */
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

    /**
     * Resuelve un valor priorizando una variable de entorno no vacía sobre el archivo.
     *
     * @param name nombre o filtro de nombre; en configuración, nombre de la variable
     * @param fallback valor alternativo si la propiedad no existe
     * @return valor sin espacios en los extremos, o el valor alternativo
     */
    private static String value(String name, String fallback) {
        String environment = System.getenv(name);
        return environment == null || environment.isBlank()
                ? properties().getProperty(name, fallback).trim() : environment.trim();
    }

    /**
     * Resuelve la URL de una operación y comprueba que use HTTP o HTTPS.
     * Si la URL específica está vacía, concatena la URL base con la ruta predeterminada.
     *
     * @param operation nombre de la propiedad que configura la operación
     * @param defaultPath ruta utilizada si no se configura una URL específica
     * @return URL configurada o URL construida a partir de la base
     * @throws IllegalStateException si la configuración no puede utilizarse
     */
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

    /**
     * Obtiene la clave de API según la prioridad de configuración.
     * El valor es confidencial y no debe incluirse en registros ni documentación.
     *
     * @return clave configurada, o cadena vacía si no existe
     */
    public static String key() { return value("DUMBLEFX_API_KEY", ""); }
}
