using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Ventana emergente de aviso (ID no válida, no encontrada, sin patronus...).
// Es modal: mientras está abierta bloquea el menú de detrás y se cierra con el botón Aceptar.
[RequireComponent(typeof(CanvasGroup))]
public class PopupAviso : MonoBehaviour
{
    public RectTransform ventana;
    public TMP_Text titulo;
    public TMP_Text mensaje;
    public CanvasGroup menuDetras;
    [Range(0f, 1f)] public float alphaMenuDetras = 0.35f;

    CanvasGroup grupo;

    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
    }

    public void Open(string textoTitulo, string textoMensaje)
    {
        gameObject.SetActive(true);   // si estaba desactivado, aquí se ejecuta Awake
        if (!grupo) grupo = GetComponent<CanvasGroup>();

        if (titulo) titulo.text = textoTitulo;
        if (mensaje) mensaje.text = textoMensaje;

        transform.SetAsLastSibling();
        if (menuDetras) menuDetras.interactable = false;
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);

        StopAllCoroutines();
        StartCoroutine(Animar(0f, 1f));
    }

    // Asígnalo al OnClick del botón Aceptar
    public void Close()
    {
        if (menuDetras) menuDetras.interactable = true;
        StopAllCoroutines();
        StartCoroutine(Animar(1f, 0f, () => gameObject.SetActive(false)));
    }

    IEnumerator Animar(float desde, float hasta, System.Action alFinal = null)
    {
        const float duracion = 0.15f;
        grupo.blocksRaycasts = true;
        for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(desde, hasta, t / duracion);
            grupo.alpha = k;
            if (ventana) ventana.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, k);
            if (menuDetras) menuDetras.alpha = Mathf.Lerp(1f, alphaMenuDetras, k);
            yield return null;
        }
        grupo.alpha = hasta;
        if (ventana) ventana.localScale = Vector3.one;
        if (menuDetras) menuDetras.alpha = Mathf.Lerp(1f, alphaMenuDetras, hasta);
        alFinal?.Invoke();
    }
}
