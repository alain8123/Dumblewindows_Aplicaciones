using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    public GameObject prefab; // prefab de UI (Image + Animator, etc.) que sustituye a la animación procedural
}

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
    // Si un patronus no está aquí, se usa la animación procedural generada por código.
    public List<AnimacionPorValor> animacionesPersonalizadas = new List<AnimacionPorValor>();

    [Header("Valores que significan 'sin patronus'")]
    public string[] valoresSinPatronus = { "Non-corporeal" };

    [Header("Depuración")]
    public bool mostrarLogs = true;

    bool consultando;

    // Asígnalo al OnClick del botón Buscar
    public void AlPulsarBuscar()
    {
        if (consultando) return;

        string id = inputId != null ? inputId.text.Trim() : "";

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

            if (request.result == UnityWebRequest.Result.Success)
            {
                ProcesarRespuesta(texto);
            }
            else if (request.responseCode == 404)
            {
                Avisar("ID no encontrada", "No existe ningún estudiante con ese ID.");
            }
            else if (request.responseCode == 400 || request.responseCode == 422)
            {
                Avisar("ID no válida", "La API ha rechazado ese ID.");
                Debug.LogWarning("[ConsultaAlumno] La API rechazó el ID: " + texto);
            }
            else if (request.responseCode == 401 || request.responseCode == 403)
            {
                Avisar("Acceso denegado", "La clave de API no es válida.");
                Debug.LogError("[ConsultaAlumno] Autenticación rechazada: " + texto);
            }
            else
            {
                Avisar("Error de conexión", "No se ha podido contactar con el servidor.");
                Debug.LogError($"[ConsultaAlumno] {request.error}");
            }
        }

        consultando = false;
    }

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

        string patronus = datos != null ? datos.patronus : null;

        // El estudiante existe pero no tiene patronus (campo ausente, null o vacío)
        if (string.IsNullOrWhiteSpace(patronus))
        {
            Avisar("Sin patronus", "Este estudiante no tiene patronus asignado.");
            return;
        }

        // La API marca explícitamente que no tiene patronus corpóreo (ej. "Non-corporeal")
        if (EsSinPatronus(patronus))
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

    // ---------- Animaciones personalizadas ----------

    GameObject BuscarPrefab(string valor)
    {
        string buscado = Normalizar(valor);
        foreach (var par in animacionesPersonalizadas)
        {
            if (par == null || par.prefab == null || string.IsNullOrEmpty(par.valor)) continue;
            if (Normalizar(par.valor) == buscado) return par.prefab;
        }
        return null;
    }

    // "Polar Bear" / "polar bear " / "Cérvido" -> "polar_bear" / "cervido"
    static string Normalizar(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        string descompuesto = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (char c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue; // quita tildes
            sb.Append(c == ' ' ? '_' : c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    // ---------- Utilidades ----------

    bool EsSinPatronus(string valor)
    {
        if (valoresSinPatronus == null) return false;
        string v = valor.Trim();
        foreach (var s in valoresSinPatronus)
            if (!string.IsNullOrEmpty(s) && string.Equals(s.Trim(), v, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    // "Jack Rabbit (c. 1986), Wolf (1995-1998)" -> "Wolf"   |   "Stag" -> "Stag"
    static string ExtraerPatronusActual(string valor)
    {
        string v = valor;
        int coma = v.LastIndexOf(',');
        if (coma >= 0) v = v.Substring(coma + 1);

        var sb = new StringBuilder(v.Length);
        int nivel = 0;
        foreach (char c in v)
        {
            if (c == '(') { nivel++; continue; }
            if (c == ')') { if (nivel > 0) nivel--; continue; }
            if (nivel == 0) sb.Append(c);
        }
        return sb.ToString().Trim();
    }

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