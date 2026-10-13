package alain.holamundo.dumblefx.controllers;

import alain.holamundo.dumblefx.I18n.I18n;
import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.scene.Node;
import javafx.scene.control.ComboBox;
import javafx.scene.control.Label;
import javafx.scene.layout.StackPane;

public final class MainController {
    @FXML private StackPane content;
    @FXML private Label sectionTitle;
    @FXML private ComboBox<String> languageSelector;

    @FXML private void initialize() {
        languageSelector.valueProperty().addListener((observable, previous, selected) -> {
            if (selected != null) I18n.setLanguage(selected);
        });
        show("consulta-id.fxml", "nav.id");
    }
    @FXML private void id() { show("consulta-id.fxml", "nav.id"); }
    @FXML private void data() { show("consulta-datos.fxml", "nav.data"); }
    @FXML private void create() { show("crear-estudiante.fxml", "nav.create"); }
    @FXML private void delete() { show("eliminar-estudiante.fxml", "nav.delete"); }
    @FXML private void update() { show("actualizar-estudiante.fxml", "nav.update"); }

    private void show(String file, String title) {
        try {
            FXMLLoader loader = new FXMLLoader(getClass().getResource("/alain/holamundo/dumblefx/ventanas/" + file), I18n.bundle());
            Node view = loader.load();
            I18n.bindView(view);
            content.getChildren().setAll(view);
            I18n.message(sectionTitle.textProperty(), title);
        } catch (Exception error) {
            Label message = new Label();
            I18n.message(message.textProperty(), "screen.error", error.getMessage());
            content.getChildren().setAll(message);
        }
    }
}
