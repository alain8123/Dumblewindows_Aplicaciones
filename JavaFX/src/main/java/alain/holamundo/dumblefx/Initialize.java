package alain.holamundo.dumblefx;

import alain.holamundo.dumblefx.I18n.I18n;
import javafx.application.Application;
import javafx.application.Platform;
import javafx.scene.control.Alert;
import javafx.fxml.FXMLLoader;
import javafx.scene.Scene;
import javafx.scene.image.Image;
import javafx.stage.Stage;

/**
 * Aplicación JavaFX: prepara la configuración, carga la vista principal y muestra la ventana.
 * Si falla la configuración inicial, informa al usuario y finaliza el arranque.
 *
 * @author Alain Paunero
 */
public class Initialize extends Application {

    /**
     * Prepara la conexión y construye la escena, los estilos, los iconos y el título traducido.
     *
     * @param stage ventana principal proporcionada por JavaFX
     * @throws Exception si falla la carga de la vista, los recursos o el arranque
     */
    @Override
    public void start(Stage stage) throws Exception {
        try {
            if (!ConfigDialog.ensureConfiguration(stage)) {
                Platform.exit();
                return;
            }
        } catch (Exception failure) {
            System.getLogger(Initialize.class.getName()).log(System.Logger.Level.ERROR,
                    "No se pudo preparar la configuración inicial", failure);
            Alert alert = new Alert(Alert.AlertType.ERROR,
                    "No se pudo preparar la configuración inicial. Consulta el detalle del error en la consola.");
            alert.setHeaderText("Error de configuración");
            alert.showAndWait();
            Platform.exit();
            return;
        }
        FXMLLoader loader = new FXMLLoader(getClass().getResource("/alain/holamundo/dumblefx/ventanas/hello-view.fxml"), I18n.bundle());
        javafx.scene.Parent root = loader.load();
        I18n.bindView(root);
        Scene scene = new Scene(root, 1100, 700);

        scene.getStylesheets().add(getClass().getResource("/alain/holamundo/dumblefx/estilos/app.css").toExternalForm());
        stage.setMinWidth(1100);
        stage.setMinHeight(700);
        stage.titleProperty().bind(I18n.binding("app.title"));
        stage.setScene(scene);
        for (int size : new int[] {16, 24, 32, 48, 64, 128, 256, 512, 1024}) {
            String path = "/alain/holamundo/dumblefx/icono/icon-" + size + "x" + size + ".png";
            Image icon = new Image(java.util.Objects.requireNonNull(getClass().getResource(path),
                    "No se encuentra el icono: " + path).toExternalForm());
            if (icon.isError()) {
                throw new IllegalStateException("No se pudo cargar el icono: " + path, icon.getException());
            }
            stage.getIcons().add(icon);
        }
        stage.show();
    }
}
