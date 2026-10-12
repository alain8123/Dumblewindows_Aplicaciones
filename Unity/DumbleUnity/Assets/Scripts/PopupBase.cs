using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Base de las ventanas emergentes (PopupAlumno y PopupAviso).
// Son modales: al abrirse quedan por encima de todo, atenúan el menú de detrás y lo bloquean,
// y aparecen/desaparecen con un pequeño fundido y zoom.
[RequireComponent(typeof(CanvasGroup))]
public abstract class PopupBase : MonoBehaviour
{
    public RectTransform ventana;                        // la ventana que hace el zoom al abrirse
    public CanvasGroup menuDetras;                       // menú que se atenúa y bloquea mientras está abierto
    [Range(0f, 1f)] public float alphaMenuDetras = 0.35f; // opacidad del menú de detrás con el popup abierto

    const float Duracion = 0.15f; // segundos que dura la animación de abrir/cerrar
    CanvasGroup grupo;

    protected virtual void Awake() => grupo = GetComponent<CanvasGroup>();

    // Muestra el popup (llamado desde Open de cada popup)
    protected void Abrir()
    {
        gameObject.SetActive(true); // si estaba desactivado, aquí se ejecuta Awake
        if (!grupo) grupo = GetComponent<CanvasGroup>();
        transform.SetAsLastSibling();
        if (menuDetras) menuDetras.interactable = false;
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        StopAllCoroutines();
        StartCoroutine(Animar(0f, 1f));
    }

    // Asígnalo al OnClick del botón de cerrar / aceptar
    public void Close()
    {
        if (menuDetras) menuDetras.interactable = true;
        StopAllCoroutines();
        StartCoroutine(Animar(1f, 0f, () => gameObject.SetActive(false)));
    }

    // Fundido de 'desde' a 'hasta' (0 = oculto, 1 = visible); usa tiempo real para no depender de Time.timeScale
    IEnumerator Animar(float desde, float hasta, System.Action alFinal = null)
    {
        grupo.blocksRaycasts = true;
        for (float t = 0f; t < Duracion; t += Time.unscaledDeltaTime)
        {
            Aplicar(Mathf.SmoothStep(desde, hasta, t / Duracion));
            yield return null;
        }
        Aplicar(hasta);
        if (ventana) ventana.localScale = Vector3.one;
        alFinal?.Invoke();
    }

    void Aplicar(float k)
    {
        grupo.alpha = k;
        if (ventana) ventana.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, k);
        if (menuDetras) menuDetras.alpha = Mathf.Lerp(1f, alphaMenuDetras, k);
    }
}
