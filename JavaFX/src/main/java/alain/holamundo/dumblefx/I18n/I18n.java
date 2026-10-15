package alain.holamundo.dumblefx.I18n;

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

public final class I18n {
    private static final ObjectProperty<Locale> LOCALE = new SimpleObjectProperty<>(Locale.forLanguageTag("es"));
    private static final String BASE = "alain.holamundo.dumblefx.i18n.messages";

    private I18n() { }

    public static void setLanguage(String language) {
        LOCALE.set(Locale.forLanguageTag(switch (language) {
            case "Inglés" -> "en";
            case "Euskera" -> "eu";
            default -> "es";
        }));
    }

    public static ResourceBundle bundle() { return ResourceBundle.getBundle(BASE, LOCALE.get()); }

    public static String text(String key, Object... arguments) {
        return String.format(LOCALE.get(), bundle().getString(key), arguments);
    }

    public static StringBinding binding(String key, Object... arguments) {
        return Bindings.createStringBinding(() -> text(key, arguments), LOCALE);
    }

    public static void message(Property<String> property, String key, Object... arguments) {
        property.unbind();
        property.bind(binding(key, arguments));
    }

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

    private static void bindColumn(TableColumn<?, ?> column) {
        bindStatic(column.textProperty());
        for (TableColumn<?, ?> child : column.getColumns()) bindColumn(child);
    }

    public static void tooltip(Control control, String key) {
        Tooltip tooltip = new Tooltip();
        message(tooltip.textProperty(), key);
        configureTooltip(tooltip);
        control.setTooltip(tooltip);
    }

    private static void configureTooltip(Tooltip tooltip) {
        tooltip.setWrapText(true);
        tooltip.setMaxWidth(420);
        tooltip.setShowDelay(Duration.millis(200));
        tooltip.setShowDuration(Duration.INDEFINITE);
        if (!tooltip.getStyleClass().contains("info-tooltip")) {
            tooltip.getStyleClass().add("info-tooltip");
        }
    }

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
