package alain.holamundo.dumblefx.controllers;

import alain.holamundo.dumblefx.I18n.I18n;
import javafx.css.PseudoClass;
import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.scene.Node;
import javafx.scene.control.Button;
import javafx.scene.control.ComboBox;
import javafx.scene.control.Label;
import javafx.scene.layout.StackPane;

public final class MainController {
    private static final PseudoClass SELECTED = PseudoClass.getPseudoClass("selected");
    @FXML private StackPane content;
    @FXML private Label sectionTitle;
    @FXML private ComboBox<String> languageSelector;

    @FXML private Button idButton;
    @FXML private Button dataButton;
    @FXML private Button createButton;
    @FXML private Button deleteButton;
    @FXML private Button updateButton;
    private Button selectedNavigation;

    @FXML private void initialize() {
        languageSelector.valueProperty().addListener((observable, previous, selected) -> {
            if (selected != null) I18n.setLanguage(selected);
        });
        id();
    }
    @FXML private void id() { show("consulta-id.fxml", "nav.id", idButton); }
    @FXML private void data() { show("consulta-datos.fxml", "nav.data", dataButton); }
    @FXML private void create() { show("crear-estudiante.fxml", "nav.create", createButton); }
    @FXML private void delete() { show("eliminar-estudiante.fxml", "nav.delete", deleteButton); }
    @FXML private void update() { show("actualizar-estudiante.fxml", "nav.update", updateButton); }

    private void show(String file, String title, Button navigation) {
        try {
            FXMLLoader loader = new FXMLLoader(getClass().getResource("/alain/holamundo/dumblefx/ventanas/" + file), I18n.bundle());
            Node view = loader.load();
            I18n.bindView(view);
            content.getChildren().setAll(view);
            I18n.message(sectionTitle.textProperty(), title);
            if (selectedNavigation != null) {
                selectedNavigation.pseudoClassStateChanged(SELECTED, false);
            }
            selectedNavigation = navigation;
            selectedNavigation.pseudoClassStateChanged(SELECTED, true);
        } catch (Exception error) {
            if (selectedNavigation != null) {
                selectedNavigation.pseudoClassStateChanged(SELECTED, false);
                selectedNavigation = null;
            }
            Label message = new Label();
            I18n.message(message.textProperty(), "screen.error", error.getMessage());
            content.getChildren().setAll(message);
        }
    }
}
