package Dumblewindows.javafx.dumblefx.Modelos;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonAlias;

/**
 * Modelo de lectura de un estudiante recibido como JSON.
 * Ignora campos desconocidos y admite variantes de apellido y fecha de nacimiento enviadas por la API.
 *
 * @author Alain Paunero
 */
@JsonIgnoreProperties(ignoreUnknown = true)
public class Alumno {
    /**
     * Identificador del estudiante recibido de la API.
     */
    private String id;
    /**
     * Nombre del estudiante.
     */
    private String nombre;
    /**
     * Apellidos; admite también la clave JSON singular apellido.
     */
    @JsonAlias("apellido")
    private String apellidos;
    /**
     * Casa del estudiante.
     */
    private String casa;
    /**
     * Fecha de nacimiento como texto, normalmente en formato ISO.
     */
    private String nacimiento;
    /**
     * Origen recibido de la API, conservado sin acceso público explícito.
     */
    private String origen;

    /**
     * Devuelve el identificador del estudiante.
     *
     * @return Identificador del estudiante recibido de la API.
     */
    public String getId() { return id; }
    /**
     * Actualiza el identificador del estudiante.
     *
     * @param id nuevo valor del atributo; puede ser null
     */
    public void setId(String id) { this.id = id; }
    /**
     * Devuelve el nombre del estudiante.
     *
     * @return Nombre del estudiante.
     */
    public String getNombre() { return nombre; }
    /**
     * Actualiza el nombre del estudiante.
     *
     * @param nombre nuevo valor del atributo; puede ser null
     */
    public void setNombre(String nombre) { this.nombre = nombre; }
    /**
     * Devuelve los apellidos del estudiante.
     *
     * @return Apellidos; admite también la clave JSON singular apellido.
     */
    public String getApellidos() { return apellidos; }
    /**
     * Actualiza los apellidos del estudiante.
     *
     * @param apellidos nuevo valor del atributo; puede ser null
     */
    public void setApellidos(String apellidos) { this.apellidos = apellidos; }
    /**
     * Devuelve la casa del estudiante.
     *
     * @return Casa del estudiante.
     */
    public String getCasa() { return casa; }
    /**
     * Actualiza la casa del estudiante.
     *
     * @param casa nuevo valor del atributo; puede ser null
     */
    public void setCasa(String casa) { this.casa = casa; }
    /**
     * Devuelve la fecha de nacimiento del estudiante.
     *
     * @return Fecha de nacimiento como texto, normalmente en formato ISO.
     */
    public String getNacimiento() { return nacimiento; }
    /**
     * Actualiza la fecha de nacimiento del estudiante.
     *
     * @param nacimiento nuevo valor del atributo; puede ser null
     */
    public void setNacimiento(String nacimiento) { this.nacimiento = nacimiento; }
    /**
     * Acepta la variante JSON fecha_nacimiento sin sobrescribir la fecha con un valor vacío.
     *
     * @param fecha nuevo valor del atributo; puede ser null
     */
    @JsonProperty("fecha_nacimiento")
    public void setFechaNacimiento(String fecha) {
        if (fecha != null && !fecha.isBlank()) nacimiento = fecha;
    }
}
