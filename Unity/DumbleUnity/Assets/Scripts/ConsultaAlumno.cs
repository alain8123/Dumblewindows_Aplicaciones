using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

// Modelo parcial: solo el campo que nos interesa del JSON del estudiante
[Serializable]
public class RespuestaApi
{
    public string patronus; // nombre exacto de la clave en el JSON
}

[Serializable]
public class ImagenPorValor
{
    public string valor;   // el string que devuelve la API (ej. "ciervo")
    public Sprite sprite;  // la imagen que se mostrará
}

public class ConsultaAlumno : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField inputId;
    public TMP_Text textoEstado;       // opcional: mensajes de error / cargando
    public PopupAlumno popup;

    [Header("API")]
    // Debe terminar en "/" y SIN llaves. El ID escrito en el campo se añade al final.
    public string urlBase = "https://dumblewindows-api.tail6bccfa.ts.net/estudiantes/";

    [Header("Autenticación")]
    public string nombreCabecera = "X-API-Key"; // el nombre que indique Swagger (botón Authorize)
    public string apiKey = "";                  // rellénala en el inspector, no en el código
    public bool usarBearer = false;             // actívalo si Swagger pide "Authorization: Bearer ..."

    [Header("Mapeo string -> imagen")]
    public List<ImagenPorValor> imagenes = new List<ImagenPorValor>();

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
            Mostrar("Escribe un ID");
            return;
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
            else if (request.responseCode == 401 || request.responseCode == 403)
            {
                Mostrar("Clave de API no válida");
                Debug.LogError("[ConsultaAlumno] Autenticación rechazada: " + texto);
            }
            else if (request.responseCode == 404)
            {
                Mostrar("No existe ese ID");
            }
            else if (request.responseCode == 422)
            {
                Mostrar("ID con formato no válido");
                Debug.LogError("[ConsultaAlumno] La API rechazó el ID: " + texto);
            }
            else
            {
                Mostrar("Error de conexión");
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
            Mostrar("La API no devolvió un JSON válido");
            Debug.LogError("[ConsultaAlumno] La respuesta no es un objeto JSON. Revisa la URL base.");
            return;
        }

        RespuestaApi datos = null;
        try
        {
            datos = JsonUtility.FromJson<RespuestaApi>(limpio);
        }
        catch (ArgumentException e)
        {
            Mostrar("Respuesta ilegible");
            Debug.LogError("[ConsultaAlumno] Error al leer el JSON: " + e.Message);
            return;
        }

        string patronus = datos != null ? datos.patronus : null;
        if (mostrarLogs) Debug.Log("[ConsultaAlumno] Patronus: " + (patronus ?? "(vacío)"));

        Sprite sprite = BuscarSprite(patronus);

        Mostrar("");
        popup.SetImagen(sprite); // si es null, el popup muestra el texto "Imagen"
        popup.Open();
    }

    Sprite BuscarSprite(string valor)
    {
        if (string.IsNullOrEmpty(valor)) return null;

        foreach (var par in imagenes)
        {
            if (par == null || string.IsNullOrEmpty(par.valor)) continue;
            if (string.Equals(par.valor.Trim(), valor.Trim(), StringComparison.OrdinalIgnoreCase))
                return par.sprite;
        }
        return null;
    }

    void Mostrar(string mensaje)
    {
        if (textoEstado) textoEstado.text = mensaje;
    }
}