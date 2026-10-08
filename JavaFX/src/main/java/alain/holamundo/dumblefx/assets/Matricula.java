package alain.holamundo.dumblefx.assets;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;

@JsonIgnoreProperties(ignoreUnknown = true)
public record Matricula(
        @JsonProperty("student_id") String studentId,
        @JsonProperty("full_name") String fullName,
        String house,
        @JsonProperty("birth_date") String birthDate) { }
