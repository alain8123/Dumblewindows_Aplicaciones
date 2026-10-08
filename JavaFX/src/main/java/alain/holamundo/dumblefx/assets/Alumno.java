package alain.holamundo.dumblefx.assets;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;

@JsonIgnoreProperties(ignoreUnknown = true)
public class Alumno {
    private String id;
    private String nombre;
    private String casa;
    private String nacimiento;
    private String origen;

    public String getId() { return id; }
    public void setId(String id) { this.id = id; }
    public String getNombre() { return nombre; }
    public void setNombre(String nombre) { this.nombre = nombre; }
    public String getCasa() { return casa; }
    public void setCasa(String casa) { this.casa = casa; }
    public String getNacimiento() { return nacimiento; }
    public void setNacimiento(String nacimiento) { this.nacimiento = nacimiento; }
    @JsonProperty("fecha_nacimiento")
    public void setFechaNacimiento(String fecha) {
        if (fecha != null && !fecha.isBlank()) nacimiento = fecha;
    }
    public String getOrigen() { return origen; }
    public void setOrigen(String origen) { this.origen = origen; }
}
