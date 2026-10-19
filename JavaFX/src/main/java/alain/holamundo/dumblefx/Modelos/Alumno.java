package alain.holamundo.dumblefx.Modelos;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonAlias;

@JsonIgnoreProperties(ignoreUnknown = true)
public class Alumno {
    private String id;
    private String nombre;
    @JsonAlias("apellido")
    private String apellidos;
    private String casa;
    private String nacimiento;
    private String origen;

    public String getId() { return id; }
    public void setId(String id) { this.id = id; }
    public String getNombre() { return nombre; }
    public void setNombre(String nombre) { this.nombre = nombre; }
    public String getApellidos() { return apellidos; }
    public void setApellidos(String apellidos) { this.apellidos = apellidos; }
    public String getCasa() { return casa; }
    public void setCasa(String casa) { this.casa = casa; }
    public String getNacimiento() { return nacimiento; }
    public void setNacimiento(String nacimiento) { this.nacimiento = nacimiento; }
    @JsonProperty("fecha_nacimiento")
    public void setFechaNacimiento(String fecha) {
        if (fecha != null && !fecha.isBlank()) nacimiento = fecha;
    }
}
