// -----------------------------------------------------------------------------
// Archivo:  RespuestaAlumnosJson.cs
// Proyecto: DumbleUnity
// Autor:    Ivan Salazar
// -----------------------------------------------------------------------------

using System;      // ArgumentException y [Serializable].
using UnityEngine; // JsonUtility, el lector de JSON incluido en Unity.

/// <summary>
/// Convierte el texto JSON que devuelve la API en un array de RespuestaApi (un elemento por estudiante).
/// Acepta los dos formatos que puede enviar la API:
/// <br/>- Una lista:        [ { "nombre": "...", "patronus": "..." }, { ... } ]
/// <br/>- Un único objeto:  { "nombre": "...", "patronus": "..." }
/// Es "static" porque no guarda ningún estado: solo ofrece la función Leer.
/// </summary>
public static class RespuestaAlumnosJson
{
    /// <summary>
    /// JsonUtility NO sabe leer un JSON que empieza directamente por "[" (una lista suelta).
    /// Truco: se envuelve la lista en un objeto { "alumnos": [ ... ] } y se lee con esta clase auxiliar.
    /// </summary>
    [Serializable] // Necesario para que JsonUtility pueda rellenarla.
    private class ListaAlumnos
    {
        public RespuestaApi[] alumnos; // Recibe el contenido de la clave "alumnos" del objeto envoltorio.
    }

    /// <summary>
    /// Devuelve los estudiantes contenidos en el texto JSON.
    /// Lanza ArgumentException si el texto no contiene estudiantes válidos (ConsultaAlumno lo captura y avisa).
    /// </summary>
    /// <param name="texto">Texto JSON recibido de la API.</param>
    /// <returns>Los estudiantes contenidos en el JSON (puede estar vacío).</returns>
    /// <exception cref="ArgumentException">El texto no contiene una lista ni un estudiante válido.</exception>
    public static RespuestaApi[] Leer(string texto)
    {
        // Evita null y quita espacios o saltos de línea del principio y del final.
        string json = (texto ?? string.Empty).Trim();

        // Caso 1: el JSON es una lista de estudiantes.
        if (json.StartsWith("["))
        {
            // Envuelve la lista para que JsonUtility pueda leerla (ver ListaAlumnos).
            ListaAlumnos lista = JsonUtility.FromJson<ListaAlumnos>("{\"alumnos\":" + json + "}");

            // Devuelve los estudiantes leídos o un array vacío si no había ninguno.
            return lista?.alumnos ?? new RespuestaApi[0];
        }

        // Caso 2: el JSON es un único estudiante.
        if (json.StartsWith("{"))
        {
            RespuestaApi alumno = JsonUtility.FromJson<RespuestaApi>(json); // Lee el objeto directamente.

            // Solo se acepta si tiene al menos nombre o patronus (si no, probablemente es otro tipo de objeto, p. ej. un error).
            if (alumno != null && (!string.IsNullOrWhiteSpace(alumno.nombre) || !string.IsNullOrWhiteSpace(alumno.patronus)))
                return new[] { alumno }; // Se devuelve como un array de un solo elemento.
        }

        // Ni lista ni estudiante válido: el texto no es lo que esperábamos.
        throw new ArgumentException("La respuesta no contiene alumnos válidos.");
    }
}
