using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

// Modelo parcial: solo el campo que nos interesa del JSON del estudiante
[Serializable]
public class RespuestaApi
{
    public string id;
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

// Controlador de consulta: consulta la API con el ID escrito y muestra el patronus del estudiante en PopupAlumno,
// o un aviso en PopupAviso si algo falla.
public class ConsultaAlumno : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField inputId;
    [SerializeField] private TMP_Text textoEstado;       // opcional: texto "Buscando..."
    [SerializeField] private PopupAlumno popup;          // muestra la animación del patronus
    [SerializeField] private PopupAviso popupAviso;      // avisos de error

    [SerializeField] private PopupSeleccionAlumno popupSeleccion; // se carga de Resources si no se asigna

    [Header("API")]
    // Debe terminar en "/" y SIN llaves. El ID escrito en el campo se añade al final.
    [SerializeField] private string urlBase = "https://dumblewindows-api.tail6bccfa.ts.net/estudiantes/";
    
    // Conserva la configuración guardada en escenas y prefabs al renombrar el campo.
    [FormerlySerializedAs("validarFormatoUuid")]
    [SerializeField] private bool validarFormatoId = true;

    // Dos letras, dos grupos de tres letras y un sufijo opcional de dos dígitos.
    private static readonly Regex IdRegex = new Regex(
        @"^[A-Z]{2}-[A-Z]{3}-[A-Z]{3}(-[0-9]{2})?$",
        RegexOptions.Compiled
    );

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
        if (!inputId) return;
        inputId.onValueChanged.AddListener(ActualizarIdEnMayusculas);
        ActualizarIdEnMayusculas(inputId.text);
    }

    private void ActualizarIdEnMayusculas(string texto)
    {
        if (!inputId) return;

        string textoEnMayusculas = (texto ?? string.Empty).ToUpperInvariant();
        if (inputId.text == textoEnMayusculas) return;

        int inicioSeleccion = inputId.selectionStringAnchorPosition;
        int finSeleccion = inputId.selectionStringFocusPosition;
        inputId.SetTextWithoutNotify(textoEnMayusculas);
        inputId.selectionStringAnchorPosition = Mathf.Clamp(inicioSeleccion, 0, textoEnMayusculas.Length);
        inputId.selectionStringFocusPosition = Mathf.Clamp(finSeleccion, 0, textoEnMayusculas.Length);
    }

    // Asígnalo al OnClick del botón Buscar
    public void AlPulsarBuscar()
    {
        if (consultando) return;

        string id = inputId ? (inputId.text ?? string.Empty).Trim() : "";
        if (string.IsNullOrEmpty(id))
        {
            Avisar("ID no válido", "Escribe el ID del estudiante.");
            return;
        }

        // Normaliza el identificador antes de validarlo y enviarlo a la API.
        id = (id ?? string.Empty).Trim().ToUpperInvariant();

        if (validarFormatoId && !IdRegex.IsMatch(id))
        {
            Avisar(
                "ID no válido",
                "El ID no tiene un formato correcto (AA-BBB-CCC o AA-BBB-CCC-00)."
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(urlBase) ||
            !Uri.TryCreate(urlBase, UriKind.Absolute, out Uri direccionBase) ||
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

        StartCoroutine(Consultar(id));
    }

    // Petición GET a la API y reacción según el código HTTP de la respuesta
    IEnumerator Consultar(string id, RespuestaApi seleccionado = null)
    {
        consultando = true;
        Mostrar("Buscando...");

        // El finally restablece el estado aunque falle la petición o el procesamiento.
        try
        {
            using (UnityWebRequest request = CrearPeticion(id, seleccionado))
            {
                yield return request.SendWebRequest();
                GestionarRespuesta(request, seleccionado);
            }
        }
        finally
        {
            consultando = false;
            if (textoEstado && textoEstado.text == "Buscando...") Mostrar("");
        }
    }

    // Centraliza la URL, el tiempo de espera y las cabeceras; no registra credenciales.
    UnityWebRequest CrearPeticion(string id, RespuestaApi seleccionado = null)
    {
        string url = urlBase.TrimEnd('/') + "/" + UnityWebRequest.EscapeURL(id);
        // El ID base ambiguo vuelve a producir una lista. La búsqueda por nombre permite
        // recuperar el detalle y después seleccionar exclusivamente el ID elegido.
        if (seleccionado != null && !Regex.IsMatch(id, @"-[0-9]{2}$"))
        {
            string nombreCompleto = (seleccionado.nombre + " " + seleccionado.apellido).Trim();
            url = urlBase.TrimEnd('/') + "?name=" + UnityWebRequest.EscapeURL(nombreCompleto) + "&limit=100";
        }
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
    void GestionarRespuesta(UnityWebRequest request, RespuestaApi seleccionado)
    {
        if (mostrarLogs) Debug.Log($"[ConsultaAlumno] HTTP {request.responseCode}", this);
        if (request.result == UnityWebRequest.Result.Success)
        {
            ProcesarRespuesta(request.downloadHandler?.text ?? string.Empty, seleccionado);
            return;
        }

        switch (request.responseCode)
        {
            case 404:
                Avisar("Alumno no encontrado", "No existe ningún estudiante con ese ID.");
                break;
            case 400:
            case 422:
                Avisar("ID no válido", "La API ha rechazado ese ID.");
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
        if (inputId) inputId.onValueChanged.RemoveListener(ActualizarIdEnMayusculas);
        StopAllCoroutines();
        consultando = false;
        Mostrar("");
    }

    // Una respuesta puede ser un detalle o una lista de candidatos sin Patronus.
    void ProcesarRespuesta(string texto, RespuestaApi seleccionado)
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
            Avisar("Alumno no encontrado", "No existe ningún estudiante con ese ID.");
            return;
        }

        if (seleccionado != null)
        {
            // Nunca mostramos otro alumno aunque la búsqueda por nombre sea aproximada.
            RespuestaApi detalle = RespuestaAlumnosJson.BuscarPorId(alumnos, seleccionado.id);
            if (detalle == null)
            {
                Avisar("Alumno no encontrado", "La API no ha devuelto el detalle del alumno seleccionado.");
                return;
            }
            MostrarAlumno(detalle);
            return;
        }

        if ((texto ?? string.Empty).TrimStart().StartsWith("["))
        {
            MostrarSeleccion(alumnos);
            return;
        }

        MostrarAlumno(alumnos[0]);
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
        if (consultando || alumno == null || string.IsNullOrWhiteSpace(alumno.id)) return;
        StartCoroutine(Consultar(alumno.id, alumno));
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

        // Si trae varios (ej. "Jack Rabbit (c. 1986), Wolf (1995-1998)") nos quedamos con el último
        patronus = ExtraerPatronusActual(patronus);

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

    // "Jack Rabbit (c. 1986), Wolf (1995-1998)" -> "Wolf"   |   "Stag" -> "Stag"
    static string ExtraerPatronusActual(string valor)
    {
        string ultimoPatronus = valor.Substring(valor.LastIndexOf(',') + 1); // lo que va tras la última coma
        var nombreSinFechas = new StringBuilder(ultimoPatronus.Length);
        int profundidadParentesis = 0; // profundidad de paréntesis: se descarta todo lo que hay dentro
        foreach (char caracter in ultimoPatronus)
        {
            if (caracter == '(') profundidadParentesis++;
            else if (caracter == ')') profundidadParentesis = Math.Max(0, profundidadParentesis - 1);
            else if (profundidadParentesis == 0) nombreSinFechas.Append(caracter);
        }
        return nombreSinFechas.ToString().Trim();
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
