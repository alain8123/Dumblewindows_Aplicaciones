package alain.holamundo.dumblefx.controllers;

import alain.holamundo.dumblefx.API.ApiClient;
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
    @FXML private TableColumn<Alumno, String> idColumn, nameColumn, houseColumn, birthColumn, originColumn;
    private final ApiClient api = new ApiClient();
    private String loadedId;

    @FXML private void initialize() {
        if (table == null) return;
        idColumn.setCellValueFactory(cell -> new SimpleStringProperty(cell.getValue().getId()));
        nameColumn.setCellValueFactory(cell -> new SimpleStringProperty(cell.getValue().getNombre()));
        houseColumn.setCellValueFactory(cell -> new SimpleStringProperty(cell.getValue().getCasa()));
        birthColumn.setCellValueFactory(cell -> new SimpleStringProperty(cell.getValue().getNacimiento()));
        originColumn.setCellValueFactory(cell -> new SimpleStringProperty(cell.getValue().getOrigen()));
        table.setPlaceholder(new Label("Sin resultados"));
    }

    @FXML private void findId() {
        String id = parsedId();
        if (id != null) run(() -> api.get(id), alumno -> result.setText(describe(alumno)));
    }

    @FXML private void search() {
        int limit;
        try {
            limit = Integer.parseInt(limitField.getText().trim());
            if (limit < 1 || limit > 100) throw new NumberFormatException();
        } catch (NumberFormatException error) {
            status.setText("El límite debe estar entre 1 y 100.");
            return;
        }
        String name = searchField.getText().trim();
        String house = houseFilterField.getText().trim();
        run(() -> api.search(name, house, limit), alumnos -> {
            table.setItems(FXCollections.observableArrayList(alumnos));
            status.setText(alumnos.size() + " resultado(s) históricos; no incluye matrículas nuevas.");
        });
    }

    @FXML private void create() {
        String id = parsedId();
        if (id == null) return;
        Matricula value = form(id);
        if (value == null) return;
        run(() -> api.create(value), created -> {
            idField.clear(); nameField.clear(); houseField.clear(); birthDateField.clear();
            status.setText("Matrícula creada correctamente: " + created.studentId());
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
            status.setText("Datos cargados. Solo las matrículas de MariaDB se pueden modificar.");
        });
    }

    @FXML private void update() {
        String id = parsedId();
        if (id == null) return;
        if (!id.equals(loadedId)) {
            status.setText("Carga primero los datos del ID que quieres actualizar.");
            return;
        }
        Matricula value = form(id);
        if (value != null) run(() -> api.update(id, value), updated -> status.setText("Matrícula actualizada correctamente: " + updated.studentId()));
    }

    @FXML private void loadForDelete() {
        String id = parsedId();
        if (id != null) run(() -> api.get(id), alumno -> result.setText(describe(alumno)));
    }

    @FXML private void delete() {
        String id = parsedId();
        if (id == null) return;
        Alert alert = new Alert(Alert.AlertType.CONFIRMATION, "¿Eliminar definitivamente la matrícula con ID " + id + "?", ButtonType.CANCEL, ButtonType.OK);
        alert.setHeaderText("Confirmar eliminación");
        if (alert.showAndWait().orElse(ButtonType.CANCEL) != ButtonType.OK) return;
        run(() -> { api.delete(id); return null; }, ignored -> {
            result.setText(""); status.setText("Matrícula eliminada correctamente.");
        });
    }

    private String parsedId() {
        String id = idField.getText().trim();
        if (id.isBlank() || id.length() > 64) {
            status.setText("Introduce un ID de entre 1 y 64 caracteres; puede ser texto o UUID.");
            return null;
        }
        return id;
    }

    private Matricula form(String id) {
        String name = nameField.getText().trim();
        String house = houseField.getText().trim();
        String birth = birthDateField.getText().trim();
        if (name.isBlank() || name.length() > 200 || house.length() > 80) {
            status.setText("Nombre obligatorio (máximo 200 caracteres); casa: máximo 80.");
            return null;
        }
        try {
            if (!birth.isBlank()) {
                if (!birth.matches("\\d{4}-\\d{2}-\\d{2}")) throw new IllegalArgumentException();
                LocalDate.parse(birth);
            }
        } catch (DateTimeParseException | IllegalArgumentException error) {
            status.setText("Introduce una fecha válida con formato AAAA-MM-DD o déjala vacía.");
            return null;
        }
        return new Matricula(id, name, house.isBlank() ? null : house, birth.isBlank() ? null : birth);
    }

    private static String describe(Alumno alumno) {
        return "ID: " + alumno.getId() + "\nNombre: " + text(alumno.getNombre()) + "\nCasa: " + text(alumno.getCasa())
                + "\nNacimiento: " + text(alumno.getNacimiento()) + "\nOrigen: " + text(alumno.getOrigen());
    }

    private static String text(String value) { return value == null || value.isBlank() ? "—" : value; }

    private <T> void run(Callable<T> work, Consumer<T> success) {
        status.setText("Conectando con la API...");
        operationPane.setDisable(true);
        Task<T> task = new Task<>() { @Override protected T call() throws Exception { return work.call(); } };
        task.setOnSucceeded(event -> {
            operationPane.setDisable(false);
            status.setText("Listo"); success.accept(task.getValue());
        });
        task.setOnFailed(event -> {
            operationPane.setDisable(false);
            status.setText("Error: " + task.getException().getMessage());
        });
        Thread thread = new Thread(task, "dumblefx-api");
        thread.setDaemon(true); thread.start();
    }
}
