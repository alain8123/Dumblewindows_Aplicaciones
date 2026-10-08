package alain.holamundo.dumblefx.controllers;

import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.scene.Node;
import javafx.scene.control.Label;
import javafx.scene.layout.StackPane;

/** Keeps navigation in one window and loads each student task from its own FXML. */
public final class MainController {
    @FXML private StackPane content;
    @FXML private Label sectionTitle;

    @FXML private void initialize() { show("consulta-id.fxml", "Consulta por ID"); }
    @FXML private void id() { show("consulta-id.fxml", "Consulta por ID"); }
    @FXML private void data() { show("consulta-datos.fxml", "Consulta por datos"); }
    @FXML private void create() { show("crear-estudiante.fxml", "Crear estudiante"); }
    @FXML private void delete() { show("eliminar-estudiante.fxml", "Eliminar estudiante"); }
    @FXML private void update() { show("actualizar-estudiante.fxml", "Actualizar estudiante"); }

    private void show(String file, String title) {
        try {
            Node view = FXMLLoader.load(getClass().getResource("/alain/holamundo/dumblefx/ventanas/" + file));
            content.getChildren().setAll(view);
            sectionTitle.setText(title);
        } catch (Exception error) {
            content.getChildren().setAll(new Label("No se pudo abrir la pantalla: " + error.getMessage()));
        }
    }
}
