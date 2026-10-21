// -----------------------------------------------------------------------------
// Archivo:  PopupSeleccionAlumno.cs
// Proyecto: DumbleUnity
// Autor:    Ivan Salazar
// -----------------------------------------------------------------------------

using System;                     // Action<T>, el tipo del aviso "se ha elegido un alumno".
using System.Collections.Generic; // List<T>, para guardar los botones creados.
using TMPro;                      // TMP_Text, para el título y las etiquetas de los botones.
using UnityEngine;                // API principal de Unity.
using UnityEngine.UI;             // Button, ScrollRect y LayoutRebuilder.

/// <summary>
/// Ventana con una lista desplazable de los estudiantes encontrados en la búsqueda.
/// Crea un botón por estudiante a partir de un botón "plantilla" y avisa a quien la abrió
/// (ConsultaAlumno) de cuál se ha elegido. No hace peticiones a la API: solo muestra y notifica.
/// Su prefab está en Assets/Prefabs/UI/PopupSeleccionAlumno.prefab y está colocado en la escena, desactivado.
/// </summary>
public class PopupSeleccionAlumno : PopupBase
{
    [SerializeField, Tooltip("Texto superior: \"Selecciona un estudiante (N)\".")] private TMP_Text titulo;
    [SerializeField, Tooltip("Contenedor de la lista donde se colocan los botones.")] private RectTransform contenido;
    [SerializeField, Tooltip("Botón de ejemplo que se copia para cada estudiante (permanece oculto).")] private Button plantillaOpcion;
    [SerializeField, Tooltip("Zona con barra de desplazamiento que contiene la lista.")] private ScrollRect desplazamiento;

    private readonly List<GameObject> opciones = new List<GameObject>(); // Botones creados, para poder borrarlos luego.
    private Action<RespuestaApi> alSeleccionar;                           // Función a llamar cuando se elige un estudiante.
    private bool seleccionConfirmada;                                     // Evita que un doble clic elija dos veces.

    /// <summary>
    /// Rellena la lista con los estudiantes y abre la ventana.
    /// "seleccion" es la función que se ejecutará con el estudiante que pulse el usuario.
    /// </summary>
    /// <param name="alumnos">Estudiantes a mostrar en la lista.</param>
    /// <param name="seleccion">Función que se ejecutará con el estudiante elegido.</param>
    public void Open(RespuestaApi[] alumnos, Action<RespuestaApi> seleccion)
    {
        // Sin las referencias del prefab o sin datos no se puede construir la lista.
        if (!contenido || !plantillaOpcion || alumnos == null)
        {
            Debug.LogError("[PopupSeleccionAlumno] Faltan las referencias de la lista.", this);
            return;
        }

        LimpiarOpciones();                          // Borra los botones de una búsqueda anterior.
        alSeleccionar = seleccion;                  // Guarda a quién avisar.
        seleccionConfirmada = false;                // Permite una nueva elección.
        plantillaOpcion.gameObject.SetActive(false); // La plantilla nunca se muestra; solo se copia.

        int cantidad = 0; // Número de botones creados (para el título y el nombre de cada botón).

        // Crea un botón por cada estudiante recibido.
        foreach (RespuestaApi alumno in alumnos)
        {
            if (alumno == null) continue; // Entrada vacía en el JSON: se salta.

            Button opcion = Instantiate(plantillaOpcion, contenido, false); // Copia la plantilla dentro de la lista.
            opcion.gameObject.name = "Alumno_" + cantidad;                 // Nombre en la jerarquía (útil al depurar).

            // Busca el texto del botón (incluso si está desactivado) para escribir el nombre del estudiante.
            TMP_Text etiqueta = opcion.GetComponentInChildren<TMP_Text>(true);
            if (etiqueta)
            {
                etiqueta.richText = false; // Muestra el texto tal cual: evita que "<b>" o similares en los datos cambien el formato.

                // Nombre completo: "nombre apellido", sin espacios sobrantes si falta alguna parte.
                string nombre = (alumno.nombre + " " + alumno.apellido).Trim();

                // Si no hay ni nombre ni apellido, se muestra un texto genérico.
                etiqueta.text = string.IsNullOrEmpty(nombre) ? "Estudiante" : nombre;
            }

            opcion.onClick.RemoveAllListeners(); // Quita cualquier acción que trajera la plantilla.

            // Copia local del estudiante: cada botón debe recordar SU estudiante (no el último del bucle).
            RespuestaApi candidato = alumno;
            opcion.onClick.AddListener(() => Elegir(candidato)); // Al pulsar el botón se elige este estudiante.

            opciones.Add(opcion.gameObject); // Se guarda para poder borrarlo en la próxima búsqueda.
            opcion.gameObject.SetActive(true); // Se muestra (la plantilla estaba oculta, y la copia también).
            cantidad++;                       // Un botón más.
        }

        if (titulo) titulo.text = "Selecciona un estudiante (" + cantidad + ")"; // Título con el número de resultados.

        Abrir(); // Muestra la ventana con la animación de PopupBase.

        // Recalcula ya la colocación de los botones para que la lista tenga su tamaño correcto desde el primer frame.
        LayoutRebuilder.ForceRebuildLayoutImmediate(contenido);

        // Coloca la barra de desplazamiento arriba del todo (1 = arriba, 0 = abajo).
        if (desplazamiento) desplazamiento.verticalNormalizedPosition = 1f;
    }

    /// <summary>
    /// Se ejecuta al pulsar el botón de un estudiante.
    /// </summary>
    private void Elegir(RespuestaApi alumno)
    {
        if (seleccionConfirmada) return; // Ya se eligió uno: se ignoran clics repetidos.
        seleccionConfirmada = true;      // Marca la elección como hecha.

        Action<RespuestaApi> seleccion = alSeleccionar; // Se guarda antes de cerrar, porque OnDisable la borra.

        // Se cierra la ventana ANTES de avisar. Si se cerrara con animación después, su fundido podría
        // desbloquear el menú mientras la siguiente ventana (la del patronus) ya está abierta.
        gameObject.SetActive(false);

        seleccion?.Invoke(alumno); // Avisa a ConsultaAlumno del estudiante elegido (si hay alguien escuchando).
    }

    /// <summary>
    /// Borra todos los botones creados en la búsqueda anterior.
    /// </summary>
    private void LimpiarOpciones()
    {
        foreach (GameObject opcion in opciones)
        {
            if (!opcion) continue; // Ya no existe: nada que borrar.
            opcion.SetActive(false); // Se oculta al momento (Destroy no actúa hasta el final del frame).
            Destroy(opcion);         // Se elimina el botón.
        }

        opciones.Clear(); // Vacía la lista de referencias.
    }

    /// <summary>
    /// Al cerrar la ventana se olvida a quién avisar y se ejecuta la limpieza común de PopupBase.
    /// </summary>
    protected override void OnDisable()
    {
        alSeleccionar = null; // Evita avisos tardíos con una ventana ya cerrada.
        base.OnDisable();     // Restaura el menú de detrás (ver PopupBase.OnDisable).
    }
}
