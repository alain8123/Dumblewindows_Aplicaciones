package alain.holamundo.dumblefx;

import javafx.application.Application;
import javafx.fxml.FXMLLoader;
import javafx.scene.Scene;
import javafx.scene.image.Image;
import javafx.stage.Stage;

public class Initialize extends Application {

    @Override
    public void start(Stage stage) throws Exception {
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