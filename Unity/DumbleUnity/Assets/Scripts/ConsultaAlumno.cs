using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

// Modelo parcial: solo el campo que nos interesa del JSON del estudiante
[Serializable]
public class RespuestaApi
{
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

    [Header("API")]
    // Debe terminar en "/" y SIN llaves. El ID escrito en el campo se añade al final.
    [SerializeField] private string urlBase = "https://dumblewindows-api.tail6bccfa.ts.net/estudiantes/";
    [SerializeField] private bool validarFormatoUuid = true; // evita llamar a la API si el ID no tiene formato UUID

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

        if (validarFormatoUuid)
        {
            if (!Guid.TryParse(id, out Guid guid))
            {
                Avisar("ID no válido", "El ID no tiene un formato correcto.");
                return;
            }
            id = guid.ToString(); // formato estándar con guiones y en minúsculas
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
    IEnumerator Consultar(string id)
    {
        consultando = true;
        Mostrar("Buscando...");

        // El finally restablece el estado aunque falle la petición o el procesamiento.
        try
        {
            using (UnityWebRequest request = CrearPeticion(id))
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
    UnityWebRequest CrearPeticion(string id)
    {
        string url = urlBase.TrimEnd('/') + "/" + UnityWebRequest.EscapeURL(id);
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
        StopAllCoroutines();
        consultando = false;
        Mostrar("");
    }

    // Lee el JSON, decide qué patronus mostrar y abre el popup
    void ProcesarRespuesta(string texto)
    {
        string limpio = (texto ?? string.Empty).TrimStart();

        // Si la URL fuese incorrecta, el servidor podría devolver HTML en vez de JSON
        if (!limpio.StartsWith("{"))
        {
            Avisar("Error", "La respuesta del servidor no es válida.");
            Debug.LogError("[ConsultaAlumno] La respuesta no es un objeto JSON. Revisa la URL base.");
            return;
        }

        RespuestaApi datos;
        try
        {
            datos = JsonUtility.FromJson<RespuestaApi>(limpio);
        }
        catch (ArgumentException e)
        {
            Avisar("Error", "No se ha podido leer la respuesta del servidor.");
            Debug.LogError("[ConsultaAlumno] Error al leer el JSON: " + e.Message);
            return;
        }

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
