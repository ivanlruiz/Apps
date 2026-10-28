using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Un kart que juega la partida: su número, su nombre y su color (el de la carita en la barra y en el
    /// minimapa).
    /// </summary>
    [RequireComponent(typeof(Kart))]
    public sealed class Participante : MonoBehaviour
    {
        [SerializeField] private string nombre = "Blip";
        [SerializeField] private Color color = new Color(0.56f, 0.83f, 1f);

        private Kart _kart;

        public string Nombre => nombre;
        public Color Color => color;

        /// <summary>Se busca la primera vez que se pide (también funciona en el editor, fuera de Play).</summary>
        public Kart Kart => _kart != null ? _kart : _kart = GetComponent<Kart>();

        /// <summary>Su número en la partida (lo pone la partida al empezar).</summary>
        public int Indice { get; set; }

        /// <summary>Lo usa la herramienta que arma la escena.</summary>
        public void Configurar(string nuevoNombre, Color nuevoColor)
        {
            nombre = nuevoNombre;
            color = nuevoColor;
        }
    }
}
