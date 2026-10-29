using System;
using System.Collections.Generic;
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
    /// armar con la Cocina, los cuatro karts, la rebanada de pan, la partida, la cámara, la interfaz, la
    /// pausa y la luz. Los números del manejo, las reglas y las medidas de la sala quedan en Assets/BichiKart/Ajustes y
    /// no se pisan.
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
            var reglas = AsegurarAjustes<ReglasDePartida>("ReglasDePartida");
            var niveles = new[]
            {
                AsegurarAjustes<AjustesDeCompu>("AjustesDeCompuFacil", ComoFacil),
                AsegurarAjustes<AjustesDeCompu>("AjustesDeCompuNormal", ComoNormal),
                AsegurarAjustes<AjustesDeCompu>("AjustesDeCompuDificil"),
            };

            foreach (var raiz in escena.GetRootGameObjects()) Object.DestroyImmediate(raiz);

            ConfigurarAmbiente();
            var cocina = CrearCocina(ajustesSala, capaSala);
            new GameObject("Tubería azul").AddComponent<Tuberia>().Configurar(cocina);
            var participantes = new List<Participante>();
            for (int i = 0; i < Bichitos.Length; i++) participantes.Add(CrearKart(cocina, i, ajustesKart, niveles[1], capaKarts, capaSala));
            var comida = CrearComida(cocina);
            var partida = CrearPartida(reglas, cocina, comida, participantes, niveles);
            var camara = CrearCamaras(participantes[0].Kart, capaSala);
            CrearInterfaz(partida, camara);
            new GameObject("Pausa").AddComponent<Pausa>();
            CrearVolumen();

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            ArmarMenu.AsegurarEscenasDelJuego();
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

        /// <summary>
        /// Los cuatro karts: el primero lo maneja el jugador y los otros son rivales de la compu. Los colores
        /// son los del diseño (cada kart contrasta con su bichito).
        /// </summary>
        private static readonly (string nombre, string carroceria, string bichito, float tonoBocina)[] Bichitos =
        {
            ("Blip", "#ff9f1c", "#8fd3ff", 620f),
            ("Pulpi", "#3ec1f0", "#ff8fc8", 700f),
            ("Zorbo", "#ffd23f", "#9b6bff", 440f),
            ("Mukko", "#ff7eb6", "#7be08a", 520f),
        };

        private static Participante CrearKart(Cocina cocina, int numero, AjustesDelKart ajustes, AjustesDeCompu compu, int capaKarts, int capaSala)
        {
            var (nombre, carroceria, bichito, tonoBocina) = Bichitos[numero];
            var salida = cocina.Salida(numero);

            var raiz = new GameObject($"Kart de {nombre}") { layer = capaKarts };
            raiz.transform.position = salida.position + Vector3.up * ajustes.radioEsfera;
            raiz.AddComponent<Rigidbody>().mass = 1f;
            raiz.AddComponent<SphereCollider>().radius = ajustes.radioEsfera;

            var chasis = new GameObject("Chasis") { layer = capaKarts };
            chasis.transform.SetParent(raiz.transform, false);
            chasis.transform.SetPositionAndRotation(salida.position, salida.rotation);
            var modelo = chasis.AddComponent<ModeloKart>();
            modelo.ColorCarroceria = Formas.Hex(carroceria);
            modelo.ColorBichito = Formas.Hex(bichito);
            modelo.Bichito = nombre;

            var kart = raiz.AddComponent<Kart>();
            kart.Configurar(ajustes, chasis.transform, 1 << capaSala);
            var participante = raiz.AddComponent<Participante>();
            participante.Configurar(nombre, Formas.Hex(bichito));
            raiz.AddComponent<MarcaDeCaida>();
            if (numero == 0) raiz.AddComponent<FuenteTeclado>();
            else raiz.AddComponent<FuenteCompu>().Configurar(compu);
            raiz.AddComponent<AudioSource>();
            raiz.AddComponent<SonidosDelKart>().Configurar(tonoBocina);
            return participante;
        }

        private static Comida CrearComida(Cocina cocina)
        {
            var go = new GameObject("Rebanada de pan");
            go.transform.position = cocina.transform.position + Vector3.up * 1.8f;
            return go.AddComponent<Comida>();
        }

        private static Partida CrearPartida(ReglasDePartida reglas, Cocina cocina, Comida comida, List<Participante> participantes, AjustesDeCompu[] niveles)
        {
            var partida = new GameObject("Partida").AddComponent<Partida>();
            partida.Configurar(reglas, cocina, comida, participantes, niveles);
            return partida;
        }

        // Los valores de arranque de cada nivel de la compu. Solo se usan al crear el archivo: después se
        // ajustan en el Inspector. Difícil usa los valores por defecto de AjustesDeCompu.

        /// <summary>Más lenta para decidir y para manejar, no se adelanta, no derrapa, no usa el caño ni se esconde.</summary>
        private static void ComoFacil(AjustesDeCompu a)
        {
            a.reaccion = 0.7f;
            a.velocidad = 0.82f;
            a.anticipacion = 0f;
            a.anguloGiroCompleto = 45f;
            a.derrapa = false;
            a.distanciaParaGravedad = 4f;
            a.usaCanio = false;
            a.seEsconde = false;
            a.distanciaParaHuir = 5f;
            a.reaccionAGravedad = new Vector2(1.2f, 1.8f);
            a.esperaParaSubir = 5f;
        }

        /// <summary>Entre las dos: usa todo, pero más tarde y un poco más lenta que Difícil.</summary>
        private static void ComoNormal(AjustesDeCompu a)
        {
            a.reaccion = 0.45f;
            a.velocidad = 0.92f;
            a.anticipacion = 0.2f;
            a.anguloParaDerrapar = 65f;
            a.distanciaParaGravedad = 6f;
            a.distanciaParaHuir = 7f;
            a.reaccionAGravedad = new Vector2(0.8f, 1.2f);
            a.esperaParaSubir = 2.5f;
        }

        private static void CrearInterfaz(Partida partida, Camera camara)
        {
            var go = new GameObject("Interfaz", typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            go.AddComponent<Interfaz>().Configurar(partida, camara);
        }

        private static Camera CrearCamaras(Kart kart, int capaSala)
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
            // Arranca un poco desde arriba, para ver por encima de la comida cuando la llevás. Sube hasta casi
            // encima del kart y baja hasta el ras del piso; más abajo, CamaraDelKart levanta la mirada al techo.
            orbita.VerticalAxis = Eje(21f, new Vector2(-5f, 85f), false);

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
            return camara;
        }

        /// <summary>
        /// Un eje de la cámara. Vuelve al centro 1,5 s después de soltar el mouse, pero CamaraDelKart solo
        /// lo deja volver mientras se maneja.
        /// </summary>
        private static InputAxis Eje(float centro, Vector2 rango, bool vuelta)
        {
            return new InputAxis
            {
                Value = centro,
                Center = centro,
                Range = rango,
                Wrap = vuelta,
                Recentering = new InputAxis.RecenteringSettings { Enabled = true, Wait = 1.5f, Time = 0.8f },
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

        /// <param name="alCrear">Valores de arranque, si hay que crear el archivo (si ya existe, no se tocan).</param>
        private static T AsegurarAjustes<T>(string nombre, Action<T> alCrear = null) where T : ScriptableObject
        {
            string ruta = $"{CarpetaAjustes}/{nombre}.asset";
            var ajustes = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (ajustes != null) return ajustes;

            if (!AssetDatabase.IsValidFolder(CarpetaAjustes)) AssetDatabase.CreateFolder("Assets/BichiKart", "Ajustes");
            ajustes = ScriptableObject.CreateInstance<T>();
            alCrear?.Invoke(ajustes);
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
