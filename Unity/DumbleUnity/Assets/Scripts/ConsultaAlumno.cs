using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

// Modelo parcial: solo el campo que nos interesa del JSON del estudiante
[Serializable]
public class RespuestaApi
{
    public string nombre;
    public string apellido;
    public string patronus; // nombre exacto de la clave en el JSON
}

// Opcional: animación propia (prefab de UI) para un patronus concreto
[Serializable]
public class AnimacionPorValor
{
    public string valor;      // el string que devuelve la API (ej. "Stag")
    public GameObject prefab; // prefab de UI (Image + Animator, etc.) que sustituye al spritesheet
}

// Controlador de consulta: busca por nombre y muestra el patronus del estudiante elegido en PopupAlumno,
// o un aviso en PopupAviso si algo falla.
public class ConsultaAlumno : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField inputNombre;
    [SerializeField] private TMP_Text textoEstado;       // opcional: texto "Buscando..."
    [SerializeField] private PopupAlumno popup;          // muestra la animación del patronus
    [SerializeField] private PopupAviso popupAviso;      // avisos de error

    [SerializeField] private PopupSeleccionAlumno popupSeleccion; // se carga de Resources si no se asigna

    [Header("API")]
    // Dirección de la colección de estudiantes (se indica en el inspector); la búsqueda utiliza el parámetro name.
    [SerializeField] private string urlBase = "";
    
    // Admite nombres internacionales y compuestos, sin números ni separadores aislados.
    private static readonly Regex NombreRegex = new Regex(
        @"\A(?!.*[\u00AA\u00BA])\p{L}[\p{L}\p{M}]*(?:[ '\u2019]\p{L}[\p{L}\p{M}]*)*\z",
        RegexOptions.Compiled);
    private const string AvisoCaracter = "Carácter no válido: usa letras, espacios o apóstrofos.";

    [Header("Autenticación")]
    [SerializeField] private string nombreCabecera = "X-API-Key"; // el nombre que indique Swagger (botón Authorize)
    [SerializeField] private string apiKey = "";                  // Una clave incluida en el cliente no debe considerarse secreta.
    [SerializeField] private bool usarBearer = false;             // actívalo si Swagger pide "Authorization: Bearer ..."

    [Header("Animaciones personalizadas (opcional)")]
    // Si un patronus no está aquí, se usa su spritesheet de Resources/PatronusPixelSprites.
    [SerializeField] private List<AnimacionPorValor> animacionesPersonalizadas = new List<AnimacionPorValor>();

    [Header("Valores que significan 'sin patronus'")]
    [SerializeField] private string[] valoresSinPatronus = { "Non-corporeal" };

    [Header("Depuración")]
    [SerializeField] private bool mostrarLogs = true;

    private bool consultando; // evita lanzar dos consultas a la vez

    // Convierte el texto visible al escribir o pegar sin generar eventos recursivos.
    private void OnEnable()
    {
        if (!inputNombre) return;
        inputNombre.contentType = TMP_InputField.ContentType.Standard;
        inputNombre.lineType = TMP_InputField.LineType.SingleLine;
        inputNombre.characterLimit = 0;
        inputNombre.onValidateInput += ValidarCaracterNombre;
        inputNombre.onValueChanged.AddListener(ActualizarNombreEnMayusculas);
        ActualizarNombreEnMayusculas(inputNombre.text);
    }

    // Se aplica al teclear; la limpieza posterior cubre también el pegado y cambios externos.
    private char ValidarCaracterNombre(string texto, int posicion, char caracter)
    {
        if (EsCaracterNombre(caracter)) return char.ToUpperInvariant(caracter);
        if (!consultando) Mostrar(AvisoCaracter);
        return '\0';
    }

    private static bool EsCaracterNombre(char caracter)
    {
        // Unicode considera letras los indicadores ordinales; no forman parte de un nombre.
        if (caracter == '\u00AA' || caracter == '\u00BA') return false;
        System.Globalization.UnicodeCategory categoria = char.GetUnicodeCategory(caracter);
        return char.IsLetter(caracter) || categoria == System.Globalization.UnicodeCategory.NonSpacingMark ||
            categoria == System.Globalization.UnicodeCategory.SpacingCombiningMark ||
            caracter == ' ' || caracter == '\'' || caracter == '\u2019';
    }

    private void ActualizarNombreEnMayusculas(string texto)
    {
        if (!inputNombre) return;
        string original = texto ?? string.Empty;
        var permitido = new StringBuilder(original.Length);
        int inicioSeleccion = inputNombre.selectionStringAnchorPosition;
        int finSeleccion = inputNombre.selectionStringFocusPosition;
        int nuevoInicio = 0;
        int nuevoFin = 0;
        for (int indice = 0; indice < original.Length; indice++)
        {
            if (!EsCaracterNombre(original[indice])) continue;
            permitido.Append(char.ToUpperInvariant(original[indice]));
            if (indice < inicioSeleccion) nuevoInicio++;
            if (indice < finSeleccion) nuevoFin++;
        }
        if (permitido.Length != original.Length && !consultando) Mostrar(AvisoCaracter);
        string normalizado = permitido.ToString();
        if (inputNombre.text == normalizado) return;
        inputNombre.SetTextWithoutNotify(normalizado);
        inputNombre.selectionStringAnchorPosition = nuevoInicio;
        inputNombre.selectionStringFocusPosition = nuevoFin;
    }

    // La validación final también protege las consultas iniciadas desde otros scripts.
    public static bool EsNombreValido(string nombre)
    {
        return !string.IsNullOrWhiteSpace(nombre) && NombreRegex.IsMatch(nombre);
    }

    // Asígnalo al OnClick del botón Buscar
    public void AlPulsarBuscar()
    {
        if (consultando) return;

        string nombre = inputNombre ? (inputNombre.text ?? string.Empty).Trim().Normalize().ToUpperInvariant() : "";
        if (string.IsNullOrEmpty(nombre))
        {
            Avisar("Nombre vacío", "Escribe el nombre del estudiante.");
            return;
        }

        nombre = Regex.Replace(nombre, " +", " ");
        if (!EsNombreValido(nombre))
        {
            Avisar(
                "Nombre no válido",
                "Usa letras y separa los nombres con espacios o apóstrofos. No se admiten números ni otros símbolos."
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(urlBase))
        {
            Avisar("Configuración incorrecta", "Falta la dirección de la API en el inspector.");
            return;
        }
        if (!Uri.TryCreate(urlBase, UriKind.Absolute, out Uri direccionBase) ||
            (direccionBase.Scheme != Uri.UriSchemeHttp && direccionBase.Scheme != Uri.UriSchemeHttps))
        {
            Avisar("Configuración incorrecta", "La dirección del servidor no es válida.");
            return;
        }
        if (!usarBearer && !string.IsNullOrEmpty(apiKey) && string.IsNullOrWhiteSpace(nombreCabecera))
        {
            Avisar("Configuración incorrecta", "Falta el nombre de la cabecera de autenticación.");
            return;
        }

        StartCoroutine(Consultar(nombre));
    }

    // Petición GET a la API y reacción según el código HTTP de la respuesta
    IEnumerator Consultar(string nombre)
    {
        consultando = true;
        Mostrar("Buscando...");

        // El finally restablece el estado aunque falle la petición o el procesamiento.
        try
        {
            using (UnityWebRequest request = CrearPeticion(nombre))
            {
                yield return request.SendWebRequest();
                GestionarRespuesta(request);
            }
        }
        finally
        {
            consultando = false;
            if (textoEstado && textoEstado.text == "Buscando...") Mostrar("");
        }
    }

    // Centraliza la URL, el tiempo de espera y las cabeceras; no registra credenciales.
    UnityWebRequest CrearPeticion(string nombre)
    {
        string url = urlBase.TrimEnd('/') + "?name=" + UnityWebRequest.EscapeURL(nombre) + "&limit=100";
        UnityWebRequest request = UnityWebRequest.Get(url);
        try
        {
            request.timeout = 10;
            request.SetRequestHeader("Accept", "application/json");
            if (!string.IsNullOrEmpty(apiKey))
            {
                string cabecera = usarBearer ? "Authorization" : nombreCabecera;
                string credencial = usarBearer ? "Bearer " + apiKey : apiKey;
                request.SetRequestHeader(cabecera, credencial);
            }
            return request;
        }
        catch
        {
            request.Dispose();
            throw;
        }
    }

    // Distingue una respuesta válida de errores de formato, acceso o conexión.
    void GestionarRespuesta(UnityWebRequest request)
    {
        if (mostrarLogs) Debug.Log($"[ConsultaAlumno] HTTP {request.responseCode}", this);
        if (request.result == UnityWebRequest.Result.Success)
        {
            ProcesarRespuesta(request.downloadHandler?.text ?? string.Empty);
            return;
        }

        switch (request.responseCode)
        {
            case 404:
                Avisar("Alumno no encontrado", "No se han encontrado estudiantes con ese nombre.");
                break;
            case 400:
            case 422:
                Avisar("Nombre no válido", "La API ha rechazado la búsqueda por nombre.");
                break;
            case 401:
            case 403:
                Avisar("Acceso denegado", "La clave de API no es válida.");
                break;
            default:
                Avisar("Error de conexión", "No se ha podido contactar con el servidor.");
                break;
        }
    }

    // Una desactivación cancela la consulta y permite volver a buscar al reactivar.
    void OnDisable()
    {
        if (inputNombre)
        {
            inputNombre.onValidateInput -= ValidarCaracterNombre;
            inputNombre.onValueChanged.RemoveListener(ActualizarNombreEnMayusculas);
        }
        StopAllCoroutines();
        consultando = false;
        Mostrar("");
    }

    // Cualquier coincidencia pasa por el selector, incluso si solo hay un estudiante.
    void ProcesarRespuesta(string texto)
    {
        RespuestaApi[] alumnos;
        try
        {
            alumnos = RespuestaAlumnosJson.Leer(texto);
        }
        catch (ArgumentException)
        {
            Avisar("Respuesta incorrecta", "No se ha podido leer la respuesta del servidor.");
            return;
        }

        if (alumnos.Length == 0)
        {
            Avisar("Alumno no encontrado", "No se han encontrado estudiantes con ese nombre.");
            return;
        }

        MostrarSeleccion(alumnos);
    }

    // El selector es un prefab independiente: no modifica los popups ni botones existentes.
    void MostrarSeleccion(RespuestaApi[] alumnos)
    {
        if (!popupSeleccion)
        {
            PopupSeleccionAlumno plantilla = Resources.Load<PopupSeleccionAlumno>("UI/PopupSeleccionAlumno");
            Transform contenedor = popupAviso ? popupAviso.transform.parent : popup ? popup.transform.parent : null;
            if (!plantilla || !contenedor)
            {
                Avisar("Configuración incorrecta", "Falta el prefab del selector de alumnos o el contenedor de ventanas.");
                return;
            }
            popupSeleccion = Instantiate(plantilla, contenedor, false);
            popupSeleccion.ConfigurarFondoComo(popupAviso ? (PopupBase)popupAviso : popup);
        }
        Mostrar("");
        popupSeleccion.Open(alumnos, SeleccionarAlumno);
    }

    void SeleccionarAlumno(RespuestaApi alumno)
    {
        if (consultando || alumno == null) return;
        // La lista ya incluye el Patronus: no es necesaria otra consulta.
        MostrarAlumno(alumno);
    }

    // Se conserva el tratamiento de los Patronus y de las animaciones personalizadas.
    void MostrarAlumno(RespuestaApi datos)
    {
        string patronus = datos?.patronus;

        // El estudiante existe pero no tiene patronus (campo ausente, null o vacío)
        if (string.IsNullOrWhiteSpace(patronus))
        {
            Avisar("Sin patronus", "Este estudiante no tiene patronus asignado.");
            return;
        }

        // La API marca explícitamente que no tiene patronus corpóreo (ej. "Non-corporeal")
        if (!PatronusSpriteAnimation.EsNoCorporeo(patronus) && valoresSinPatronus != null && valoresSinPatronus.Any(valor =>
                !string.IsNullOrEmpty(valor) && string.Equals(valor.Trim(), patronus.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            Avisar("Sin patronus", "Este estudiante no puede producir un patronus corporeo.");
            return;
        }

        patronus = patronus.Trim();

        if (!popup)
        {
            Debug.LogError("[ConsultaAlumno] Falta asignar PopupAlumno en ControladorConsultaAlumno.");
            return;
        }

        Mostrar("");
        popup.SetPatronus(patronus, BuscarPrefab(patronus));
        popup.Open();
    }

    // Prefab propio configurado en el inspector para ese patronus (o null si no hay)
    GameObject BuscarPrefab(string valor)
    {
        string buscado = PatronusSpriteAnimation.NormalizarNombre(valor);
        if (animacionesPersonalizadas == null) return null;
        return animacionesPersonalizadas.FirstOrDefault(animacion =>
            animacion != null && animacion.prefab &&
            !string.IsNullOrEmpty(animacion.valor) &&
            PatronusSpriteAnimation.NormalizarNombre(animacion.valor) == buscado)?.prefab;
    }

    // Muestra un aviso en el popup (o en el texto de estado si no hay popup)
    void Avisar(string titulo, string mensaje)
    {
        Mostrar("");
        if (popupAviso) popupAviso.Open(titulo, mensaje);
        else Mostrar(mensaje);
    }

    void Mostrar(string mensaje)
    {
        if (textoEstado) textoEstado.text = mensaje;
    }
}
