using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using static CONST;

public static class GameEditor
{
    // Play  Loading Scene  in any scene by ctrl +9
    [MenuItem("Tools/PlayFromStartupScene %9")]
    public static void PlayFromStartupScene()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        EditorPrefs.SetString("lastLoadedScenePath", SceneManager.GetActiveScene().path);
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path);
        EditorApplication.isPlaying = true;
    }

    private static void OpenScene(string sceneName)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(
                "Assets/Scenes/" + sceneName + ".unity"
            );
        }
    }

    // Shift + 1  -> open Main scene
    [MenuItem("Tools/OpenMainScene #1")]
    public static void OpenMainScene()
    {
        OpenScene(SCENE_MAIN);
    }
    
    // Shift + 2  -> open Game scene
    [MenuItem("Tools/OpenGameScene #2")]
    public static void OpenGameScene()
    {
        OpenScene(SCENE_GAMEPLAY);
    }
}