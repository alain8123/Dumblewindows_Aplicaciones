using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Lista modal reutilizable. Solo notifica la elección; la consulta permanece en ConsultaAlumno.
public class PopupSeleccionAlumno : PopupBase
{
    [SerializeField] private TMP_Text titulo;
    [SerializeField] private RectTransform contenido;
    [SerializeField] private Button plantillaOpcion;
    [SerializeField] private ScrollRect desplazamiento;

    private readonly List<GameObject> opciones = new List<GameObject>();
    private Action<RespuestaApi> alSeleccionar;
    private bool seleccionConfirmada;

    public void Open(RespuestaApi[] alumnos, Action<RespuestaApi> seleccion)
    {
        if (!contenido || !plantillaOpcion || alumnos == null)
        {
            Debug.LogError("[PopupSeleccionAlumno] Faltan las referencias de la lista.", this);
            return;
        }

        LimpiarOpciones();
        alSeleccionar = seleccion;
        seleccionConfirmada = false;
        plantillaOpcion.gameObject.SetActive(false);
        int cantidad = 0;
        foreach (RespuestaApi alumno in alumnos)
        {
            if (alumno == null || string.IsNullOrWhiteSpace(alumno.id)) continue;
            Button opcion = Instantiate(plantillaOpcion, contenido, false);
            opcion.gameObject.name = "Alumno_" + alumno.id;
            TMP_Text etiqueta = opcion.GetComponentInChildren<TMP_Text>(true);
            if (etiqueta)
            {
                etiqueta.richText = false;
                string nombre = (alumno.nombre + " " + alumno.apellido).Trim();
                etiqueta.text = (string.IsNullOrEmpty(nombre) ? "Estudiante" : nombre) + "\n" + alumno.id;
            }
            opcion.onClick.RemoveAllListeners();
            RespuestaApi candidato = alumno;
            opcion.onClick.AddListener(() => Elegir(candidato));
            opciones.Add(opcion.gameObject);
            opcion.gameObject.SetActive(true);
            cantidad++;
        }
        if (titulo) titulo.text = "Selecciona un estudiante (" + cantidad + ")";
        Abrir();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contenido);
        if (desplazamiento) desplazamiento.verticalNormalizedPosition = 1f;
    }

    private void Elegir(RespuestaApi alumno)
    {
        if (seleccionConfirmada) return;
        seleccionConfirmada = true;
        Action<RespuestaApi> seleccion = alSeleccionar;
        // Cerrar antes del callback evita que el fundido desbloquee otro popup recién abierto.
        gameObject.SetActive(false);
        seleccion?.Invoke(alumno);
    }

    private void LimpiarOpciones()
    {
        foreach (GameObject opcion in opciones)
        {
            if (!opcion) continue;
            opcion.SetActive(false);
            Destroy(opcion);
        }
        opciones.Clear();
    }

    protected override void OnDisable()
    {
        alSeleccionar = null;
        base.OnDisable();
    }
}
