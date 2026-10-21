// -----------------------------------------------------------------------------
// Archivo:  ConsultaAlumno.cs
// Proyecto: DumbleUnity
// Autor:    Ivan Salazar
// -----------------------------------------------------------------------------

using System;                          // Tipos básicos: Uri, StringComparison, ArgumentException, [Serializable]...
using System.Collections;              // IEnumerator, necesario para las corrutinas de Unity.
using System.Collections.Generic;      // List<T>, usada para la lista de animaciones personalizadas.
using System.Linq;                     // Any() y FirstOrDefault() para buscar dentro de listas y arrays.
using System.Text;                     // StringBuilder, para construir textos carácter a carácter sin crear muchas cadenas.
using System.Text.RegularExpressions;  // Regex, para validar el formato del nombre y compactar espacios.
using TMPro;                           // TextMeshPro: campo de texto (TMP_InputField) y etiquetas (TMP_Text).
using UnityEngine;                     // API principal de Unity (MonoBehaviour, Debug, Resources...).
using UnityEngine.Networking;          // UnityWebRequest, para hacer peticiones HTTP a la API.

/// <summary>
/// Representa UN estudiante tal y como lo devuelve la API en formato JSON.
/// JsonUtility (el lector de JSON de Unity) rellena estos campos buscando claves con EXACTAMENTE el mismo
/// nombre en el JSON. Si la API envía más campos, simplemente se ignoran; si falta alguno, queda en null.
/// </summary>
[Serializable] // Obligatorio para que JsonUtility pueda rellenar esta clase desde un JSON.
public class RespuestaApi
{
    public string nombre;    // Nombre de pila del estudiante (clave "nombre" del JSON).
    public string apellido;  // Apellido del estudiante (clave "apellido" del JSON).
    public string patronus;  // Animal del patronus, en inglés, p. ej. "Stag" o "Otter" (clave "patronus").
}

/// <summary>
/// Entrada de la lista "Animaciones personalizadas" del inspector.
/// Permite decir: "cuando la API devuelva este patronus, en lugar del spritesheet usa este prefab".
/// Es opcional: si la lista está vacía, todos los patronus usan su spritesheet de Resources.
/// </summary>
[Serializable] // Necesario para que Unity muestre y guarde esta clase en el inspector.
public class AnimacionPorValor
{
    public string valor;      // Texto del patronus tal y como llega de la API (p. ej. "Stag").
    public GameObject prefab; // Prefab de UI (Image + Animator, etc.) que se mostrará en su lugar.
}

/// <summary>
/// Script principal de la pantalla. Flujo completo:
/// <br/>1. El usuario escribe un nombre en el campo de texto (solo se admiten letras, espacios y apóstrofos).
/// <br/>2. Al pulsar "Buscar" se valida el nombre y la configuración de la API.
/// <br/>3. Se hace una petición GET a la API.
/// <br/>4. Con la respuesta se abre una lista (PopupSeleccionAlumno) con los estudiantes encontrados.
/// <br/>5. Al elegir uno, se muestra su patronus animado en PopupAlumno.
/// <br/>Si algo falla en cualquier paso, se muestra un aviso en PopupAviso.
/// Este componente está en el objeto "ControladorConsultaAlumno" de la escena "menu".
/// </summary>
public class ConsultaAlumno : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Referencias a la interfaz (se arrastran desde la escena en el inspector)
    // ------------------------------------------------------------------
    [Header("UI")] // Título que agrupa estos campos en el inspector.
    [SerializeField, Tooltip("Campo donde el usuario escribe el nombre a buscar.")] private TMP_InputField inputNombre;
    [SerializeField, Tooltip("(Opcional) Texto pequeño para mensajes como \"Buscando...\".")] private TMP_Text textoEstado;
    [SerializeField, Tooltip("Ventana que enseña la animación del patronus.")] private PopupAlumno popup;
    [SerializeField, Tooltip("Ventana de avisos y errores (título + mensaje).")] private PopupAviso popupAviso;

    [SerializeField, Tooltip("Ventana con la lista de estudiantes encontrados.")] private PopupSeleccionAlumno popupSeleccion;

    // ------------------------------------------------------------------
    // Configuración de la API
    // ------------------------------------------------------------------
    [Header("API")] // Título que agrupa estos campos en el inspector.
    [SerializeField, Tooltip("Dirección de la colección de estudiantes, p. ej. \"https://servidor/estudiantes/\". Se rellena en el inspector; si se deja vacía, la búsqueda muestra un aviso de configuración.")] private string urlBase = "";

    // Expresión regular que define qué es un nombre válido:
    //   \A ... \z                   -> el texto COMPLETO debe cumplir el patrón (desde el inicio hasta el final).
    //   (?!.*[\u00AA\u00BA])        -> no puede contener "ª" ni "º" (Unicode los considera letras, pero no lo son).
    //   \p{L}[\p{L}\p{M}]*          -> una palabra: empieza por letra y sigue con letras o tildes combinadas.
    //   (?:[ '\u2019]\p{L}...)*     -> más palabras, separadas por UN espacio, un apóstrofo ' o un apóstrofo tipográfico ’.
    // Así se aceptan "MARÍA JOSÉ" u "O'BRIEN", pero no "ANA  2", " ANA" ni "ANA'".
    private static readonly Regex NombreRegex = new Regex(
        @"\A(?!.*[\u00AA\u00BA])\p{L}[\p{L}\p{M}]*(?:[ '\u2019]\p{L}[\p{L}\p{M}]*)*\z",
        RegexOptions.Compiled); // Compiled: se compila una vez y se reutiliza más rápido en cada validación.

    // Mensaje que aparece en el texto de estado cuando el usuario teclea o pega un carácter no permitido.
    private const string AvisoCaracter = "Carácter no válido: usa letras, espacios o apóstrofos.";

    // ------------------------------------------------------------------
    // Autenticación contra la API
    // ------------------------------------------------------------------
    [Header("Autenticación")] // Título que agrupa estos campos en el inspector.
    [SerializeField, Tooltip("Nombre de la cabecera HTTP donde va la clave (lo indica Swagger en \"Authorize\").")] private string nombreCabecera = "X-API-Key";
    [SerializeField, Tooltip("La clave de la API.")] private string apiKey = "";
    [SerializeField, Tooltip("Actívalo si la API pide \"Authorization: Bearer <clave>\" en vez de nombreCabecera.")] private bool usarBearer = false;

    // ------------------------------------------------------------------
    // Animaciones personalizadas
    // ------------------------------------------------------------------
    [Header("Animaciones personalizadas (opcional)")] // Título en el inspector.
    [SerializeField, Tooltip("Lista de excepciones \"patronus -> prefab\". Los patronus que no estén aquí usan su spritesheet de Resources/PatronusPixelSprites (ver PatronusSpriteAnimation).")] private List<AnimacionPorValor> animacionesPersonalizadas = new List<AnimacionPorValor>();

    // ------------------------------------------------------------------
    // Valores especiales del campo patronus
    // ------------------------------------------------------------------
    [Header("Valores que significan 'sin patronus'")] // Título en el inspector.
    [SerializeField, Tooltip("Textos que, si llegan en el campo patronus, significan que el estudiante NO tiene un patronus con forma de animal.")] private string[] valoresSinPatronus = { "Non-corporeal" };

    // ------------------------------------------------------------------
    // Depuración
    // ------------------------------------------------------------------
    [Header("Depuración")] // Título en el inspector.
    [SerializeField, Tooltip("Si está activo, escribe en la consola el código HTTP de cada respuesta.")] private bool mostrarLogs = true;

    // Indica si hay una petición en curso. Sirve para no lanzar dos búsquedas a la vez
    // y para no mostrar avisos de carácter no válido mientras se busca.
    private bool consultando;

    /// <summary>
    /// Unity lo llama cada vez que el objeto se activa (también al arrancar la escena).
    /// Configura el campo de texto y se suscribe a sus eventos para filtrar y pasar a mayúsculas lo que se escribe.
    /// </summary>
    private void OnEnable()
    {
        if (!inputNombre) return; // Sin campo de texto asignado no hay nada que configurar.

        inputNombre.contentType = TMP_InputField.ContentType.Standard;  // Texto libre: el filtrado lo hace este script.
        inputNombre.lineType = TMP_InputField.LineType.SingleLine;      // Una sola línea (Enter no crea saltos de línea).
        inputNombre.characterLimit = 0;                                  // 0 = sin límite de longitud.

        // onValidateInput se llama con CADA carácter que se teclea, antes de añadirlo al texto.
        inputNombre.onValidateInput += ValidarCaracterNombre;

        // onValueChanged se llama cuando el texto cambia por cualquier motivo (teclear, pegar, borrar...).
        inputNombre.onValueChanged.AddListener(ActualizarNombreEnMayusculas);

        // Limpia también el texto que pudiera tener ya el campo al activarse.
        ActualizarNombreEnMayusculas(inputNombre.text);
    }

    /// <summary>
    /// Filtro de cada tecla pulsada.
    /// Devuelve el carácter a insertar (en mayúscula) o '\0', que para TextMeshPro significa "no insertes nada".
    /// </summary>
    /// <param name="texto">Texto que hay en el campo antes de insertar el carácter.</param>
    /// <param name="posicion">Posición donde se va a insertar el carácter.</param>
    /// <param name="caracter">Carácter que acaba de teclear el usuario.</param>
    /// <returns>El carácter en mayúscula si está permitido, o '\0' para descartarlo.</returns>
    private char ValidarCaracterNombre(string texto, int posicion, char caracter)
    {
        // Carácter permitido: se inserta convertido a mayúscula.
        if (EsCaracterNombre(caracter)) return char.ToUpperInvariant(caracter);

        // Carácter no permitido: se avisa al usuario (salvo si hay una búsqueda en curso, para no tapar "Buscando...").
        if (!consultando) Mostrar(AvisoCaracter);

        return '\0'; // '\0' = descartar el carácter.
    }

    /// <summary>
    /// Decide si un único carácter puede formar parte de un nombre.
    /// Permitidos: letras de cualquier idioma, tildes "sueltas" que se combinan con la letra anterior,
    /// el espacio, el apóstrofo recto ' y el apóstrofo tipográfico ’.
    /// </summary>
    /// <param name="caracter">Carácter a comprobar.</param>
    /// <returns>true si el carácter puede formar parte de un nombre.</returns>
    private static bool EsCaracterNombre(char caracter)
    {
        // "ª" y "º" son letras para Unicode, pero no forman parte de un nombre: se rechazan.
        if (caracter == '\u00AA' || caracter == '\u00BA') return false;

        // Categoría Unicode del carácter (letra, número, signo, marca combinada...).
        System.Globalization.UnicodeCategory categoria = char.GetUnicodeCategory(caracter);

        return char.IsLetter(caracter)                                                  // Cualquier letra: A, Ñ, É,...
            || categoria == System.Globalization.UnicodeCategory.NonSpacingMark        // Tilde combinada (p. ej. la de "É" escrita en dos partes).
            || categoria == System.Globalization.UnicodeCategory.SpacingCombiningMark  // Otras marcas combinadas de algunos alfabetos.
            || caracter == ' '                                                          // Espacio entre nombre y apellidos.
            || caracter == '\''                                                         // Apóstrofo recto: O'Brien.
            || caracter == '\u2019';                                                    // Apóstrofo tipográfico: O’Brien.
    }

    /// <summary>
    /// Limpia el texto completo del campo.
    /// Cubre lo que ValidarCaracterNombre no ve (por ejemplo, texto pegado con Ctrl+V):
    /// elimina los caracteres no permitidos, pasa el resto a mayúsculas y conserva la posición del cursor.
    /// </summary>
    /// <param name="texto">Texto actual del campo.</param>
    private void ActualizarNombreEnMayusculas(string texto)
    {
        if (!inputNombre) return; // Sin campo de texto no hay nada que limpiar.

        string original = texto ?? string.Empty;                       // Texto actual (o vacío si llega null).
        var permitido = new StringBuilder(original.Length);            // Aquí se va construyendo el texto limpio.
        int inicioSeleccion = inputNombre.selectionStringAnchorPosition; // Posición donde empieza la selección/cursor.
        int finSeleccion = inputNombre.selectionStringFocusPosition;     // Posición donde termina la selección/cursor.
        int nuevoInicio = 0;                                           // Posición del inicio de selección en el texto limpio.
        int nuevoFin = 0;                                              // Posición del fin de selección en el texto limpio.

        // Recorre el texto carácter a carácter.
        for (int indice = 0; indice < original.Length; indice++)
        {
            if (!EsCaracterNombre(original[indice])) continue;           // Carácter no permitido: se salta.

            permitido.Append(char.ToUpperInvariant(original[indice]));  // Carácter válido: se añade en mayúscula.

            // Si el carácter conservado estaba antes del cursor, el cursor se desplaza una posición en el texto nuevo.
            if (indice < inicioSeleccion) nuevoInicio++;
            if (indice < finSeleccion) nuevoFin++;
        }

        // Si se ha eliminado algún carácter, se avisa (salvo durante una búsqueda).
        if (permitido.Length != original.Length && !consultando) Mostrar(AvisoCaracter);

        string normalizado = permitido.ToString(); // Texto final ya limpio y en mayúsculas.

        if (inputNombre.text == normalizado) return; // Ya estaba bien: no se toca, así se evita trabajo innecesario.

        // SetTextWithoutNotify cambia el texto SIN volver a lanzar onValueChanged (evita un bucle infinito).
        inputNombre.SetTextWithoutNotify(normalizado);

        // Recoloca el cursor/selección donde estaba el usuario.
        inputNombre.selectionStringAnchorPosition = nuevoInicio;
        inputNombre.selectionStringFocusPosition = nuevoFin;
    }

    /// <summary>
    /// Validación final del nombre completo con la expresión regular NombreRegex.
    /// Es pública y estática para poder usarla también desde otros scripts.
    /// </summary>
    /// <param name="nombre">Nombre completo a validar, ya en mayúsculas y sin espacios repetidos.</param>
    /// <returns>true si el nombre no está vacío y cumple el formato.</returns>
    public static bool EsNombreValido(string nombre)
    {
        // Válido si no está vacío y cumple el patrón completo.
        return !string.IsNullOrWhiteSpace(nombre) && NombreRegex.IsMatch(nombre);
    }

    /// <summary>
    /// Se ejecuta al pulsar el botón "Buscar" (asignado en su evento OnClick del inspector).
    /// Comprueba que todo está bien antes de lanzar la petición a la API.
    /// </summary>
    public void AlPulsarBuscar()
    {
        if (consultando) return; // Ya hay una búsqueda en marcha: se ignora el clic.

        // Toma el texto del campo, quita espacios de los extremos, unifica las tildes (Normalize) y lo pasa a mayúsculas.
        string nombre = inputNombre ? (inputNombre.text ?? string.Empty).Trim().Normalize().ToUpperInvariant() : "";

        // Campo vacío: se avisa y no se busca.
        if (string.IsNullOrEmpty(nombre))
        {
            Avisar("Nombre vacío", "Escribe el nombre del estudiante.");
            return;
        }

        // Sustituye varios espacios seguidos por uno solo ("ANA    MARÍA" -> "ANA MARÍA").
        nombre = Regex.Replace(nombre, " +", " ");

        // El nombre no cumple el formato (p. ej. termina en apóstrofo): se avisa y no se busca.
        if (!EsNombreValido(nombre))
        {
            Avisar(
                "Nombre no válido",
                "Usa letras y separa los nombres con espacios o apóstrofos. No se admiten números ni otros símbolos."
            );
            return;
        }

        // No se ha rellenado la URL de la API en el inspector.
        if (string.IsNullOrWhiteSpace(urlBase))
        {
            Avisar("Configuración incorrecta", "Falta la dirección de la API en el inspector.");
            return;
        }

        // La URL no es una dirección web válida o no empieza por http:// o https://.
        if (!Uri.TryCreate(urlBase, UriKind.Absolute, out Uri direccionBase) ||
            (direccionBase.Scheme != Uri.UriSchemeHttp && direccionBase.Scheme != Uri.UriSchemeHttps))
        {
            Avisar("Configuración incorrecta", "La dirección del servidor no es válida.");
            return;
        }

        // Hay clave de API pero no se sabe en qué cabecera enviarla (solo aplica si no se usa Bearer).
        if (!usarBearer && !string.IsNullOrEmpty(apiKey) && string.IsNullOrWhiteSpace(nombreCabecera))
        {
            Avisar("Configuración incorrecta", "Falta el nombre de la cabecera de autenticación.");
            return;
        }

        // Todo correcto: se lanza la petición como corrutina (se ejecuta a lo largo de varios frames sin congelar la app).
        StartCoroutine(Consultar(nombre));
    }

    /// <summary>
    /// Corrutina que envía la petición a la API y espera la respuesta sin bloquear la interfaz.
    /// </summary>
    /// <param name="nombre">Nombre ya validado que se envía a la API.</param>
    /// <returns>Enumerador que Unity usa para ejecutar la corrutina.</returns>
    IEnumerator Consultar(string nombre)
    {
        consultando = true;     // Marca que hay una búsqueda en curso.
        Mostrar("Buscando..."); // Informa al usuario.

        // try/finally: el bloque finally se ejecuta SIEMPRE (aunque la petición falle o se lance una excepción),
        // así nunca se queda "consultando" bloqueado a true.
        try
        {
            // using: la petición se libera automáticamente al salir del bloque (evita fugas de memoria).
            using (UnityWebRequest request = CrearPeticion(nombre))
            {
                yield return request.SendWebRequest(); // Envía la petición y espera aquí hasta tener respuesta.
                GestionarRespuesta(request);           // Decide qué hacer según el resultado.
            }
        }
        finally
        {
            consultando = false; // La búsqueda ha terminado (bien o mal).

            // Borra "Buscando..." solo si sigue ahí (si se ha mostrado otro mensaje, se respeta).
            if (textoEstado && textoEstado.text == "Buscando...") Mostrar("");
        }
    }

    /// <summary>
    /// Monta la petición HTTP GET con la URL, el tiempo de espera y las cabeceras.
    /// Nunca escribe la clave de la API en la consola.
    /// </summary>
    /// <param name="nombre">Nombre ya validado que se envía a la API.</param>
    /// <returns>La petición configurada, todavía sin enviar.</returns>
    UnityWebRequest CrearPeticion(string nombre)
    {
        // URL final: <urlBase sin "/" final>?name=<nombre codificado para URL>&limit=100
        // EscapeURL convierte espacios, tildes, etc. a un formato seguro para una URL (p. ej. " " -> "+").
        string url = urlBase.TrimEnd('/') + "?name=" + UnityWebRequest.EscapeURL(nombre) + "&limit=100";

        UnityWebRequest request = UnityWebRequest.Get(url); // Crea la petición GET (todavía no se envía).
        try
        {
            request.timeout = 10;                                 // Si el servidor no responde en 10 segundos, se cancela.
            request.SetRequestHeader("Accept", "application/json"); // Indica al servidor que esperamos JSON.

            // Si hay clave de API, se añade la cabecera de autenticación.
            if (!string.IsNullOrEmpty(apiKey))
            {
                string cabecera = usarBearer ? "Authorization" : nombreCabecera;  // Nombre de la cabecera a usar.
                string credencial = usarBearer ? "Bearer " + apiKey : apiKey;     // Valor: con o sin el prefijo "Bearer ".
                request.SetRequestHeader(cabecera, credencial);                   // Añade la cabecera a la petición.
            }

            return request; // Petición lista para enviarse.
        }
        catch
        {
            request.Dispose(); // Si algo falla al configurarla, se libera antes de propagar el error.
            throw;             // Relanza la excepción original.
        }
    }

    /// <summary>
    /// Interpreta el resultado de la petición.
    /// Si fue bien, procesa el JSON; si no, muestra el aviso adecuado según el código HTTP.
    /// </summary>
    /// <param name="request">Petición ya terminada, con su resultado y su código HTTP.</param>
    void GestionarRespuesta(UnityWebRequest request)
    {
        // Escribe el código HTTP en la consola (200 = OK, 404 = no encontrado...) si la depuración está activa.
        if (mostrarLogs) Debug.Log($"[ConsultaAlumno] HTTP {request.responseCode}", this);

        // Petición correcta: se procesa el texto recibido (o vacío si no hubiera contenido).
        if (request.result == UnityWebRequest.Result.Success)
        {
            ProcesarRespuesta(request.downloadHandler?.text ?? string.Empty);
            return;
        }

        // Petición fallida: se elige el mensaje según el código HTTP devuelto por el servidor.
        switch (request.responseCode)
        {
            case 404: // Not Found: no hay estudiantes con ese nombre.
                Avisar("Alumno no encontrado", "No se han encontrado estudiantes con ese nombre.");
                break;

            case 400: // Bad Request: la API considera que la petición es incorrecta.
            case 422: // Unprocessable Entity: el parámetro "name" no tiene un formato aceptado.
                Avisar("Nombre no válido", "La API ha rechazado la búsqueda por nombre.");
                break;

            case 401: // Unauthorized: falta la clave o es incorrecta.
            case 403: // Forbidden: la clave no tiene permiso.
                Avisar("Acceso denegado", "La clave de API no es válida.");
                break;

            default:  // Cualquier otro caso: sin conexión, servidor caído, tiempo agotado...
                Avisar("Error de conexión", "No se ha podido contactar con el servidor.");
                break;
        }
    }

    /// <summary>
    /// Unity lo llama cuando el objeto se desactiva o se destruye.
    /// Se da de baja de los eventos del campo de texto y cancela cualquier búsqueda en curso.
    /// </summary>
    void OnDisable()
    {
        if (inputNombre)
        {
            inputNombre.onValidateInput -= ValidarCaracterNombre;                     // Quita el filtro de teclas.
            inputNombre.onValueChanged.RemoveListener(ActualizarNombreEnMayusculas); // Quita la limpieza del texto.
        }

        StopAllCoroutines(); // Cancela la petición en curso, si la hay.
        consultando = false; // Permite volver a buscar cuando se reactive.
        Mostrar("");         // Limpia el texto de estado.
    }

    /// <summary>
    /// Convierte el JSON recibido en una lista de estudiantes y abre el selector.
    /// Siempre se pasa por la lista, aunque solo haya un resultado, para que el usuario confirme a quién busca.
    /// </summary>
    /// <param name="texto">Texto JSON recibido de la API.</param>
    void ProcesarRespuesta(string texto)
    {
        RespuestaApi[] alumnos; // Aquí se guardarán los estudiantes leídos del JSON.

        try
        {
            alumnos = RespuestaAlumnosJson.Leer(texto); // Lee el JSON (puede ser una lista o un único objeto).
        }
        catch (ArgumentException)
        {
            // El texto no es un JSON con estudiantes: se avisa.
            Avisar("Respuesta incorrecta", "No se ha podido leer la respuesta del servidor.");
            return;
        }

        // JSON correcto pero sin ningún estudiante (lista vacía).
        if (alumnos.Length == 0)
        {
            Avisar("Alumno no encontrado", "No se han encontrado estudiantes con ese nombre.");
            return;
        }

        MostrarSeleccion(alumnos); // Abre la lista para que el usuario elija.
    }

    /// <summary>
    /// Abre la ventana con la lista de estudiantes encontrados.
    /// </summary>
    /// <param name="alumnos">Estudiantes encontrados en la búsqueda.</param>
    void MostrarSeleccion(RespuestaApi[] alumnos)
    {
        // Sin la ventana de la lista asignada en el inspector no se puede mostrar: se avisa en la consola.
        if (!popupSeleccion)
        {
            Debug.LogError("[ConsultaAlumno] Falta asignar PopupSeleccionAlumno en ControladorConsultaAlumno.");
            return;
        }

        Mostrar("");                                     // Limpia el texto de estado.
        popupSeleccion.Open(alumnos, SeleccionarAlumno); // Abre la lista; al elegir uno se llamará a SeleccionarAlumno.
    }

    /// <summary>
    /// Se ejecuta cuando el usuario pulsa un estudiante de la lista.
    /// </summary>
    /// <param name="alumno">Estudiante que ha pulsado el usuario.</param>
    void SeleccionarAlumno(RespuestaApi alumno)
    {
        if (consultando || alumno == null) return; // Ignora la elección si hay una búsqueda en curso o el dato es nulo.

        // La respuesta de la búsqueda ya incluye el patronus, así que no hace falta otra petición.
        MostrarAlumno(alumno);
    }

    /// <summary>
    /// Comprueba el patronus del estudiante elegido y abre la ventana con su animación.
    /// </summary>
    /// <param name="datos">Estudiante elegido, con su patronus.</param>
    void MostrarAlumno(RespuestaApi datos)
    {
        string patronus = datos?.patronus; // Texto del patronus (null si no hay datos).

        // El estudiante no tiene patronus (campo ausente, null o vacío).
        if (string.IsNullOrWhiteSpace(patronus))
        {
            Avisar("Sin patronus", "Este estudiante no tiene patronus asignado.");
            return;
        }

        // El patronus está en la lista "valoresSinPatronus" (comparación sin distinguir mayúsculas).
        // Excepción: los valores que PatronusSpriteAnimation reconoce como "no corpóreo" sí se muestran,
        // porque existe un sprite específico para ellos.
        if (!PatronusSpriteAnimation.EsNoCorporeo(patronus) && valoresSinPatronus != null && valoresSinPatronus.Any(valor =>
                !string.IsNullOrEmpty(valor) && string.Equals(valor.Trim(), patronus.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            Avisar("Sin patronus", "Este estudiante no puede producir un patronus corporeo.");
            return;
        }

        patronus = patronus.Trim(); // Quita espacios sobrantes para que coincida con el nombre del sprite.

        // Sin ventana del patronus asignada en el inspector no se puede mostrar: se avisa en la consola.
        if (!popup)
        {
            Debug.LogError("[ConsultaAlumno] Falta asignar PopupAlumno en ControladorConsultaAlumno.");
            return;
        }

        Mostrar("");                                       // Limpia el texto de estado.
        popup.SetPatronus(patronus, BuscarPrefab(patronus)); // Prepara la animación (prefab propio si existe, si no el spritesheet).
        popup.Open();                                      // Muestra la ventana.
    }

    /// <summary>
    /// Busca en "animacionesPersonalizadas" un prefab para este patronus.
    /// Devuelve el prefab encontrado o null si no hay ninguno (entonces se usará el spritesheet).
    /// </summary>
    /// <param name="valor">Nombre del patronus tal y como llega de la API.</param>
    /// <returns>El prefab configurado para ese patronus, o null si no hay ninguno.</returns>
    GameObject BuscarPrefab(string valor)
    {
        // Normaliza el nombre ("Polar Bear" -> "polar_bear") para comparar sin importar mayúsculas, tildes o espacios.
        string buscado = PatronusSpriteAnimation.NormalizarNombre(valor);

        if (animacionesPersonalizadas == null) return null; // Lista inexistente: no hay prefabs.

        // Devuelve el prefab de la primera entrada válida cuyo nombre normalizado coincida.
        return animacionesPersonalizadas.FirstOrDefault(animacion =>
            animacion != null &&                                                  // La entrada existe...
            animacion.prefab &&                                                   // ...tiene prefab asignado...
            !string.IsNullOrEmpty(animacion.valor) &&                             // ...tiene texto de patronus...
            PatronusSpriteAnimation.NormalizarNombre(animacion.valor) == buscado  // ...y coincide con el buscado.
        )?.prefab; // ?. -> si no se encontró ninguna entrada, devuelve null en lugar de dar error.
    }

    /// <summary>
    /// Muestra un aviso con título y mensaje en PopupAviso.
    /// Si no hay PopupAviso asignado, usa el texto de estado como alternativa.
    /// </summary>
    /// <param name="titulo">Título del aviso.</param>
    /// <param name="mensaje">Texto explicativo del aviso.</param>
    void Avisar(string titulo, string mensaje)
    {
        Mostrar("");                                     // Borra cualquier mensaje anterior del texto de estado.
        if (popupAviso) popupAviso.Open(titulo, mensaje); // Abre la ventana de aviso.
        else Mostrar(mensaje);                           // Sin ventana: el mensaje va al texto de estado.
    }

    /// <summary>
    /// Escribe un mensaje en el texto de estado (si está asignado). Mostrar("") lo deja vacío.
    /// </summary>
    /// <param name="mensaje">Texto a mostrar; una cadena vacía lo borra.</param>
    void Mostrar(string mensaje)
    {
        if (textoEstado) textoEstado.text = mensaje;
    }
}
