using PlayerBlock;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadSceneButton : MonoBehaviour
{
    [Header("要加载的场景名")]
    public string sceneName;

    private void Awake()
    {
        UiEffectsUtility.EnsureSceneButtonEffects();

        var button = GetComponent<Button>();
        if (button != null && button.GetComponent<UiButtonFeedback>() == null)
        {
            button.gameObject.AddComponent<UiButtonFeedback>();
        }
    }

    public void LoadScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Scene name is empty!");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}

public class startbutton : LoadSceneButton
{
}
