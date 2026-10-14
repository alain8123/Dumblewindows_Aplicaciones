package alain.holamundo.dumblefx.assets;

import java.text.Normalizer;
import java.util.Locale;
import java.util.Set;

public final class StudentIdPreview {
    private static final Set<String> PARTICLES = Set.of("de", "del", "la", "las", "los", "van", "von", "da", "das", "do", "dos", "di");

    private StudentIdPreview() { }

    public static String generate(String house, String name, String surnames) {
        String housePrefix = prefix(house == null || house.isBlank() ? "Central" : house, 2);
        String namePrefix = prefix(name, 3);
        String surnamePrefix = prefix(firstSurname(surnames), 3);
        if (housePrefix.isEmpty() || namePrefix.isEmpty() || surnamePrefix.isEmpty()) return "";
        return housePrefix + "-" + namePrefix + "-" + surnamePrefix;
    }

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

    private static String prefix(String value, int size) {
        if (value == null) return "";
        String letters = Normalizer.normalize(value, Normalizer.Form.NFKD)
                .replaceAll("[^a-zA-Z]", "").toUpperCase(Locale.ROOT);
        return letters.substring(0, Math.min(size, letters.length()));
    }
}
