package Dumblewindows.javafx.dumblefx.assets;

import java.text.Normalizer;
import java.util.Locale;
import java.util.Set;

/**
 * Construye una vista previa del identificador a partir de casa, nombre y primer apellido.
 * La vista previa no reserva identificadores ni resuelve duplicados: el ID definitivo procede de la API.
 *
 * @author Alain Paunero
 */
public final class StudentIdPreview {
    /**
     * Partículas reconocidas al determinar el primer apellido compuesto.
     */
    private static final Set<String> PARTICLES = Set.of("de", "del", "la", "las", "los", "van", "von", "da", "das", "do", "dos", "di");

    /**
     * Impide crear instancias de esta utilidad.
     */
    private StudentIdPreview() { }

    /**
     * Une prefijos de casa, nombre y primer apellido; usa Central si falta la casa.
     * Cada prefijo contiene como máximo dos, tres y tres letras respectivamente.
     *
     * @param house casa o filtro de casa
     * @param name nombre o filtro de nombre; en configuración, nombre de la variable
     * @param surnames apellidos originales, que pueden incluir partículas
     * @return vista previa en mayúsculas, o cadena vacía si falta alguno de los prefijos
     */
    public static String generate(String house, String name, String surnames) {
        String housePrefix = prefix(house == null || house.isBlank() ? "Central" : house, 2);
        String namePrefix = prefix(name, 3);
        String surnamePrefix = prefix(firstSurname(surnames), 3);
        if (housePrefix.isEmpty() || namePrefix.isEmpty() || surnamePrefix.isEmpty()) return "";
        return housePrefix + "-" + namePrefix + "-" + surnamePrefix;
    }

    /**
     * Obtiene el primer apellido, conservando las partículas iniciales reconocidas.
     *
     * @param surnames apellidos originales, que pueden incluir partículas
     * @return primer apellido compuesto, o cadena vacía si no se proporciona
     */
    private static String firstSurname(String surnames) {
        if (surnames == null || surnames.isBlank()) return "";
        String[] words = surnames.strip().split("\\s+");
        StringBuilder first = new StringBuilder(words[0]);
        int index = 1;
        while (PARTICLES.contains(words[index - 1].toLowerCase(Locale.ROOT)) && index < words.length) {
            first.append(' ').append(words[index]);
            index++;
        }
        return first.toString();
    }

    /**
     * Normaliza diacríticos, conserva solo letras ASCII y obtiene un prefijo en mayúsculas.
     *
     * @param value valor que se valida, transforma o envía
     * @param size número máximo de letras del prefijo
     * @return hasta size letras normalizadas, o cadena vacía si no hay letras
     */
    private static String prefix(String value, int size) {
        if (value == null) return "";
        String letters = Normalizer.normalize(value, Normalizer.Form.NFKD)
                .replaceAll("[^a-zA-Z]", "").toUpperCase(Locale.ROOT);
        return letters.substring(0, Math.min(size, letters.length()));
    }
}
