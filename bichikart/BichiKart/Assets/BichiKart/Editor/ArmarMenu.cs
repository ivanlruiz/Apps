using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BichiKart.Editor
{
    /// <summary>
    /// Menú "BichiKart → Armar escena Menú": borra todo lo que hay en la escena Menu y la vuelve a armar
    /// con la casa flotando en el espacio, la cámara, las luces y el menú principal. También deja Menu y
    /// Prototipo como las escenas del juego (el juego compilado arranca en Menu).
    /// </summary>
    public static class ArmarMenu
    {
        public const string RutaEscena = "Assets/Scenes/Menu.unity";

        [MenuItem("BichiKart/Armar escena Menú")]
        public static void Armar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Menú] Salí de Play antes de armar la escena.");
                return;
            }

            var escena = SceneManager.GetActiveScene();
            if (escena.path != RutaEscena)
            {
                if (escena.isDirty)
                {
                    Debug.LogWarning("[Menú] La escena abierta tiene cambios sin guardar. Guardalos o descartalos y volvé a probar.");
                    return;
                }
                escena = File.Exists(RutaEscena)
                    ? EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single)
                    : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            foreach (var raiz in escena.GetRootGameObjects()) Object.DestroyImmediate(raiz);

            // Afuera de la casa: luz de relleno azulada, como de noche en el espacio.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.36f, 0.36f, 0.52f);
            RenderSettings.fog = false;
            RenderSettings.skybox = null;

            var casa = new GameObject("Casa del menú").AddComponent<CasaDelMenu>();
            var camara = CrearCamara();
            CrearLuces();
            new GameObject("Menú principal").AddComponent<MenuPrincipal>().Configurar(casa, camara);

            var volumen = new GameObject("Volumen global").AddComponent<Volume>();
            volumen.isGlobal = true;
            volumen.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena, RutaEscena);
            AsegurarEscenasDelJuego();
            Debug.Log("[Menú] Escena armada.");
        }

        /// <summary>El juego compilado arranca en el menú y desde ahí carga la partida.</summary>
        public static void AsegurarEscenasDelJuego()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(RutaEscena, true),
                new EditorBuildSettingsScene("Assets/Scenes/Prototipo.unity", true),
            };
        }

        private static Camera CrearCamara()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.SetPositionAndRotation(new Vector3(0f, 10f, -31f), Quaternion.Euler(10f, 0f, 0f));
            var camara = go.AddComponent<Camera>();
            camara.fieldOfView = 40f;
            camara.nearClipPlane = 0.1f;
            camara.farClipPlane = 1000f;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.05f, 0.05f, 0.15f);
            go.AddComponent<AudioListener>();
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            return camara;
        }

        private static void CrearLuces()
        {
            // Luz cálida desde arriba a la izquierda, del lado de la cámara, y un relleno frío del otro lado.
            var sol = new GameObject("Luz principal").AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.color = new Color(1f, 0.94f, 0.85f);
            sol.intensity = 1.3f;
            sol.shadows = LightShadows.Soft;
            sol.transform.rotation = Quaternion.Euler(42f, 25f, 0f);

            var relleno = new GameObject("Relleno").AddComponent<Light>();
            relleno.type = LightType.Directional;
            relleno.color = new Color(0.62f, 0.7f, 1f);
            relleno.intensity = 0.45f;
            relleno.shadows = LightShadows.None;
            relleno.transform.rotation = Quaternion.Euler(30f, -60f, 0f);
        }
    }
}
