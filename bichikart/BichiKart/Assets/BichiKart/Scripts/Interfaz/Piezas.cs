using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BichiKart
{
    /// <summary>
    /// Piezas para armar pantallas desde código con el estilo "inflado" del juego: lienzos, imágenes,
    /// textos con borde y botones que se agrandan al pasar por encima. Las usan la interfaz de la partida,
    /// el menú principal y la pausa.
    /// </summary>
    public static class Piezas
    {
        public static readonly Color Panel = new Color(0.11f, 0.14f, 0.25f, 0.85f);
        public static readonly Color Tinta = new Color(0.11f, 0.14f, 0.25f, 0.9f);

        private static Font _letra;

        public static Font Letra => _letra != null ? _letra : _letra = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>Un lienzo encima de todo, que se adapta al tamaño de la pantalla (pensado para 1920 × 1080).</summary>
        public static RectTransform Lienzo(string nombre, int orden, bool conBotones)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var lienzo = go.AddComponent<Canvas>();
            lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            lienzo.sortingOrder = orden;
            var escala = go.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920f, 1080f);
            escala.matchWidthOrHeight = 0.5f;
            if (conBotones) go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        /// <summary>Para que los botones respondan al mouse, al teclado y al joystick.</summary>
        public static void AsegurarEventSystem()
        {
            if (EventSystem.current != null || UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        /// <summary>Una imagen (o un lugar vacío, si no hay dibujo) anclada a un punto de su padre.</summary>
        public static RectTransform Imagen(string nombre, Transform padre, Sprite dibujo, Color color, Vector2 ancla, Vector2 medidas, Vector2 lugar)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            go.layer = padre.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = ancla;
            rect.sizeDelta = medidas;
            rect.anchoredPosition = lugar;
            if (dibujo == null) return rect;

            var imagen = go.AddComponent<Image>();
            imagen.sprite = dibujo;
            imagen.color = color;
            imagen.type = dibujo == Dibujos.Redondeado ? Image.Type.Sliced : Image.Type.Simple;
            imagen.raycastTarget = false;
            return rect;
        }

        public static void Borde(RectTransform rect, Color color, float grosor)
        {
            var borde = rect.gameObject.AddComponent<Outline>();
            borde.effectColor = color;
            borde.effectDistance = new Vector2(grosor, -grosor);
        }

        /// <summary>Texto en negrita, blanco y con borde oscuro, para que se lea sobre cualquier fondo.</summary>
        public static Text Texto(string nombre, Transform padre, int tamano, TextAnchor alineacion, Vector2 ancla, Vector2 medidas, Vector2 lugar)
        {
            var rect = Imagen(nombre, padre, null, Color.clear, ancla, medidas, lugar);
            var texto = rect.gameObject.AddComponent<Text>();
            texto.font = Letra;
            texto.fontSize = tamano;
            texto.fontStyle = FontStyle.Bold;
            texto.alignment = alineacion;
            texto.color = Color.white;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            var borde = rect.gameObject.AddComponent<Outline>();
            borde.effectColor = Tinta;
            borde.effectDistance = new Vector2(3f, -3f);
            return texto;
        }

        /// <summary>Botón redondeado con su texto, que se agranda al pasar el mouse o elegirlo con el joystick.</summary>
        public static Button Boton(string nombre, Transform padre, string texto, Color color, Vector2 ancla, Vector2 medidas, Vector2 lugar, Action alApretar, int tamanoLetra = 34)
        {
            var rect = Imagen(nombre, padre, Dibujos.Redondeado, color, ancla, medidas, lugar);
            Borde(rect, Color.white, 3f);
            var imagen = rect.GetComponent<Image>();
            imagen.raycastTarget = true;

            var boton = rect.gameObject.AddComponent<Button>();
            boton.targetGraphic = imagen;
            var colores = boton.colors;
            colores.normalColor = new Color(0.88f, 0.88f, 0.88f);
            colores.highlightedColor = Color.white;
            colores.selectedColor = Color.white;
            colores.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            colores.fadeDuration = 0.08f;
            boton.colors = colores;
            if (alApretar != null) boton.onClick.AddListener(() => alApretar());
            rect.gameObject.AddComponent<Inflable>();

            if (!string.IsNullOrEmpty(texto)) Texto("Texto", rect, tamanoLetra, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), medidas, Vector2.zero).text = texto;
            return boton;
        }

        /// <summary>Botón redondo con una flecha (para los "◀ ▶" de las opciones).</summary>
        public static Button BotonFlecha(string nombre, Transform padre, bool haciaLaDerecha, Vector2 ancla, Vector2 lugar, Action alApretar)
        {
            var boton = Boton(nombre, padre, null, new Color(0.3f, 0.45f, 0.85f), ancla, new Vector2(56f, 56f), lugar, alApretar);
            ((Image)boton.targetGraphic).sprite = Dibujos.Circulo;
            ((Image)boton.targetGraphic).type = Image.Type.Simple;
            var flecha = Imagen("Flecha", boton.transform, Dibujos.Flecha, Color.white, new Vector2(0.5f, 0.5f), new Vector2(26f, 26f), Vector2.zero);
            flecha.localRotation = Quaternion.Euler(0f, 0f, haciaLaDerecha ? -90f : 90f);
            return boton;
        }
    }

    /// <summary>Agranda un poco lo que tiene el mouse encima (o el joystick elegido). Anda también en pausa.</summary>
    public sealed class Inflable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private bool _encima, _elegido;

        public void OnPointerEnter(PointerEventData e) => _encima = true;
        public void OnPointerExit(PointerEventData e) => _encima = false;
        public void OnSelect(BaseEventData e) => _elegido = true;
        public void OnDeselect(BaseEventData e) => _elegido = false;

        private void OnDisable()
        {
            _encima = _elegido = false;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            float buscado = _encima || _elegido ? 1.08f : 1f;
            float escala = Mathf.Lerp(transform.localScale.x, buscado, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            transform.localScale = Vector3.one * escala;
        }
    }
}
