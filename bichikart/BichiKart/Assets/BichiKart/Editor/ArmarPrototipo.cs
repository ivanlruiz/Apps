using System;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
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
    /// Menú "BichiKart → Armar escena Prototipo": borra todo lo que hay en la escena Prototipo y la vuelve a
    /// armar con la Cocina, el kart de Blip, la cámara y la luz. Los números del manejo y las medidas de la
    /// sala quedan en Assets/BichiKart/Ajustes y no se pisan.
    /// </summary>
    public static class ArmarPrototipo
    {
        private const string RutaEscena = "Assets/Scenes/Prototipo.unity";
        private const string CarpetaAjustes = "Assets/BichiKart/Ajustes";

        [MenuItem("BichiKart/Armar escena Prototipo")]
        public static void Armar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Prototipo] Salí de Play antes de armar la escena.");
                return;
            }

            var escena = SceneManager.GetActiveScene();
            if (escena.path != RutaEscena)
            {
                if (escena.isDirty)
                {
                    Debug.LogWarning("[Prototipo] La escena abierta tiene cambios sin guardar. Guardalos o descartalos y volvé a probar.");
                    return;
                }
                escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            }

            int capaSala = AsegurarCapa("Sala");
            int capaKarts = AsegurarCapa("Karts");
            var ajustesSala = AsegurarAjustes<AjustesDeSala>("AjustesDeSala");
            var ajustesKart = AsegurarAjustes<AjustesDelKart>("AjustesDelKart");

            foreach (var raiz in escena.GetRootGameObjects()) Object.DestroyImmediate(raiz);

            ConfigurarAmbiente();
            var cocina = CrearCocina(ajustesSala, capaSala);
            var kart = CrearKart(cocina, ajustesKart, capaKarts, capaSala);
            CrearCamaras(kart, capaSala);
            CrearVolumen();

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("[Prototipo] Escena armada.");
        }

        private static void ConfigurarAmbiente()
        {
            // La sala es cerrada: no hay sol, así que la luz de relleno es cálida y pareja.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.43f, 0.39f);
            RenderSettings.fog = false;
        }

        private static Cocina CrearCocina(AjustesDeSala ajustes, int capaSala)
        {
            var go = new GameObject("Cocina") { layer = capaSala };
            var cocina = go.AddComponent<Cocina>();
            cocina.Ajustes = ajustes;
            return cocina;
        }

        private static Kart CrearKart(Cocina cocina, AjustesDelKart ajustes, int capaKarts, int capaSala)
        {
            var salida = cocina.Salida(0);

            var raiz = new GameObject("Kart de Blip") { layer = capaKarts };
            raiz.transform.position = salida.position + Vector3.up * ajustes.radioEsfera;
            raiz.AddComponent<Rigidbody>().mass = 1f;
            raiz.AddComponent<SphereCollider>().radius = ajustes.radioEsfera;

            var chasis = new GameObject("Chasis") { layer = capaKarts };
            chasis.transform.SetParent(raiz.transform, false);
            chasis.transform.SetPositionAndRotation(salida.position, salida.rotation);
            chasis.AddComponent<ModeloKart>();

            var kart = raiz.AddComponent<Kart>();
            kart.Configurar(ajustes, chasis.transform, 1 << capaSala);
            raiz.AddComponent<FuenteTeclado>();
            raiz.AddComponent<AudioSource>();
            raiz.AddComponent<SonidosDelKart>();
            return kart;
        }

        private static void CrearCamaras(Kart kart, int capaSala)
        {
            var principal = new GameObject("Main Camera") { tag = "MainCamera" };
            var camara = principal.AddComponent<Camera>();
            camara.nearClipPlane = 0.1f;
            camara.farClipPlane = 300f;
            principal.AddComponent<AudioListener>();
            principal.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            principal.AddComponent<CinemachineBrain>();

            var go = new GameObject("Cámara del kart");
            var detras = kart.Chasis.position - kart.Chasis.forward * 6f + Vector3.up * 2.5f;
            go.transform.SetPositionAndRotation(detras, Quaternion.LookRotation(kart.Chasis.position - detras));

            var vcam = go.AddComponent<CinemachineCamera>();
            vcam.Target.TrackingTarget = kart.Chasis;
            var lente = LensSettings.Default;
            lente.FieldOfView = 60f;
            lente.NearClipPlane = 0.1f;
            lente.FarClipPlane = 300f;
            vcam.Lens = lente;

            // Detrás del kart, a 6,5 de distancia y un poco desde arriba. Gira con el kart (LockToTarget):
            // cuando suba por las paredes, la cámara va a girar con él.
            var orbita = go.AddComponent<CinemachineOrbitalFollow>();
            orbita.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbita.Radius = 6.5f;
            orbita.TargetOffset = new Vector3(0f, 1.1f, 0f);
            var seguimiento = orbita.TrackerSettings;
            seguimiento.BindingMode = BindingMode.LockToTarget;
            seguimiento.PositionDamping = new Vector3(0.15f, 0.15f, 0.15f);
            seguimiento.AngularDampingMode = AngularDampingMode.Quaternion;
            seguimiento.QuaternionDamping = 0.25f;
            orbita.TrackerSettings = seguimiento;
            orbita.HorizontalAxis = Eje(0f, new Vector2(-180f, 180f), true);
            orbita.VerticalAxis = Eje(14f, new Vector2(-5f, 55f), false);

            var mira = go.AddComponent<CinemachineRotationComposer>();
            mira.TargetOffset = new Vector3(0f, 1.3f, 0f);
            mira.Damping = new Vector2(0.15f, 0.15f);

            // Si una pared queda entre el kart y la cámara, la cámara se acerca.
            var paredes = go.AddComponent<CinemachineDeoccluder>();
            paredes.CollideAgainst = 1 << capaSala;
            paredes.MinimumDistanceFromTarget = 1f;
            var esquivar = paredes.AvoidObstacles;
            esquivar.Enabled = true;
            esquivar.CameraRadius = 0.3f;
            esquivar.Strategy = CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
            esquivar.Damping = 0.2f;
            esquivar.DampingWhenOccluded = 0f;
            paredes.AvoidObstacles = esquivar;

            go.AddComponent<CamaraDelKart>();
        }

        private static InputAxis Eje(float centro, Vector2 rango, bool vuelta)
        {
            return new InputAxis
            {
                Value = centro,
                Center = centro,
                Range = rango,
                Wrap = vuelta,
                Recentering = new InputAxis.RecenteringSettings { Enabled = true, Wait = 1f, Time = 0.8f },
            };
        }

        private static void CrearVolumen()
        {
            var go = new GameObject("Volumen global");
            var volumen = go.AddComponent<Volume>();
            volumen.isGlobal = true;
            volumen.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
        }

        // ------------------------------------------------------------------ ayudas

        private static T AsegurarAjustes<T>(string nombre) where T : ScriptableObject
        {
            string ruta = $"{CarpetaAjustes}/{nombre}.asset";
            var ajustes = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (ajustes != null) return ajustes;

            if (!AssetDatabase.IsValidFolder(CarpetaAjustes)) AssetDatabase.CreateFolder("Assets/BichiKart", "Ajustes");
            ajustes = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(ajustes, ruta);
            AssetDatabase.SaveAssets();
            return ajustes;
        }

        private static int AsegurarCapa(string nombre)
        {
            int capa = LayerMask.NameToLayer(nombre);
            if (capa >= 0) return capa;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var capas = tagManager.FindProperty("layers");
            for (int i = 8; i < capas.arraySize; i++)
            {
                var lugar = capas.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(lugar.stringValue)) continue;
                lugar.stringValue = nombre;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
            throw new InvalidOperationException($"No quedan capas libres para \"{nombre}\".");
        }
    }
}
