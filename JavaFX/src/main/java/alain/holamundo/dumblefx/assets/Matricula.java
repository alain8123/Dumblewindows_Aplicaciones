package alain.holamundo.dumblefx.assets;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonAlias;

@JsonIgnoreProperties(ignoreUnknown = true)
@JsonInclude(JsonInclude.Include.NON_NULL)
public record Matricula(
        @JsonProperty("student_id") String studentId,
        @JsonProperty("full_name") String fullName,
        @JsonProperty("apellido") @JsonAlias("apellidos") String apellidos,
        String house,
        @JsonProperty("birth_date") String birthDate) { }
