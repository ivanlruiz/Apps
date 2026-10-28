using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Sonidos de un kart: la bocina (cada bichito tiene su tono), el despegue del botón de gravedad, el
    /// salto, el mini turbo, el "plop" al agarrar la comida y la queja al perderla.
    /// </summary>
    [RequireComponent(typeof(Kart), typeof(AudioSource))]
    public sealed class SonidosDelKart : MonoBehaviour
    {
        [Tooltip("Tono de la bocina, en vueltas por segundo. Más alto es más agudo.")]
        [SerializeField] private float tonoBocina = 620f;

        /// <summary>Lo usa la herramienta que arma la escena.</summary>
        public void Configurar(float nuevoTono) => tonoBocina = nuevoTono;

        private Kart _kart;
        private AudioSource _parlante;
        private AudioClip _bocina, _gravedad, _salto, _turbo, _plop, _queja;

        private void Awake()
        {
            _kart = GetComponent<Kart>();
            _parlante = GetComponent<AudioSource>();
            _parlante.playOnAwake = false;
            _parlante.spatialBlend = 0.5f;

            _bocina = Sonidos.Bocina(tonoBocina);
            _gravedad = Sonidos.Succion();
            _salto = Sonidos.Plop();
            _turbo = Sonidos.Turbo();
            _plop = Sonidos.Plop();
            _queja = Sonidos.Queja(tonoBocina * 0.9f);
        }

        /// <summary>"Plop" del que agarra o roba la comida.</summary>
        public void Agarrar() => _parlante.PlayOneShot(_plop);

        /// <summary>Grito ofendido del que se queda sin la comida.</summary>
        public void Quejarse() => _parlante.PlayOneShot(_queja);

        private void OnEnable()
        {
            _kart.AlTocarBocina += TocarBocina;
            _kart.AlCambiarGravedad += CambiarGravedad;
            _kart.AlSaltar += Saltar;
            _kart.AlArrancarTurbo += ArrancarTurbo;
        }

        private void OnDisable()
        {
            _kart.AlTocarBocina -= TocarBocina;
            _kart.AlCambiarGravedad -= CambiarGravedad;
            _kart.AlSaltar -= Saltar;
            _kart.AlArrancarTurbo -= ArrancarTurbo;
        }

        private void OnDestroy()
        {
            foreach (var clip in new[] { _bocina, _gravedad, _salto, _turbo, _plop, _queja })
                if (clip != null) Destroy(clip);
        }

        private void TocarBocina() => _parlante.PlayOneShot(_bocina);

        private void CambiarGravedad() => _parlante.PlayOneShot(_gravedad, 0.7f);

        private void Saltar() => _parlante.PlayOneShot(_salto, 0.5f);

        private void ArrancarTurbo() => _parlante.PlayOneShot(_turbo, 0.8f);
    }
}
