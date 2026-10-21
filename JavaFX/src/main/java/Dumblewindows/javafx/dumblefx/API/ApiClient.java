package Dumblewindows.javafx.dumblefx.API;

import Dumblewindows.javafx.dumblefx.Modelos.Alumno;
import Dumblewindows.javafx.dumblefx.Modelos.Matricula;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import Dumblewindows.javafx.dumblefx.i18n.I18n;
import java.io.IOException;
import java.net.URI;
import java.net.URLEncoder;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.List;

/**
 * Cliente HTTP síncrono de estudiantes y matrículas con serialización JSON mediante Jackson.
 * Incluye X-API-Key en las peticiones y convierte respuestas HTTP fallidas en excepciones.
 * El controlador debe invocarlo fuera del hilo de interfaz para no bloquear JavaFX.
 *
 * @author Alain Paunero
 */
public final class ApiClient {
    /**
     * Conversor entre objetos Java y árboles JSON.
     */
    private final ObjectMapper mapper = new ObjectMapper();
    /**
     * Cliente reutilizable con tiempo máximo de conexión de ocho segundos.
     */
    private final HttpClient http = HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(8)).build();

    /**
     * Envía una petición autenticada y procesa la respuesta JSON.
     * Usa 8 segundos para conectar y 20 segundos por petición. En errores HTTP,
     * extrae detail si es posible y oculta coincidencias de la clave en el mensaje.
     *
     * @param method método HTTP de la operación
     * @param url URL de destino
     * @param body matrícula enviada como JSON; null para una petición sin cuerpo
     * @return árbol JSON de respuesta, u objeto vacío si el cuerpo está vacío
     * @throws Exception si falla la conexión, el protocolo HTTP o el procesamiento JSON
     */
    private JsonNode request(String method, String url, Matricula body) throws Exception {
        String key = ApiConfig.key();
        if (key.isBlank()) throw new IllegalStateException(I18n.text("api.key"));
        URI uri = URI.create(url);
        HttpRequest.Builder builder = HttpRequest.newBuilder(uri)
                .version("https".equalsIgnoreCase(uri.getScheme())
                        ? HttpClient.Version.HTTP_2 : HttpClient.Version.HTTP_1_1)
                .timeout(Duration.ofSeconds(20)).header("Accept", "application/json")
                .header("X-API-Key", key);
        if (body != null) builder.header("Content-Type", "application/json; charset=UTF-8");
        HttpRequest request = builder.method(method, body == null ? HttpRequest.BodyPublishers.noBody()
                : HttpRequest.BodyPublishers.ofString(mapper.writeValueAsString(body), StandardCharsets.UTF_8)).build();
        HttpResponse<String> response = http.send(request, HttpResponse.BodyHandlers.ofString(StandardCharsets.UTF_8));
        // Solo la familia 2xx se considera una respuesta HTTP satisfactoria.
        if (response.statusCode() / 100 != 2) {
            String detail;
            try {
                JsonNode error = mapper.readTree(response.body()).get("detail");
                detail = error == null ? I18n.text("api.noDetail") : error.isTextual() ? error.asText() : error.toString();
            } catch (Exception ignored) {
                detail = I18n.text("api.notJson");
            }
            throw new IOException(("HTTP " + response.statusCode() + ": " + detail).replace(key, I18n.text("api.hidden")));
        }
        return response.body().isBlank() ? mapper.createObjectNode() : mapper.readTree(response.body());
    }

    /**
     * Codifica un valor UTF-8 para incluirlo en una URL, representando espacios como %20.
     *
     * @param value valor que se valida, transforma o envía
     * @return valor codificado para su uso en la URL
     */
    private static String encode(String value) {
        return URLEncoder.encode(value, StandardCharsets.UTF_8).replace("+", "%20");
    }

    /**
     * Resuelve una URL de operación y sustituye su marcador obligatorio de estudiante.
     *
     * @param operation nombre de la propiedad que configura la operación
     * @param defaultPath ruta utilizada si no se configura una URL específica
     * @param id identificador del estudiante; null al construir los datos de alta
     * @return URL con el identificador codificado
     * @throws IllegalStateException si la URL no incluye el marcador de estudiante
     */
    private static String studentUrl(String operation, String defaultPath, String id) {
        String url = ApiConfig.endpoint(operation, defaultPath);
        if (!url.contains("{student_id}")) throw new IllegalStateException(I18n.text("api.studentPath", operation));
        return url.replace("{student_id}", encode(id));
    }

    /**
     * Consulta un estudiante esperando una respuesta JSON de tipo objeto.
     *
     * @param id identificador del estudiante; null al construir los datos de alta
     * @return estudiante deserializado desde la respuesta
     * @throws Exception si falla la conexión, el protocolo HTTP o el procesamiento JSON
     */
    public Alumno get(String id) throws Exception {
        return mapper.treeToValue(request("GET", studentUrl("DUMBLEFX_API_URL_CONSULTA_ID", "/estudiantes/{student_id}", id), null), Alumno.class);
    }

    /**
     * Consulta por identificador admitiendo tanto un objeto como una lista de coincidencias.
     *
     * @param id identificador del estudiante; null al construir los datos de alta
     * @return lista de estudiantes devuelta por la API
     * @throws Exception si falla la conexión, el protocolo HTTP o el procesamiento JSON
     */
    public List<Alumno> findById(String id) throws Exception {
        JsonNode result = request("GET", studentUrl("DUMBLEFX_API_URL_CONSULTA_ID", "/estudiantes/{student_id}", id), null);
        if (result.isArray()) return mapper.convertValue(result, new TypeReference<List<Alumno>>() { });
        return List.of(mapper.treeToValue(result, Alumno.class));
    }

    /**
     * Busca estudiantes por nombre y casa, limitando los resultados entre 1 y 100.
     * Los filtros vacíos se omiten y se exige una respuesta JSON de tipo lista.
     *
     * @param name nombre o filtro de nombre; en configuración, nombre de la variable
     * @param house casa o filtro de casa
     * @param limit número máximo de resultados, entre 1 y 100
     * @return estudiantes que cumplen los filtros
     * @throws Exception si falla la conexión, el protocolo HTTP o el procesamiento JSON
     * @throws IllegalArgumentException si el límite no está entre 1 y 100
     */
    public List<Alumno> search(String name, String house, int limit) throws Exception {
        if (limit < 1 || limit > 100) throw new IllegalArgumentException(I18n.text("validation.limit"));
        String url = ApiConfig.endpoint("DUMBLEFX_API_URL_CONSULTA_DATOS", "/estudiantes");
        StringBuilder query = new StringBuilder(url).append(url.contains("?") ? "&" : "?").append("limit=").append(limit);
        if (!name.isBlank()) query.append("&name=").append(encode(name));
        if (!house.isBlank()) query.append("&house=").append(encode(house));
        JsonNode result = request("GET", query.toString(), null);
        if (!result.isArray()) throw new IOException(I18n.text("api.list"));
        return mapper.convertValue(result, new TypeReference<List<Alumno>>() { });
    }

    /**
     * Envía una solicitud POST para crear una matrícula.
     *
     * @param value valor que se valida, transforma o envía
     * @return matrícula confirmada por la API, incluido su identificador definitivo
     * @throws Exception si falla la conexión, el protocolo HTTP o el procesamiento JSON
     */
    public Matricula create(Matricula value) throws Exception {
        return mapper.treeToValue(request("POST", ApiConfig.endpoint("DUMBLEFX_API_URL_CREAR", "/matriculas"), value), Matricula.class);
    }

    /**
     * Envía una solicitud PUT para actualizar la matrícula del estudiante indicado.
     *
     * @param id identificador del estudiante; null al construir los datos de alta
     * @param value valor que se valida, transforma o envía
     * @return matrícula devuelta por la API tras la actualización
     * @throws Exception si falla la conexión, el protocolo HTTP o el procesamiento JSON
     */
    public Matricula update(String id, Matricula value) throws Exception {
        return mapper.treeToValue(request("PUT", studentUrl("DUMBLEFX_API_URL_ACTUALIZAR", "/matriculas/{student_id}", id), value), Matricula.class);
    }

    /**
     * Solicita por DELETE la eliminación de una matrícula.
     *
     * @param id identificador del estudiante; null al construir los datos de alta
     * @throws Exception si falla la conexión, el protocolo HTTP o el procesamiento JSON
     */
    public void delete(String id) throws Exception {
        request("DELETE", studentUrl("DUMBLEFX_API_URL_ELIMINAR", "/matriculas/{student_id}", id), null);
    }
}
