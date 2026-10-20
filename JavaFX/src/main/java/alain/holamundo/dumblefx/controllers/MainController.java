package alain.holamundo.dumblefx.controllers;

import alain.holamundo.dumblefx.I18n.I18n;
import javafx.css.PseudoClass;
import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.scene.Node;
import javafx.scene.control.Button;
import javafx.scene.control.ComboBox;
import javafx.scene.control.Label;
import javafx.scene.control.Tooltip;
import javafx.scene.layout.StackPane;
import javafx.util.Duration;

/**
 * Controlador de la ventana principal: navegación, idioma y ayudas de cada sección.
 * Carga las pantallas FXML bajo demanda y conserva el botón de navegación seleccionado.
 *
 * @author Alain Paunero
 */
public final class MainController {
    /**
     * Pseudoclase CSS que marca la sección seleccionada del menú.
     */
    private static final PseudoClass SELECTED = PseudoClass.getPseudoClass("selected");
    /**
     * Nombre mostrado en los créditos traducidos.
     */
    private static final String AUTHORS = "Dumblewindows";
    /**
     * Contenedor donde se inserta la pantalla de la sección activa.
     */
    @FXML private StackPane content;
    /**
     * Título traducido de la sección actual.
     */
    @FXML private Label sectionTitle;
    /**
     * Selector de idioma visible para el usuario.
     */
    @FXML private ComboBox<String> languageSelector;
    /**
     * Icono que muestra ayuda sobre la sección activa.
     */
    @FXML private Label helpIcon;
    /**
     * Icono que muestra los créditos.
     */
    @FXML private Label creditsIcon;
    /**
     * Ayuda reutilizada y actualizada al navegar entre secciones.
     */
    private final Tooltip sectionHelp = new Tooltip();

    /**
     * Botón de consulta por identificador.
     */
    @FXML private Button idButton;
    /**
     * Botón de consulta por filtros.
     */
    @FXML private Button dataButton;
    /**
     * Botón de alta de matrícula.
     */
    @FXML private Button createButton;
    /**
     * Botón de eliminación de matrícula.
     */
    @FXML private Button deleteButton;
    /**
     * Botón de actualización de matrícula.
     */
    @FXML private Button updateButton;
    /**
     * Botón actualmente resaltado; null si no hay selección.
     */
    private Button selectedNavigation;

    /**
     * Configura las ayudas, escucha el selector de idioma y abre la consulta por ID.
     */
    @FXML private void initialize() {
        configureTooltip(helpIcon, sectionHelp);
        Tooltip credits = new Tooltip();
        configureTooltip(creditsIcon, credits);
        I18n.message(credits.textProperty(), "help.credits", AUTHORS);
        creditsIcon.accessibleTextProperty().bind(credits.textProperty());
        languageSelector.valueProperty().addListener((observable, previous, selected) -> {
            if (selected != null) I18n.setLanguage(selected);
        });
        id();
    }

    /**
     * Asocia a un icono una ayuda multilínea con estilo y demora de 200 milisegundos.
     *
     * @param icon etiqueta que presenta el icono de ayuda
     * @param tooltip ayuda visual que se configura
     */
    private void configureTooltip(Label icon, Tooltip tooltip) {
        tooltip.setWrapText(true);
        tooltip.setMaxWidth(420);
        tooltip.setShowDelay(Duration.millis(200));
        tooltip.setShowDuration(Duration.INDEFINITE);
        tooltip.getStyleClass().add("info-tooltip");
        icon.setTooltip(tooltip);
    }
    /**
     * Abre la pantalla de consulta por identificador.
     */
    @FXML private void id() { show("consulta-id.fxml", "nav.id", idButton); }
    /**
     * Abre la pantalla de búsqueda por datos.
     */
    @FXML private void data() { show("consulta-datos.fxml", "nav.data", dataButton); }
    /**
     * Abre el formulario de nueva matrícula.
     */
    @FXML private void create() { show("crear-estudiante.fxml", "nav.create", createButton); }
    /**
     * Abre la pantalla de eliminación de matrícula.
     */
    @FXML private void delete() { show("eliminar-estudiante.fxml", "nav.delete", deleteButton); }
    /**
     * Abre el formulario de actualización de matrícula.
     */
    @FXML private void update() { show("actualizar-estudiante.fxml", "nav.update", updateButton); }

    /**
     * Carga una pantalla FXML, enlaza sus traducciones y actualiza navegación y ayuda.
     * Si la carga falla, muestra un mensaje traducido y limpia la selección del menú.
     *
     * @param file nombre del FXML dentro de ventanas
     * @param title clave de traducción del título
     * @param navigation botón del menú que queda seleccionado
     */
    private void show(String file, String title, Button navigation) {
        try {
            FXMLLoader loader = new FXMLLoader(getClass().getResource("/alain/holamundo/dumblefx/ventanas/" + file), I18n.bundle());
            Node view = loader.load();
            I18n.bindView(view);
            content.getChildren().setAll(view);
            I18n.message(sectionTitle.textProperty(), title);
            I18n.message(sectionHelp.textProperty(), title.replace("nav.", "help."));
            helpIcon.accessibleTextProperty().bind(sectionHelp.textProperty());
            if (selectedNavigation != null) {
                selectedNavigation.pseudoClassStateChanged(SELECTED, false);
            }
            selectedNavigation = navigation;
            selectedNavigation.pseudoClassStateChanged(SELECTED, true);
        } catch (Exception error) {
            I18n.message(sectionHelp.textProperty(), "help.error");
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
