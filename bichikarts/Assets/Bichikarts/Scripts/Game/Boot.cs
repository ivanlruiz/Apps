using UnityEngine;

namespace Bichikarts
{
    /// <summary>
    /// Arranca el juego en cualquier escena: si no hay un <see cref="GameRoot"/>, lo crea.
    /// Así alcanza con abrir el proyecto y apretar Play.
    /// </summary>
    public static class Boot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            if (Object.FindAnyObjectByType<GameRoot>() != null) return;
            new GameObject("Bichikarts").AddComponent<GameRoot>();
        }
    }
}
