using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class PopupAlumno : MonoBehaviour
{
    public RectTransform ventana;
    public Image imagen;
    public GameObject textoVacio;
    public CanvasGroup menuDetras;
    [Range(0f, 1f)] public float alphaMenuDetras = 0.35f;

    [Header("Animación")]
    public PatronusSpriteAnimation animacion;

    CanvasGroup grupo;
    GameObject instanciaPersonalizada;

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

    public void SetPatronus(string nombre, GameObject prefabPersonalizado = null)
    {
        AsegurarAnimacion();

        if (instanciaPersonalizada) Destroy(instanciaPersonalizada);

        if (prefabPersonalizado)
        {
            instanciaPersonalizada = Instantiate(prefabPersonalizado, imagen.transform);
            if (instanciaPersonalizada.transform is RectTransform rt)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
            }
            animacion.gameObject.SetActive(false);
        }
        else
        {
            animacion.gameObject.SetActive(true);
            animacion.Configurar(nombre);
        }

        PrepararEtiqueta(nombre);
    }

    void AsegurarAnimacion()
    {
        imagen.sprite = null;
        imagen.color = new Color(0.03f, 0.04f, 0.08f, 0.90f);

        if (animacion) return;

        var go = new GameObject("PatronusSpriteAnimation", typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(imagen.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.SetAsFirstSibling();
        animacion = go.AddComponent<PatronusSpriteAnimation>();
    }

    void PrepararEtiqueta(string nombre)
    {
        if (!textoVacio) return;
        var txt = textoVacio.GetComponent<TMP_Text>();
        if (!txt) return;

        var rt = txt.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 14f);
        rt.sizeDelta = new Vector2(0f, 40f);

        txt.text = nombre;
        txt.fontSize = 26;
        txt.fontStyle = FontStyles.Normal;
        txt.characterSpacing = 2;
        txt.color = new Color(0.92f, 0.92f, 0.94f, 1f);
        textoVacio.SetActive(true);
        txt.transform.SetAsLastSibling();
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
