package alain.holamundo.dumblefx;

import alain.holamundo.dumblefx.API.ApiConfig;
import alain.holamundo.dumblefx.I18n.I18n;
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

public final class ConfigDialog {
    private ConfigDialog() { }

    public static boolean ensureConfiguration(Window owner) throws IOException {
        if (ApiConfig.hasConfiguration()) return true;

        Dialog<Boolean> dialog = new Dialog<>();
        if (owner != null && owner.getScene() != null) {
            dialog.initOwner(owner);
        }
        dialog.setTitle("Configuración inicial de DumbleFX");
        dialog.setHeaderText("No existe config.properties. Introduce los datos de conexión.");
        ButtonType save = new ButtonType("Guardar y continuar", ButtonBar.ButtonData.OK_DONE);
        dialog.getDialogPane().getButtonTypes().addAll(save, ButtonType.CANCEL);
        I18n.tooltip((Button) dialog.getDialogPane().lookupButton(save), "tooltip.saveConfig");
        I18n.tooltip((Button) dialog.getDialogPane().lookupButton(ButtonType.CANCEL), "tooltip.cancelConfig");

        GridPane form = new GridPane();
        form.setPadding(new Insets(15));
        form.setHgap(12);
        form.setVgap(10);
        Map<String, TextField> fields = new LinkedHashMap<>();
        addField(form, fields, "URL base de la API *", "DUMBLEFX_API_URL", "https://servidor:puerto", false);
        addField(form, fields, "Clave de la API *", "DUMBLEFX_API_KEY", "Clave X-API-Key", true);
        addField(form, fields, "Consulta por ID", "DUMBLEFX_API_URL_CONSULTA_ID", "URL consulta id ", false);
        addField(form, fields, "Consulta por datos", "DUMBLEFX_API_URL_CONSULTA_DATOS", "URL consulta datos", false);
        addField(form, fields, "Crear matrícula", "DUMBLEFX_API_URL_CREAR", "URL crear matricula", false);
        addField(form, fields, "Eliminar matrícula", "DUMBLEFX_API_URL_ELIMINAR", "URL eliminar matricula", false);
        addField(form, fields, "Actualizar matrícula", "DUMBLEFX_API_URL_ACTUALIZAR", "URL actualizar matricula", false);
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
            StringBuilder examples = new StringBuilder("Ejemplos de las variables opcionales (pueden dejarse vacías):\n");
            for (Map.Entry<String, TextField> entry : fields.entrySet()) {
                String value = entry.getValue().getText().trim();
                boolean required = entry.getKey().equals("DUMBLEFX_API_URL") || entry.getKey().equals("DUMBLEFX_API_KEY");
                if (required && value.isEmpty()) {
                    error.setText("Completa la URL base y la clave de la API.");
                    entry.getValue().requestFocus();
                    event.consume();
                    return;
                }
                if (!value.isEmpty() && !entry.getKey().equals("DUMBLEFX_API_KEY") && !validUrl(value)) {
                    error.setText("Introduce una URL http:// o https:// válida en " + entry.getKey() + ".");
                    entry.getValue().requestFocus();
                    event.consume();
                    return;
                }
                properties.setProperty(entry.getKey(), value);
                if (!required) {
                    examples.append(entry.getKey()).append(": ")
                            .append(entry.getValue().getPromptText().trim()).append('\n');
                }
            }
            try {
                ApiConfig.saveInitialConfiguration(properties, examples.toString());
            } catch (IOException failure) {
                error.setText("No se pudo guardar la configuración. Comprueba la ruta y los permisos de escritura.");
                event.consume();
            }
        });
        return dialog.showAndWait().orElse(false);
    }

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
