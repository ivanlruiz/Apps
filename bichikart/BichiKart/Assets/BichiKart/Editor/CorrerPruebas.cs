using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace BichiKart.Editor
{
    /// <summary>
    /// Menú "BichiKart → Correr pruebas": corre las pruebas automáticas del juego y deja el resultado
    /// en la consola y en Temp/bichikart-pruebas.txt.
    /// </summary>
    public static class CorrerPruebas
    {
        public const string Archivo = "Temp/bichikart-pruebas.txt";

        [MenuItem("BichiKart/Correr pruebas")]
        public static void Correr()
        {
            if (File.Exists(Archivo)) File.Delete(Archivo);
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Resultados());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, assemblyNames = new[] { "BichiKart.Pruebas" } }));
        }

        private sealed class Resultados : ICallbacks
        {
            public void RunStarted(ITestAdaptor pruebas) { }

            public void TestStarted(ITestAdaptor prueba) { }

            public void TestFinished(ITestResultAdaptor resultado) { }

            public void RunFinished(ITestResultAdaptor resultado)
            {
                var texto = new StringBuilder();
                texto.AppendLine($"Pasaron {resultado.PassCount}, fallaron {resultado.FailCount}, salteadas {resultado.SkipCount}");
                Fallas(resultado, texto);
                File.WriteAllText(Archivo, texto.ToString());

                if (resultado.FailCount == 0) Debug.Log($"[Pruebas] {texto}");
                else Debug.LogError($"[Pruebas] {texto}");
            }

            private static void Fallas(ITestResultAdaptor resultado, StringBuilder texto)
            {
                if (resultado.HasChildren)
                {
                    foreach (var hijo in resultado.Children) Fallas(hijo, texto);
                }
                else if (resultado.TestStatus == TestStatus.Failed)
                {
                    texto.AppendLine($"FALLA {resultado.FullName}: {resultado.Message}");
                }
            }
        }
    }
}
