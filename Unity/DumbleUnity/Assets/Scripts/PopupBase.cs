using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Base de las ventanas emergentes (PopupAlumno y PopupAviso).
// Son modales: al abrirse quedan por encima de todo, atenúan el menú de detrás y lo bloquean,
// y aparecen/desaparecen con un pequeño fundido y zoom.
[RequireComponent(typeof(CanvasGroup))]
public abstract class PopupBase : MonoBehaviour
{
    [SerializeField] private RectTransform ventana;                        // la ventana que hace el zoom al abrirse
    [SerializeField] private CanvasGroup menuDetras;                       // menú que se atenúa y bloquea mientras está abierto
    [SerializeField] [Range(0f, 1f)] private float alphaMenuDetras = 0.35f; // opacidad del menú de detrás con el popup abierto

    const float Duracion = 0.15f; // segundos que dura la animación de abrir/cerrar
    CanvasGroup grupo;

    protected virtual void Awake() => grupo = GetComponent<CanvasGroup>();

    // Muestra el popup (llamado desde Open de cada popup)
    protected void Abrir()
    {
        gameObject.SetActive(true); // si estaba desactivado, aquí se ejecuta Awake
        if (!grupo) grupo = GetComponent<CanvasGroup>();
        if (!grupo) return;
        transform.SetAsLastSibling();
        if (menuDetras) menuDetras.interactable = false;
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        StopAllCoroutines();
        StartCoroutine(Animar(0f, 1f));
    }

    // Asígnalo al OnClick del botón de cerrar / aceptar
    public void Close()
    {
        if (!isActiveAndEnabled) return;
        if (!grupo) grupo = GetComponent<CanvasGroup>();
        if (!grupo) return;
        StopAllCoroutines();
        StartCoroutine(Animar(grupo.alpha, 0f, () => gameObject.SetActive(false)));
    }

    // Fundido de 'desde' a 'hasta' (0 = oculto, 1 = visible); usa tiempo real para no depender de Time.timeScale
    IEnumerator Animar(float desde, float hasta, System.Action alFinal = null)
    {
        grupo.blocksRaycasts = true;
        for (float tiempoTranscurrido = 0f; tiempoTranscurrido < Duracion; tiempoTranscurrido += Time.unscaledDeltaTime)
        {
            Aplicar(Mathf.SmoothStep(desde, hasta, tiempoTranscurrido / Duracion));
            yield return null;
        }
        Aplicar(hasta);
        if (ventana) ventana.localScale = Vector3.one;
        alFinal?.Invoke();
    }

    // Restablece el menú también si otra clase desactiva directamente esta ventana.
    protected virtual void OnDisable()
    {
        StopAllCoroutines();
        if (menuDetras)
        {
            menuDetras.interactable = true;
            menuDetras.alpha = 1f;
        }
        if (grupo) grupo.blocksRaycasts = false;
    }

    void Aplicar(float progreso)
    {
        grupo.alpha = progreso;
        if (ventana) ventana.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, progreso);
        if (menuDetras) menuDetras.alpha = Mathf.Lerp(1f, alphaMenuDetras, progreso);
    }
}
