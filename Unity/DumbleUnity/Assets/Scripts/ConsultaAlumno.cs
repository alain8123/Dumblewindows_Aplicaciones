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

// Botón Buscar: consulta la API con el ID escrito y muestra el patronus del estudiante en PopupAlumno,
// o un aviso en PopupAviso si algo falla.
public class ConsultaAlumno : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField inputId;
    public TMP_Text textoEstado;       // opcional: texto "Buscando..."
    public PopupAlumno popup;          // muestra la animación del patronus
    public PopupAviso popupAviso;      // avisos de error

    [Header("API")]
    // Debe terminar en "/" y SIN llaves. El ID escrito en el campo se añade al final.
    public string urlBase = "https://dumblewindows-api.tail6bccfa.ts.net/estudiantes/";
    public bool validarFormatoUuid = true; // evita llamar a la API si el ID no tiene formato UUID

    [Header("Autenticación")]
    public string nombreCabecera = "X-API-Key"; // el nombre que indique Swagger (botón Authorize)
    public string apiKey = "";                  // rellénala en el inspector, no en el código
    public bool usarBearer = false;             // actívalo si Swagger pide "Authorization: Bearer ..."

    [Header("Animaciones personalizadas (opcional)")]
    // Si un patronus no está aquí, se usa su spritesheet de Resources/PatronusPixelSprites.
    public List<AnimacionPorValor> animacionesPersonalizadas = new List<AnimacionPorValor>();

    [Header("Valores que significan 'sin patronus'")]
    public string[] valoresSinPatronus = { "Non-corporeal" };

    [Header("Depuración")]
    public bool mostrarLogs = true;

    bool consultando; // evita lanzar dos consultas a la vez

    // Asígnalo al OnClick del botón Buscar
    public void AlPulsarBuscar()
    {
        if (consultando) return;

        string id = inputId ? inputId.text.Trim() : "";
        if (string.IsNullOrEmpty(id))
        {
            Avisar("ID no válida", "Escribe el ID del estudiante.");
            return;
        }

        if (validarFormatoUuid)
        {
            if (!Guid.TryParse(id, out Guid guid))
            {
                Avisar("ID no válida", "El ID no tiene un formato correcto.");
                return;
            }
            id = guid.ToString(); // formato estándar con guiones y en minúsculas
        }

        StartCoroutine(Consultar(id));
    }

    // Petición GET a la API y reacción según el código HTTP de la respuesta
    IEnumerator Consultar(string id)
    {
        consultando = true;
        Mostrar("Buscando...");

        string url = urlBase + UnityWebRequest.EscapeURL(id);
        if (mostrarLogs) Debug.Log("[ConsultaAlumno] URL: " + url);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 10;
            request.SetRequestHeader("Accept", "application/json");
            if (!string.IsNullOrEmpty(apiKey))
            {
                if (usarBearer) request.SetRequestHeader("Authorization", "Bearer " + apiKey);
                else request.SetRequestHeader(nombreCabecera, apiKey);
            }

            yield return request.SendWebRequest();

            string texto = request.downloadHandler != null ? request.downloadHandler.text : "";
            if (mostrarLogs) Debug.Log($"[ConsultaAlumno] HTTP {request.responseCode} - Respuesta: {texto}");

            if (request.result == UnityWebRequest.Result.Success) ProcesarRespuesta(texto);
            else switch (request.responseCode)
            {
                case 404:
                    Avisar("ID no encontrada", "No existe ningún estudiante con ese ID.");
                    break;
                case 400:
                case 422:
                    Avisar("ID no válida", "La API ha rechazado ese ID.");
                    Debug.LogWarning("[ConsultaAlumno] La API rechazó el ID: " + texto);
                    break;
                case 401:
                case 403:
                    Avisar("Acceso denegado", "La clave de API no es válida.");
                    Debug.LogError("[ConsultaAlumno] Autenticación rechazada: " + texto);
                    break;
                default:
                    Avisar("Error de conexión", "No se ha podido contactar con el servidor.");
                    Debug.LogError($"[ConsultaAlumno] {request.error}");
                    break;
            }
        }

        consultando = false;
    }

    // Lee el JSON, decide qué patronus mostrar y abre el popup
    void ProcesarRespuesta(string texto)
    {
        string limpio = texto.TrimStart();

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
        if (valoresSinPatronus != null && valoresSinPatronus.Any(s =>
                !string.IsNullOrEmpty(s) && string.Equals(s.Trim(), patronus.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            Avisar("Sin patronus", "Este estudiante no puede producir un patronus corpóreo.");
            return;
        }

        // Si trae varios (ej. "Jack Rabbit (c. 1986), Wolf (1995-1998)") nos quedamos con el último
        patronus = ExtraerPatronusActual(patronus);

        if (!popup)
        {
            Debug.LogError("[ConsultaAlumno] Falta asignar el PopupAlumno. Ejecuta Tools > Menú > Aplicar estilo.");
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
        return animacionesPersonalizadas
            .Where(p => p != null && p.prefab && !string.IsNullOrEmpty(p.valor))
            .FirstOrDefault(p => PatronusSpriteAnimation.NormalizarNombre(p.valor) == buscado)?.prefab;
    }

    // "Jack Rabbit (c. 1986), Wolf (1995-1998)" -> "Wolf"   |   "Stag" -> "Stag"
    static string ExtraerPatronusActual(string valor)
    {
        string v = valor.Substring(valor.LastIndexOf(',') + 1); // lo que va tras la última coma
        var sb = new StringBuilder(v.Length);
        int nivel = 0; // profundidad de paréntesis: se descarta todo lo que hay dentro
        foreach (char c in v)
        {
            if (c == '(') nivel++;
            else if (c == ')') nivel = Math.Max(0, nivel - 1);
            else if (nivel == 0) sb.Append(c);
        }
        return sb.ToString().Trim();
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
