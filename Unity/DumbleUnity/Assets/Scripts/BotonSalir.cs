using UnityEngine;
using UnityEngine.EventSystems;

// Botón X de la esquina inferior derecha: cierra la aplicación.
// Al pasar el ratón por encima crece un poco y se inclina, como un sello que se levanta.
public class BotonSalir : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Min(1f)] private float escalaHover = 1.12f;
    [SerializeField] private float giroHover = -6f;
    [SerializeField, Min(1f)] float velocidad = 8f;

    bool encima;
    float progresoHover;

    // Asignado al OnClick del botón
    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnPointerEnter(PointerEventData eventData) => encima = true;
    public void OnPointerExit(PointerEventData eventData) => encima = false;

    void OnDisable()
    {
        encima = false;
        progresoHover = 0f;
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }

    void Update()
    {
        progresoHover = Mathf.MoveTowards(progresoHover, encima ? 1f : 0f, velocidad * Time.unscaledDeltaTime);
        float suavizadoHover = Mathf.SmoothStep(0f, 1f, progresoHover);
        transform.localScale = Vector3.one * Mathf.Lerp(1f, escalaHover, suavizadoHover);
        transform.localRotation = Quaternion.Euler(0f, 0f, giroHover * suavizadoHover);
    }
}
