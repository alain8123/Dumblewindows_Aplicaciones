package alain.holamundo.dumblefx.API;

import alain.holamundo.dumblefx.assets.Alumno;
import alain.holamundo.dumblefx.assets.Matricula;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.io.IOException;
import java.net.URI;
import java.net.URLEncoder;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.List;

public final class ApiClient {
    private final ObjectMapper mapper = new ObjectMapper();
    private final HttpClient http = HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(8)).build();

    private JsonNode request(String method, String url, Matricula body) throws Exception {
        String key = ApiConfig.key();
        if (key.isBlank()) throw new IllegalStateException("Configura DUMBLEFX_API_KEY en config.properties o en el entorno.");
        HttpRequest.Builder builder = HttpRequest.newBuilder(URI.create(url))
                .timeout(Duration.ofSeconds(20)).header("Accept", "application/json")
                .header("X-API-Key", key);
        if (body != null) builder.header("Content-Type", "application/json; charset=UTF-8");
        HttpRequest request = builder.method(method, body == null ? HttpRequest.BodyPublishers.noBody()
                : HttpRequest.BodyPublishers.ofString(mapper.writeValueAsString(body), StandardCharsets.UTF_8)).build();
        HttpResponse<String> response = http.send(request, HttpResponse.BodyHandlers.ofString(StandardCharsets.UTF_8));
        if (response.statusCode() / 100 != 2) {
            String detail;
            try {
                JsonNode error = mapper.readTree(response.body()).get("detail");
                detail = error == null ? "Respuesta de error sin detalle." : error.isTextual() ? error.asText() : error.toString();
            } catch (Exception ignored) {
                detail = "Respuesta no JSON; comprueba la URL y la disponibilidad de la API.";
            }
            throw new IOException(("HTTP " + response.statusCode() + ": " + detail).replace(key, "[clave ocultada]"));
        }
        return response.body().isBlank() ? mapper.createObjectNode() : mapper.readTree(response.body());
    }

    private static String encode(String value) {
        return URLEncoder.encode(value, StandardCharsets.UTF_8).replace("+", "%20");
    }

    private static String studentUrl(String operation, String defaultPath, String id) {
        String url = ApiConfig.endpoint(operation, defaultPath);
        if (!url.contains("{student_id}")) throw new IllegalStateException(operation + " debe contener {student_id}.");
        return url.replace("{student_id}", encode(id));
    }

    public Alumno get(String id) throws Exception {
        return mapper.treeToValue(request("GET", studentUrl("DUMBLEFX_API_URL_CONSULTA_ID", "/estudiantes/{student_id}", id), null), Alumno.class);
    }

    public List<Alumno> search(String name, String house, int limit) throws Exception {
        if (limit < 1 || limit > 100) throw new IllegalArgumentException("El límite debe estar entre 1 y 100.");
        String url = ApiConfig.endpoint("DUMBLEFX_API_URL_CONSULTA_DATOS", "/estudiantes");
        StringBuilder query = new StringBuilder(url).append(url.contains("?") ? "&" : "?").append("limit=").append(limit);
        if (!name.isBlank()) query.append("&name=").append(encode(name));
        if (!house.isBlank()) query.append("&house=").append(encode(house));
        JsonNode result = request("GET", query.toString(), null);
        if (!result.isArray()) throw new IOException("La consulta debe devolver una lista de estudiantes.");
        return mapper.convertValue(result, new TypeReference<List<Alumno>>() { });
    }

    public Matricula create(Matricula value) throws Exception {
        return mapper.treeToValue(request("POST", ApiConfig.endpoint("DUMBLEFX_API_URL_CREAR", "/matriculas"), value), Matricula.class);
    }

    public Matricula update(String id, Matricula value) throws Exception {
        return mapper.treeToValue(request("PUT", studentUrl("DUMBLEFX_API_URL_ACTUALIZAR", "/matriculas/{student_id}", id), value), Matricula.class);
    }

    public void delete(String id) throws Exception {
        request("DELETE", studentUrl("DUMBLEFX_API_URL_ELIMINAR", "/matriculas/{student_id}", id), null);
    }
}
