#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Poligon.Editor
{
    public static class PoligonSetup
    {
        [MenuItem("Poligon/Setup Scene (one click)")]
        public static void SetupScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Remove default camera listener conflict later handled by bootstrap
            var bootstrap = new GameObject("Bootstrap");
            bootstrap.AddComponent<Poligon.GameBootstrap>();

            // Ensure folder
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Poligon.unity");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Poligon.unity", true)
            };

            Debug.Log("Poligon: scene ready. Press Play.");
            EditorUtility.DisplayDialog("Poligon", "Scene created!\n\nPress PLAY.\n\nControls:\nWASD + Mouse\nLMB shoot, R reload\n1-5 weapons\nTab multiplayer", "OK");
        }
    }
}
#endif
