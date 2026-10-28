using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Bichikarts
{
    /// <summary>Atajos para armar la interfaz (uGUI) desde código.</summary>
    public static class Ui
    {
        public static Font Display;
        public static Font Body;

        /// <summary>Se llama en cada botón apretado (para el sonido).</summary>
        public static Action Click;

        public static void Init()
        {
            Display = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            Body = Resources.Load<Font>("Fonts/Sniglet-ExtraBold");
            var fallback = Builtin();
            if (Display == null) Display = fallback;
            if (Body == null) Body = fallback;
        }

        private static Font Builtin()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (Exception) { }
            if (f == null)
            {
                try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (Exception) { }
            }
            return f;
        }

        // ------------------------------------------------------------------ nodos y anclas

        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Fill(RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Rt(Component c) => (RectTransform)c.transform;

        public static LayoutElement Size(Component c, float width = -1, float height = -1, float flexWidth = -1, float flexHeight = -1)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (width >= 0) { le.preferredWidth = width; le.minWidth = width; }
            if (height >= 0) { le.preferredHeight = height; le.minHeight = height; }
            if (flexWidth >= 0) le.flexibleWidth = flexWidth;
            if (flexHeight >= 0) le.flexibleHeight = flexHeight;
            return le;
        }

        // ------------------------------------------------------------------ piezas

        public static Image Image(Transform parent, Sprite sprite, Color color, string name = "Imagen")
        {
            var rt = Node(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        public static Image Panel(Transform parent, Color color, string name = "Panel", int radius = 18)
        {
            return Image(parent, Art.UiRound(radius), color, name);
        }

        public static Text Text(Transform parent, string text, int size, Color color, bool display = false, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rt = Node(parent, "Texto");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = display ? Display : Body;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        /// <summary>Contorno de tinta y sombra, para títulos que se lean sobre cualquier fondo.</summary>
        public static Text Inked(Text t, float thickness = 3f)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = Palette.Ink;
            o.effectDistance = new Vector2(thickness, -thickness);
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = Palette.WithAlpha(Palette.Ink, 0.8f);
            s.effectDistance = new Vector2(0, -thickness * 2f);
            return t;
        }

        public static Button Button(Transform parent, string label, Color fill, UnityAction onClick, int fontSize = 34, bool dots = false, Color? textColor = null)
        {
            var rt = Node(parent, "Botón " + label);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Art.UiButton();
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = fill;

            if (dots)
            {
                var d = Image(rt, Art.UiDots(), Color.white, "Puntitos");
                d.type = UnityEngine.UI.Image.Type.Tiled;
                Fill(d.rectTransform, 12, 16, 12, 10);
            }

            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.selectedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.colorMultiplier = 1.1f;
            colors.fadeDuration = 0.06f;
            b.colors = colors;
            b.onClick.AddListener(() => Click?.Invoke());
            if (onClick != null) b.onClick.AddListener(onClick);

            if (!string.IsNullOrEmpty(label))
            {
                var t = Text(rt, label, fontSize, textColor ?? Palette.Ink, true);
                Fill(t.rectTransform, 10, 10, 10, 4);
            }
            return b;
        }

        public static Text Label(Button b) => b.GetComponentInChildren<Text>();

        public static T Group<T>(Transform t, float spacing, int padding = 0, TextAnchor align = TextAnchor.MiddleCenter, bool expandWidth = false, bool expandHeight = false)
            where T : HorizontalOrVerticalLayoutGroup
        {
            var g = t.gameObject.AddComponent<T>();
            g.spacing = spacing;
            g.padding = new RectOffset(padding, padding, padding, padding);
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = expandWidth;
            g.childForceExpandHeight = expandHeight;
            return g;
        }

        public static GridLayoutGroup Grid(Transform t, Vector2 cell, Vector2 spacing, int columns)
        {
            var g = t.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = cell;
            g.spacing = spacing;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = columns;
            g.childAlignment = TextAnchor.UpperCenter;
            return g;
        }

        /// <summary>Un botón que se puede marcar como elegido (borde) o bloqueado (candado con estrellas).</summary>
        public sealed class Chip
        {
            public readonly Button Button;
            public readonly Image Fill;
            public readonly Image Icon;
            public readonly Text Caption;
            private readonly Image _ring;
            private readonly GameObject _lock;
            private readonly Text _lockText;

            public Chip(Transform parent, string caption, Sprite icon, Color fill, UnityAction onClick, int fontSize = 22)
            {
                Button = Ui.Button(parent, "", fill, onClick);
                Fill = (Image)Button.targetGraphic;
                var rt = Rt(Button);

                if (icon != null)
                {
                    Icon = Image(rt, icon, Color.white, "Ícono");
                    Icon.preserveAspect = true;
                    Ui.Fill(Icon.rectTransform, 8, string.IsNullOrEmpty(caption) ? 12 : 30, 8, 6);
                }
                if (!string.IsNullOrEmpty(caption))
                {
                    Caption = Text(rt, caption, fontSize, Palette.Ink);
                    if (icon != null) Place(Caption.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 8), new Vector2(0, 28));
                    else Ui.Fill(Caption.rectTransform, 8, 10, 8, 4);
                    if (icon != null)
                    {
                        Caption.rectTransform.anchorMin = new Vector2(0f, 0f);
                        Caption.rectTransform.anchorMax = new Vector2(1f, 0f);
                    }
                }

                _ring = Image(rt, Art.UiFrame(), Palette.Cheese, "Elegido");
                Ui.Fill(_ring.rectTransform, -7, -7, -7, -7);
                _ring.gameObject.SetActive(false);

                var lockPanel = Panel(rt, new Color(0.08f, 0.04f, 0.16f, 0.78f), "Bloqueado", 14);
                Ui.Fill(lockPanel.rectTransform, 4, 8, 4, 4);
                _lock = lockPanel.gameObject;
                var star = Image(lockPanel.transform, Art.Star(), Palette.Cheese, "Estrella");
                Place(star.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-16, 0), new Vector2(30, 30));
                _lockText = Text(lockPanel.transform, "", 26, Palette.Cheese, true);
                Place(_lockText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(16, 0), new Vector2(50, 40));
                _lock.SetActive(false);
            }

            public void SetSelected(bool on) => _ring.gameObject.SetActive(on);

            public void SetLocked(bool locked, int cost)
            {
                _lock.SetActive(locked);
                _lockText.text = cost.ToString();
                Button.interactable = !locked;
            }
        }
    }
}
