package alain.holamundo.dumblefx.assets;

import java.util.regex.Pattern;

public final class InputValidation {
    private static final Pattern ID = Pattern.compile("[A-Za-z]{2}-[A-Za-z]{3}-[A-Za-z]{3}(?:-[0-9]{2})?");
    private static final Pattern ID_INPUT = Pattern.compile("[A-Za-z0-9-]{0,13}");
    private static final Pattern NAME_INPUT = Pattern.compile("(?:\\p{L}\\p{M}*| )*");

    private InputValidation() { }

    public static boolean isId(String value) {
        return value != null && ID.matcher(value).matches();
    }

    public static boolean allowsIdInput(String value) {
        return value != null && ID_INPUT.matcher(value).matches();
    }

    public static boolean allowsNameInput(String value) {
        return value != null && value.length() <= 200 && NAME_INPUT.matcher(value).matches();
    }

    public static boolean isName(String value) {
        return value != null && !value.isBlank() && allowsNameInput(value);
    }
}
