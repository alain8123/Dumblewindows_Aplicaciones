package alain.holamundo.dumblefx;

import javafx.application.Application;

/**
 * Punto de entrada que delega el arranque en la aplicación JavaFX.
 * Separar esta clase de Application permite iniciar el cliente desde Maven o el IDE.
 *
 * @author Alain Paunero
 */
public class Launcher {
    /**
     * Inicia JavaFX con Initialize como clase de aplicación.
     *
     * @param args argumentos de la línea de comandos
     */
    public static void main(String[] args) {
        Application.launch(Initialize.class, args);
    }
}
