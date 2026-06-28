using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitioner : MonoBehaviour
{
    [Tooltip("The exact name of the scene you want to load (e.g. 1_light_Ink)")]
    public string sceneToLoad = "1_light_Ink";

    /// <summary>
    /// Call this from a Unity Event (like Select Entered) to load the next scene.
    /// </summary>
    public void LoadNextScene()
    {
        Debug.Log($"Loading Scene: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);
    }
}
