using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bichikarts.EditorTools
{
    /// <summary>
    /// La primera vez que se abre el proyecto crea la escena del juego, la agrega a la compilación y
    /// deja configurados el nombre, el ícono de orientación horizontal y el identificador de Android.
    /// También se puede correr a mano desde el menú Bichikarts → Configurar proyecto.
    /// </summary>
    [InitializeOnLoad]
    public static class BichikartsSetup
    {
        private const string ScenePath = "Assets/Bichikarts/Scenes/Bichikarts.unity";
        private const string DoneKey = "Bichikarts.SetupDone";

        static BichikartsSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (File.Exists(ScenePath) && SessionState.GetBool(DoneKey, false)) return;
                SessionState.SetBool(DoneKey, true);
                if (!File.Exists(ScenePath)) Configure(openScene: true);
            };
        }

        [MenuItem("Bichikarts/Configurar proyecto")]
        public static void ConfigureFromMenu() => Configure(openScene: true);

        [MenuItem("Bichikarts/Abrir escena del juego")]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath)) Configure(openScene: false);
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        private static void Configure(bool openScene)
        {
            PlayerSettings.productName = "Bichikarts";
            if (PlayerSettings.companyName == "DefaultCompany") PlayerSettings.companyName = "ivanlruiz";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            const string id = "io.github.ivanlruiz.bichikarts";
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, id);
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, id);
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, id);
#else
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, id);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, id);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, id);
#endif

            bool created = false;
            if (!File.Exists(ScenePath))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cam = new GameObject("Cámara");
                cam.tag = "MainCamera";
                var camera = cam.AddComponent<Camera>();
                camera.orthographic = true;
                camera.transform.position = new Vector3(8f, 4.5f, -10f);
                cam.AddComponent<AudioListener>();
                new GameObject("Bichikarts").AddComponent<GameRoot>();
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
                created = true;
            }

            var scenes = EditorBuildSettings.scenes;
            bool listed = false;
            foreach (var s in scenes)
                if (s.path == ScenePath) listed = true;
            if (!listed)
            {
                var list = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
                foreach (var s in scenes)
                    if (s.path != ScenePath) list.Add(s);
                EditorBuildSettings.scenes = list.ToArray();
            }

            if (openScene && !created && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);

            Debug.Log("Bichikarts: proyecto configurado. Apretá Play para jugar.");
        }
    }
}
