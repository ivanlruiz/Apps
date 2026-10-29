using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BichiKart
{
    /// <summary>
    /// La pantalla de Opciones, igual en el menú principal y en la pausa: cámara (sensibilidad del mouse,
    /// invertir vertical y anti-mareo), sonido (volumen) y pantalla (completa o ventana, y resolución).
    /// Cada opción se cambia con las flechas y se guarda enseguida.
    /// </summary>
    public sealed class PanelDeOpciones : MonoBehaviour
    {
        private static readonly Color ColorTitulo = new Color(1f, 0.82f, 0.25f);
        private const float AltoFila = 68f;

        private readonly List<(Text valor, Func<string> leer)> _valores = new List<(Text, Func<string>)>();
        private Action _alCerrar;
        private Button _primero;

        public bool Visible => gameObject.activeSelf;

        /// <summary>Arma el panel (escondido) en el medio de <paramref name="padre"/>.</summary>
        public static PanelDeOpciones Crear(Transform padre, Action alCerrar)
        {
            var rect = Piezas.Imagen("Opciones", padre, Dibujos.Redondeado, Piezas.Panel, new Vector2(0.5f, 0.5f), new Vector2(960f, 880f), Vector2.zero);
            Piezas.Borde(rect, Color.white, 4f);
            rect.GetComponent<Image>().raycastTarget = true;
            var panel = rect.gameObject.AddComponent<PanelDeOpciones>();
            panel._alCerrar = alCerrar;
            panel.Armar();
            rect.gameObject.SetActive(false);
            return panel;
        }

        public void Mostrar()
        {
            gameObject.SetActive(true);
            Actualizar();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_primero.gameObject);
        }

        public void Cerrar()
        {
            gameObject.SetActive(false);
            _alCerrar?.Invoke();
        }

        private void OnEnable() => Opciones.AlCambiar += Actualizar;

        private void OnDisable() => Opciones.AlCambiar -= Actualizar;

        private void Actualizar()
        {
            foreach (var (valor, leer) in _valores) valor.text = leer();
        }

        // ------------------------------------------------------------------ armado

        private void Armar()
        {
            float y = -60f;
            var titulo = Piezas.Texto("Título", transform, 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(800f, 80f), new Vector2(0f, y));
            titulo.text = "Opciones";
            y -= 85f;

            Seccion("Cámara", ref y);
            Fila("Sensibilidad del mouse", ref y, () => Opciones.Sensibilidad.ToString(),
                () => Opciones.Sensibilidad--, () => Opciones.Sensibilidad++);
            Fila("Invertir vertical", ref y, () => SiNo(Opciones.InvertirVertical),
                () => Opciones.InvertirVertical = !Opciones.InvertirVertical, () => Opciones.InvertirVertical = !Opciones.InvertirVertical);
            Fila("Anti-mareo", ref y, () => SiNo(Opciones.AntiMareo),
                () => Opciones.AntiMareo = !Opciones.AntiMareo, () => Opciones.AntiMareo = !Opciones.AntiMareo);

            Seccion("Sonido", ref y);
            Fila("Volumen", ref y, () => $"{Opciones.Volumen * 10} %", () => Opciones.Volumen--, () => Opciones.Volumen++);

            Seccion("Pantalla", ref y);
            Fila("Pantalla completa", ref y, () => SiNo(Opciones.PantallaCompleta),
                () => Opciones.PantallaCompleta = !Opciones.PantallaCompleta, () => Opciones.PantallaCompleta = !Opciones.PantallaCompleta);
            Fila("Resolución", ref y, () => $"{Opciones.Resolucion.x} × {Opciones.Resolucion.y}",
                () => CambiarResolucion(-1), () => CambiarResolucion(1));

            Piezas.Boton("Listo", transform, "Listo", new Color(0.25f, 0.7f, 0.42f), new Vector2(0.5f, 0f), new Vector2(300f, 84f), new Vector2(0f, 70f), Cerrar, 40);
        }

        private void Seccion(string nombre, ref float y)
        {
            y -= 8f;
            var texto = Piezas.Texto(nombre, transform, 34, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(860f, 50f), new Vector2(0f, y));
            texto.text = nombre;
            texto.color = ColorTitulo;
            y -= 56f;
        }

        /// <summary>Una opción: su nombre a la izquierda y el valor entre dos flechas a la derecha.</summary>
        private void Fila(string nombre, ref float y, Func<string> leer, Action menos, Action mas)
        {
            var etiqueta = Piezas.Texto(nombre, transform, 32, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(440f, 60f), new Vector2(-210f, y));
            etiqueta.text = nombre;
            var izquierda = Piezas.BotonFlecha("Menos", transform, false, new Vector2(0.5f, 1f), new Vector2(90f, y), menos);
            var valor = Piezas.Texto("Valor", transform, 32, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(240f, 60f), new Vector2(245f, y));
            Piezas.BotonFlecha("Más", transform, true, new Vector2(0.5f, 1f), new Vector2(400f, y), mas);
            _valores.Add((valor, leer));
            if (_primero == null) _primero = izquierda;
            y -= AltoFila;
        }

        private static string SiNo(bool valor) => valor ? "Sí" : "No";

        private static void CambiarResolucion(int paso)
        {
            var lista = Opciones.Resoluciones();
            int i = Mathf.Max(0, lista.IndexOf(Opciones.Resolucion));
            Opciones.Resolucion = lista[(i + paso + lista.Count) % lista.Count];
        }
    }
}
