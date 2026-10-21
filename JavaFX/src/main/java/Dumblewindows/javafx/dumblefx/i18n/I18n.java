package Dumblewindows.javafx.dumblefx.i18n;

import javafx.beans.binding.Bindings;
import javafx.beans.binding.StringBinding;
import javafx.beans.property.ObjectProperty;
import javafx.beans.property.SimpleObjectProperty;
import javafx.beans.property.Property;
import javafx.scene.Node;
import javafx.scene.Parent;
import javafx.scene.control.Labeled;
import javafx.scene.control.Control;
import javafx.scene.control.Tooltip;
import javafx.util.Duration;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableView;
import javafx.scene.control.TextInputControl;
import java.util.Locale;
import java.util.ResourceBundle;

/**
 * Gestión de textos traducidos y enlaces reactivos al idioma seleccionado.
 * Los enlaces observan el locale para actualizar controles y mensajes sin reiniciar la aplicación.
 *
 * @author Alain Paunero
 */
public final class I18n {
    /**
     * Idioma observable que provoca la actualización de los enlaces de texto.
     */
    private static final ObjectProperty<Locale> LOCALE = new SimpleObjectProperty<>(Locale.forLanguageTag("es"));
    /**
     * Ruta base de los catálogos de traducción incluidos en recursos.
     */
    private static final String BASE = "Dumblewindows.javafx.dumblefx.i18n.messages";

    /**
     * Impide crear instancias de esta utilidad.
     */
    private I18n() { }

    /**
     * Selecciona inglés, euskera o español a partir del nombre visible del idioma.
     *
     * @param language nombre visible del idioma
     */
    public static void setLanguage(String language) {
        LOCALE.set(Locale.forLanguageTag(switch (language) {
            case "Inglés" -> "en";
            case "Euskera" -> "eu";
            default -> "es";
        }));
    }

    /**
     * Carga el catálogo de traducciones del idioma seleccionado.
     *
     * @return catálogo de recursos para el locale activo
     */
    public static ResourceBundle bundle() { return ResourceBundle.getBundle(BASE, LOCALE.get()); }

    /**
     * Busca una traducción y sustituye sus marcadores con String.format y el locale activo.
     *
     * @param key clave del catálogo de traducciones
     * @param arguments valores que sustituyen los marcadores de formato
     * @return texto traducido y formateado
     */
    public static String text(String key, Object... arguments) {
        return String.format(LOCALE.get(), bundle().getString(key), arguments);
    }

    /**
     * Crea un enlace de texto que se recalcula al cambiar el idioma.
     *
     * @param key clave del catálogo de traducciones
     * @param arguments valores que sustituyen los marcadores de formato
     * @return enlace observable de la traducción solicitada
     */
    public static StringBinding binding(String key, Object... arguments) {
        return Bindings.createStringBinding(() -> text(key, arguments), LOCALE);
    }

    /**
     * Reemplaza el enlace de una propiedad por la traducción reactiva indicada.
     *
     * @param property propiedad de texto que se enlaza
     * @param key clave del catálogo de traducciones
     * @param arguments valores que sustituyen los marcadores de formato
     */
    public static void message(Property<String> property, String key, Object... arguments) {
        property.unbind();
        property.bind(binding(key, arguments));
    }

    /**
     * Busca una clave cuyo texto coincida con el valor actual y enlaza su traducción.
     * No modifica propiedades ya enlazadas ni valores null.
     *
     * @param property propiedad de texto que se enlaza
     */
    private static void bindStatic(Property<String> property) {
        if (property.isBound() || property.getValue() == null) return;
        ResourceBundle resources = bundle();
        for (String key : resources.keySet()) {
            if (resources.getString(key).equals(property.getValue())) {
                message(property, key);
                return;
            }
        }
    }

    /**
     * Enlaza el título de una columna y recorre sus columnas hijas.
     *
     * @param column columna cuyos títulos se enlazan
     */
    private static void bindColumn(TableColumn<?, ?> column) {
        bindStatic(column.textProperty());
        for (TableColumn<?, ?> child : column.getColumns()) bindColumn(child);
    }

    /**
     * Crea una ayuda traducida, configura su aspecto y la asigna al control.
     *
     * @param control control que recibe la ayuda
     * @param key clave del catálogo de traducciones
     */
    public static void tooltip(Control control, String key) {
        Tooltip tooltip = new Tooltip();
        message(tooltip.textProperty(), key);
        configureTooltip(tooltip);
        control.setTooltip(tooltip);
    }

    /**
     * Configura ajuste de línea, ancho máximo y demora de la ayuda, evitando duplicar su estilo.
     *
     * @param tooltip ayuda visual que se configura
     */
    private static void configureTooltip(Tooltip tooltip) {
        tooltip.setWrapText(true);
        tooltip.setMaxWidth(420);
        tooltip.setShowDelay(Duration.millis(200));
        tooltip.setShowDuration(Duration.INDEFINITE);
        if (!tooltip.getStyleClass().contains("info-tooltip")) {
            tooltip.getStyleClass().add("info-tooltip");
        }
    }

    /**
     * Recorre una vista para enlazar textos accesibles, ayudas, etiquetas y prompts.
     * Incluye títulos de tablas y descendientes de cada Parent.
     *
     * @param node nodo inicial del recorrido de la vista
     */
    public static void bindView(Node node) {
        bindStatic(node.accessibleTextProperty());
        if (node instanceof Control control && control.getTooltip() != null) {
            bindStatic(control.getTooltip().textProperty());
            configureTooltip(control.getTooltip());
        }
        if (node instanceof Labeled labeled) bindStatic(labeled.textProperty());
        if (node instanceof TextInputControl input) bindStatic(input.promptTextProperty());
        if (node instanceof TableView<?> table) {
            for (TableColumn<?, ?> column : table.getColumns()) bindColumn(column);
        }
        if (node instanceof Parent parent) {
            for (Node child : parent.getChildrenUnmodifiable()) bindView(child);
        }
    }
}
