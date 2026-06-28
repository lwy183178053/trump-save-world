using UnityEngine;
using CWGame;
using UnityEngine.SceneManagement;

public class PauseUI : UIBase
{
    private static bool _instanceExists = false;

    void Awake()
    {
        // 防止场景重载时产生多个 PauseUI 实例
        if (_instanceExists)
        {
            Destroy(gameObject);
            return;
        }
        _instanceExists = true;
        DontDestroyOnLoad(gameObject);

        // 订阅暂停请求事件（C# 事件不依赖 Unity active 状态，即使 inactive 也保持订阅）
        UIManager.OnPauseRequested += OnPauseRequested;
    }

    void OnDestroy()
    {
        UIManager.OnPauseRequested -= OnPauseRequested;
    }

    void Start()
    {
        // ServiceLocator.Get<IUIManager>()?.RegisterPanel(this);
        Close();
    }

    private void OnPauseRequested()
    {
        PauseGame();
    }

    #region 按钮事件
    public void PauseGame()
    {
        Time.timeScale = 0;
        Open();
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;
        Close();
    }

    public void RestartGame()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Close();
    }

    public void MenuGame()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
        Close();
    }
    #endregion
}
