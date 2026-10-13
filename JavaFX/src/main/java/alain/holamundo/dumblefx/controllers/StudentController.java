package alain.holamundo.dumblefx.controllers;

import alain.holamundo.dumblefx.API.ApiClient;
import alain.holamundo.dumblefx.I18n.I18n;
import alain.holamundo.dumblefx.assets.Alumno;
import alain.holamundo.dumblefx.assets.Matricula;
import javafx.beans.property.SimpleStringProperty;
import javafx.collections.FXCollections;
import javafx.concurrent.Task;
import javafx.fxml.FXML;
import javafx.scene.control.*;
import javafx.scene.layout.VBox;
import java.time.LocalDate;
import java.time.format.DateTimeParseException;
import java.util.concurrent.Callable;
import java.util.function.Consumer;

public final class StudentController {
    @FXML private VBox operationPane;
    @FXML private TextField idField, searchField, houseFilterField, limitField, nameField, houseField, birthDateField;
    @FXML private Label status, result;
    @FXML private TableView<Alumno> table;
    @FXML private TableColumn<Alumno, String> idColumn, nameColumn, houseColumn, birthColumn;
    private final ApiClient api = new ApiClient();
    private String loadedId;

    @FXML private void initialize() {
        if (table == null) return;
        idColumn.setCellValueFactory(cell -> new SimpleStringProperty(cell.getValue().getId()));
        nameColumn.setCellValueFactory(cell -> new SimpleStringProperty(cell.getValue().getNombre()));
        houseColumn.setCellValueFactory(cell -> new SimpleStringProperty(cell.getValue().getCasa()));
        birthColumn.setCellValueFactory(cell -> new SimpleStringProperty(cell.getValue().getNacimiento()));
        Label placeholder = new Label();
        I18n.message(placeholder.textProperty(), "status.empty");
        table.setPlaceholder(placeholder);
    }

    @FXML private void findId() {
        String id = parsedId();
        if (id != null) run(() -> api.get(id), alumno -> describe(alumno));
    }

    @FXML private void search() {
        int limit;
        try {
            limit = Integer.parseInt(limitField.getText().trim());
            if (limit < 1 || limit > 100) throw new NumberFormatException();
        } catch (NumberFormatException error) {
            I18n.message(status.textProperty(), "validation.limit");
            return;
        }
        String name = searchField.getText().trim();
        String house = houseFilterField.getText().trim();
        run(() -> api.search(name, house, limit), alumnos -> {
            table.setItems(FXCollections.observableArrayList(alumnos));
            I18n.message(status.textProperty(), "status.results", alumnos.size());
        });
    }

    @FXML private void create() {
        String id = parsedId();
        if (id == null) return;
        Matricula value = form(id);
        if (value == null) return;
        run(() -> api.create(value), created -> {
            idField.clear(); nameField.clear(); houseField.clear(); birthDateField.clear();
            I18n.message(status.textProperty(), "status.created", created.studentId());
        });
    }

    @FXML private void loadForUpdate() {
        String id = parsedId();
        if (id == null) return;
        loadedId = null;
        run(() -> api.get(id), alumno -> {
            nameField.setText(alumno.getNombre()); houseField.setText(alumno.getCasa());
            birthDateField.setText(alumno.getNacimiento());
            loadedId = id;
            I18n.message(status.textProperty(), "status.loaded");
        });
    }

    @FXML private void update() {
        String id = parsedId();
        if (id == null) return;
        if (!id.equals(loadedId)) {
            I18n.message(status.textProperty(), "validation.load");
            return;
        }
        Matricula value = form(id);
        if (value != null) run(() -> api.update(id, value), updated -> I18n.message(status.textProperty(), "status.updated", updated.studentId()));
    }

    @FXML private void loadForDelete() {
        String id = parsedId();
        if (id != null) run(() -> api.get(id), alumno -> describe(alumno));
    }

    @FXML private void delete() {
        String id = parsedId();
        if (id == null) return;
        ButtonType cancel = new ButtonType(I18n.text("action.cancel"), ButtonBar.ButtonData.CANCEL_CLOSE);
        ButtonType confirm = new ButtonType(I18n.text("action.confirm"), ButtonBar.ButtonData.OK_DONE);
        Alert alert = new Alert(Alert.AlertType.CONFIRMATION, "", cancel, confirm);
        I18n.message(alert.titleProperty(), "delete.header");
        I18n.message(alert.headerTextProperty(), "delete.header");
        I18n.message(alert.contentTextProperty(), "delete.confirm", id);
        alert.initOwner(operationPane.getScene().getWindow());
        if (alert.showAndWait().orElse(cancel) != confirm) return;
        run(() -> { api.delete(id); return null; }, ignored -> {
            result.textProperty().unbind(); result.setText(""); I18n.message(status.textProperty(), "status.deleted");
        });
    }

    private String parsedId() {
        String id = idField.getText().trim();
        if (id.isBlank() || id.length() > 64) {
            I18n.message(status.textProperty(), "validation.id");
            return null;
        }
        return id;
    }

    private Matricula form(String id) {
        String name = nameField.getText().trim();
        String house = houseField.getText().trim();
        String birth = birthDateField.getText().trim();
        if (name.isBlank() || name.length() > 200 || house.length() > 80) {
            I18n.message(status.textProperty(), "validation.name");
            return null;
        }
        try {
            if (!birth.isBlank()) {
                if (!birth.matches("\\d{4}-\\d{2}-\\d{2}")) throw new IllegalArgumentException();
                LocalDate.parse(birth);
            }
        } catch (DateTimeParseException | IllegalArgumentException error) {
            I18n.message(status.textProperty(), "validation.date");
            return null;
        }
        return new Matricula(id, name, house.isBlank() ? null : house, birth.isBlank() ? null : birth);
    }

    private void describe(Alumno alumno) {
        I18n.message(result.textProperty(), "result.details", alumno.getId(), text(alumno.getNombre()),
                text(alumno.getCasa()), text(alumno.getNacimiento()));
    }

    private static String text(String value) { return value == null || value.isBlank() ? "—" : value; }

    private <T> void run(Callable<T> work, Consumer<T> success) {
        I18n.message(status.textProperty(), "status.connecting");
        operationPane.setDisable(true);
        Task<T> task = new Task<>() { @Override protected T call() throws Exception { return work.call(); } };
        task.setOnSucceeded(event -> {
            operationPane.setDisable(false);
            I18n.message(status.textProperty(), "status.ready"); success.accept(task.getValue());
        });
        task.setOnFailed(event -> {
            operationPane.setDisable(false);
            I18n.message(status.textProperty(), "status.error", task.getException().getMessage());
        });
        Thread thread = new Thread(task, "dumblefx-api");
        thread.setDaemon(true); thread.start();
    }
}
