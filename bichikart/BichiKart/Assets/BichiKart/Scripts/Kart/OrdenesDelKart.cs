using System;

namespace BichiKart
{
    /// <summary>
    /// Lo que se le pide al kart en un paso de física. El kart solo obedece órdenes: pueden venir del
    /// teclado, de la compu o, más adelante, de la red.
    /// </summary>
    [Serializable]
    public struct OrdenesDelKart
    {
        /// <summary>De -1 (frenar y marcha atrás) a 1 (acelerar).</summary>
        public float Avance;

        /// <summary>De -1 (izquierda) a 1 (derecha).</summary>
        public float Giro;

        /// <summary>Se apretó saltar desde el paso anterior.</summary>
        public bool Saltar;

        /// <summary>Se mantiene apretado derrapar.</summary>
        public bool Derrapar;

        /// <summary>Se apretó el botón de gravedad desde el paso anterior.</summary>
        public bool Gravedad;

        /// <summary>Se apretó la bocina desde el paso anterior.</summary>
        public bool Bocina;

        /// <summary>Se mantiene apretado (o se apretó desde el paso anterior) usar: por ejemplo, meterse en un caño.</summary>
        public bool Usar;
    }

    /// <summary>De dónde saca un kart sus órdenes.</summary>
    public interface IFuenteDeOrdenes
    {
        /// <summary>
        /// Lo llama el kart una vez por paso de física. Los botones apretados se cuentan una sola vez:
        /// después de leerlos, vuelven a cero.
        /// </summary>
        OrdenesDelKart LeerOrdenes();
    }
}
