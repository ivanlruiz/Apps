using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Sonidos de un kart: la bocina (cada bichito tiene su tono), el despegue del botón de gravedad, el
    /// salto y el mini turbo.
    /// </summary>
    [RequireComponent(typeof(Kart), typeof(AudioSource))]
    public sealed class SonidosDelKart : MonoBehaviour
    {
        [Tooltip("Tono de la bocina, en vueltas por segundo. Más alto es más agudo.")]
        [SerializeField] private float tonoBocina = 620f;

        private Kart _kart;
        private AudioSource _parlante;
        private AudioClip _bocina, _gravedad, _salto, _turbo;

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
        }

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
            foreach (var clip in new[] { _bocina, _gravedad, _salto, _turbo })
                if (clip != null) Destroy(clip);
        }

        private void TocarBocina() => _parlante.PlayOneShot(_bocina);

        private void CambiarGravedad() => _parlante.PlayOneShot(_gravedad, 0.7f);

        private void Saltar() => _parlante.PlayOneShot(_salto, 0.5f);

        private void ArrancarTurbo() => _parlante.PlayOneShot(_turbo, 0.8f);
    }
}
