using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Ventana emergente modal: mientras está abierta bloquea todo lo de detrás
// y solo se cierra con el botón X.
[RequireComponent(typeof(CanvasGroup))]
public class PopupAlumno : MonoBehaviour
{
    public RectTransform ventana;
    public Image imagen;            // hueco donde irá la foto
    public GameObject textoVacio;   // texto "Imagen" que se ve mientras no hay foto
    public CanvasGroup menuDetras;  // panel del menú, se desactiva mientras el popup está abierto
    [Range(0f, 1f)] public float alphaMenuDetras = 0.35f; // cuánto se apaga el menú mientras el popup está abierto

    CanvasGroup grupo;

    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        if (menuDetras) menuDetras.interactable = false;
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        StopAllCoroutines();
        StartCoroutine(Animar(0f, 1f));
    }

    public void Close()
    {
        if (menuDetras) menuDetras.interactable = true;
        StopAllCoroutines();
        StartCoroutine(Animar(1f, 0f, () => gameObject.SetActive(false)));
    }

    public void SetImagen(Sprite sprite)
    {
        imagen.sprite = sprite;
        imagen.color = sprite ? Color.white : new Color(1f, 1f, 1f, 0.06f);
        imagen.preserveAspect = true;
        if (textoVacio) textoVacio.SetActive(!sprite);
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
