package Dumblewindows.javafx.dumblefx;

import Dumblewindows.javafx.dumblefx.API.ApiConfig;
import Dumblewindows.javafx.dumblefx.i18n.I18n;
import javafx.scene.control.Button;
import javafx.event.ActionEvent;
import javafx.geometry.Insets;
import javafx.scene.control.ButtonBar;
import javafx.scene.control.ButtonType;
import javafx.scene.control.Dialog;
import javafx.scene.control.Label;
import javafx.scene.control.PasswordField;
import javafx.scene.control.TextField;
import javafx.scene.layout.GridPane;
import javafx.stage.Window;

import java.io.IOException;
import java.net.URI;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Properties;

/**
 * Formulario modal para crear o editar la conexión con la API desde la aplicación.
 * Valida las URLs antes de persistir la configuración; la clave se introduce oculta,
 * pero el archivo de configuración la almacena en texto plano.
 *
 * @author Alain Paunero
 */
public final class ConfigDialog {
    /**
     * Impide crear instancias de esta utilidad.
     */
    private ConfigDialog() { }

    /**
     * Comprueba si existe configuración y, si falta, solicita los datos en un diálogo.
     * Cancelar el diálogo devuelve false; un fallo al guardar mantiene abierto el formulario.
     *
     * @param owner ventana propietaria del diálogo; puede ser null
     * @return true si ya existe configuración o se guarda; false si se cancela
     * @throws IOException si no se puede leer o guardar la configuración
     */
    public static boolean ensureConfiguration(Window owner) throws IOException {
        if (ApiConfig.hasConfiguration()) return true;
        return showConfiguration(owner, false);
    }

    /**
     * Abre la configuración existente y aplica los cambios a las siguientes peticiones.
     * Cancelar no modifica el archivo ni la conexión activa.
     *
     * @param owner ventana propietaria del diálogo; puede ser null
     * @return true si se guarda; false si se cancela
     * @throws IllegalStateException si no se puede leer el archivo existente
     */
    public static boolean editConfiguration(Window owner) {
        return showConfiguration(owner, true);
    }

    /**
     * Construye el formulario compartido por el arranque y el botón de configuración.
     * Conserva propiedades adicionales y mantiene abierto el diálogo si no puede guardar.
     *
     * @param owner ventana propietaria; puede ser null
     * @param editing true para precargar y reemplazar la configuración existente
     * @return true si se guarda correctamente; false al cancelar
     */
    private static boolean showConfiguration(Window owner, boolean editing) {
        Properties initial = editing ? ApiConfig.editableConfiguration() : new Properties();

        Dialog<Boolean> dialog = new Dialog<>();
        if (owner != null && owner.getScene() != null) {
            dialog.initOwner(owner);
        }
        dialog.setTitle(editing ? "Configuración de DumbleFX" : "Configuración inicial de DumbleFX");
        dialog.setHeaderText(editing ? "Revisa y corrige los datos de conexión."
                : "No existe config.properties. Introduce los datos de conexión.");
        ButtonType save = new ButtonType(I18n.text("action.save"), ButtonBar.ButtonData.OK_DONE);
        ButtonType cancel = new ButtonType(I18n.text("action.cancel"), ButtonBar.ButtonData.CANCEL_CLOSE);
        dialog.getDialogPane().getButtonTypes().addAll(save, cancel);
        I18n.tooltip((Button) dialog.getDialogPane().lookupButton(save), "tooltip.saveConfig");
        I18n.tooltip((Button) dialog.getDialogPane().lookupButton(cancel), "tooltip.cancelConfig");

        GridPane form = new GridPane();
        form.setPadding(new Insets(15));
        form.setHgap(12);
        form.setVgap(10);
        Map<String, TextField> fields = new LinkedHashMap<>();
        addField(form, fields, "URL base de la API *", "DUMBLEFX_API_URL", "https://servidor:puerto", false);
        addField(form, fields, "Clave de la API *", "DUMBLEFX_API_KEY", "Clave X-API-Key", true);
        addField(form, fields, "Consulta por ID", "DUMBLEFX_API_URL_CONSULTA_ID", "http://127.0.0.1:8000/estudiantes/{student_id}", false);
        addField(form, fields, "Consulta por datos", "DUMBLEFX_API_URL_CONSULTA_DATOS", "http://127.0.0.1:8000/estudiantes", false);
        addField(form, fields, "Crear matrícula", "DUMBLEFX_API_URL_CREAR", "http://127.0.0.1:8000/matriculas", false);
        addField(form, fields, "Eliminar matrícula", "DUMBLEFX_API_URL_ELIMINAR", "http://127.0.0.1:8000/matriculas/{student_id}", false);
        addField(form, fields, "Actualizar matrícula", "DUMBLEFX_API_URL_ACTUALIZAR", "http://127.0.0.1:8000/matriculas/{student_id}", false);
        fields.forEach((name, field) -> {
            if (initial.containsKey(name)) field.setText(initial.getProperty(name));
        });
        Label help = new Label("* Obligatorios. Las URLs de operación son opcionales.\n"
                + "Si se dejan vacías, se guardan vacías con su ejemplo como comentario.\n"
                + "Conserva {student_id} en las URLs que lo necesiten.\n"
                + "Las variables de entorno tienen prioridad sobre este archivo.\n"
                + "La clave se guardará en texto plano en:\n" + ApiConfig.configurationFile());
        help.setWrapText(true);
        form.add(help, 0, fields.size(), 2, 1);
        Label error = new Label();
        error.setWrapText(true);
        error.setMaxWidth(620);
        error.setStyle("-fx-text-fill: #b00020;");
        form.add(error, 0, fields.size() + 1, 2, 1);
        dialog.getDialogPane().setContent(form);
        dialog.setResultConverter(button -> button == save);
        dialog.getDialogPane().lookupButton(save).addEventFilter(ActionEvent.ACTION, event -> {
            Properties properties = new Properties();
            properties.putAll(initial);
            StringBuilder examples = new StringBuilder("Ejemplos de las variables opcionales (pueden dejarse vacías):\n");
            for (Map.Entry<String, TextField> entry : fields.entrySet()) {
                String value = entry.getValue().getText().trim();
                boolean required = entry.getKey().equals("DUMBLEFX_API_URL") || entry.getKey().equals("DUMBLEFX_API_KEY");
                if (required && value.isEmpty()) {
                    error.setText("Completa la URL base y la clave de la API.");
                    entry.getValue().requestFocus();
                    // Evita que el botón cierre el diálogo mientras haya errores.
                    event.consume();
                    return;
                }
                if (!value.isEmpty() && !entry.getKey().equals("DUMBLEFX_API_KEY") && !validUrl(value)) {
                    error.setText("Introduce una URL http:// o https:// válida en " + entry.getKey() + ".");
                    entry.getValue().requestFocus();
                    // Evita que el botón cierre el diálogo mientras haya errores.
                    event.consume();
                    return;
                }
                properties.setProperty(entry.getKey(), value);
                boolean studentUrl = entry.getKey().equals("DUMBLEFX_API_URL_CONSULTA_ID")
                        || entry.getKey().equals("DUMBLEFX_API_URL_ELIMINAR")
                        || entry.getKey().equals("DUMBLEFX_API_URL_ACTUALIZAR");
                if (studentUrl && !value.isEmpty() && !value.contains("{student_id}")) {
                    error.setText("Conserva {student_id} en " + entry.getKey() + ".");
                    entry.getValue().requestFocus();
                    event.consume();
                    return;
                }
                if (!required) {
                    examples.append(entry.getKey()).append(": ")
                            .append(entry.getValue().getPromptText().trim()).append('\n');
                }
            }
            try {
                if (editing) ApiConfig.saveConfiguration(properties, examples.toString());
                else ApiConfig.saveInitialConfiguration(properties, examples.toString());
            } catch (IOException failure) {
                error.setText("No se pudo guardar la configuración. Comprueba la ruta y los permisos de escritura.");
                event.consume();
            }
        });
        return dialog.showAndWait().orElse(false);
    }

    /**
     * Añade un campo al formulario y lo precarga desde su variable de entorno, si existe.
     *
     * @param form cuadrícula que recibe el campo
     * @param fields campos indexados por nombre de variable de configuración
     * @param label etiqueta visible del campo
     * @param name nombre o filtro de nombre; en configuración, nombre de la variable
     * @param prompt texto de ejemplo del campo
     * @param password true para ocultar el texto introducido
     */
    private static void addField(GridPane form, Map<String, TextField> fields,
                                 String label, String name, String prompt, boolean password) {
        TextField field = password ? new PasswordField() : new TextField();
        field.setPromptText(prompt);
        field.setPrefColumnCount(40);
        String environment = System.getenv(name);
        if (environment != null) field.setText(environment);
        form.addRow(fields.size(), new Label(label), field);
        fields.put(name, field);
    }

    /**
     * Valida una URL HTTP o HTTPS sin credenciales ni fragmento.
     * Sustituye temporalmente el marcador de estudiante para poder analizar URLs de operaciones.
     *
     * @param value valor que se valida, transforma o envía
     * @return true si la URL cumple las restricciones de conexión
     */
    private static boolean validUrl(String value) {
        try {
            URI uri = URI.create(value.replace("{student_id}", "student-id"));
            return ("http".equals(uri.getScheme()) || "https".equals(uri.getScheme()))
                    && uri.getHost() != null && uri.getRawUserInfo() == null
                    && uri.getFragment() == null && uri.getPort() <= 65535;
        } catch (IllegalArgumentException failure) {
            return false;
        }
    }
}
