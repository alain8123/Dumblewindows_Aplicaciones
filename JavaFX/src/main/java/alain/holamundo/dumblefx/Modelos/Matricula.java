package alain.holamundo.dumblefx.Modelos;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonAlias;

/**
 * Datos de intercambio para crear y actualizar una matrícula.
 * Jackson omite componentes null y adapta los nombres Java al contrato JSON de la API.
 *
 * @param studentId identificador del estudiante; null al solicitar una nueva matrícula
 * @param fullName nombre enviado como full_name
 * @param apellidos apellidos enviados como apellido
 * @param house casa del estudiante
 * @param birthDate fecha de nacimiento en formato ISO, o null si no se especifica
 *
 * @author Alain Paunero
 */
@JsonIgnoreProperties(ignoreUnknown = true)
@JsonInclude(JsonInclude.Include.NON_NULL)
public record Matricula(
        @JsonProperty("student_id") String studentId,
        @JsonProperty("full_name") String fullName,
        @JsonProperty("apellido")
        @JsonAlias("apellido") String apellidos,
        String house,
        @JsonProperty("birth_date") String birthDate) { }
