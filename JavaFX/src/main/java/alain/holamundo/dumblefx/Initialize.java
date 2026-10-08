package alain.holamundo.dumblefx;

import javafx.application.Application;
import javafx.fxml.FXMLLoader;
import javafx.scene.Scene;
import javafx.stage.Stage;

public class Initialize extends Application {

    @Override
    public void start(Stage stage) throws Exception {
        FXMLLoader loader = new FXMLLoader(getClass().getResource("/alain/holamundo/dumblefx/ventanas/hello-view.fxml"));
        Scene scene = new Scene(loader.load(), 1100, 700);

        scene.getStylesheets().add(getClass().getResource("/alain/holamundo/dumblefx/estilos/app.css").toExternalForm());

        stage.setTitle("DumbleFX · Gestión de alumnos");
        stage.setScene(scene);
        stage.show();
    }
}