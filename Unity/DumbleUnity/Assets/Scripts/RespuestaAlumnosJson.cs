using System;
using UnityEngine;

// JsonUtility necesita un objeto contenedor para leer un array JSON de primer nivel.
public static class RespuestaAlumnosJson
{
    [Serializable]
    private class ListaAlumnos
    {
        public RespuestaApi[] alumnos;
    }

    public static RespuestaApi[] Leer(string texto)
    {
        string json = (texto ?? string.Empty).Trim();
        if (json.StartsWith("["))
        {
            ListaAlumnos lista = JsonUtility.FromJson<ListaAlumnos>("{\"alumnos\":" + json + "}");
            return lista?.alumnos ?? new RespuestaApi[0];
        }
        if (json.StartsWith("{"))
        {
            RespuestaApi alumno = JsonUtility.FromJson<RespuestaApi>(json);
            if (alumno != null && (!string.IsNullOrWhiteSpace(alumno.nombre) || !string.IsNullOrWhiteSpace(alumno.patronus)))
                return new[] { alumno };
        }
        throw new ArgumentException("La respuesta no contiene alumnos válidos.");
    }
}
