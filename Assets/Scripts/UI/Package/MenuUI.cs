using UnityEngine;
using System;
using CWGame;
using UnityEngine.SceneManagement;  
public class MenuUI : UIBase
{
    [Header("主菜单UI组件")]
    [SerializeField]
    private GameObject mainMenuPanel;
    public GameObject SettingsPanel;

    public override void Open()
    {
        mainMenuPanel.SetActive(true);
    }

    void Start()
    {
        UIManager.Instance.RegisterPanel(this);
    }

    #region 按钮事件
    public void StartGame()
    {
        // Close();
        SceneManager.LoadScene(1);//加载第一场景
    }
    public void ExitGame()
    {
        Application.Quit();
        print("退出游戏");
    }
    public void LoadGame()
    {
        // ServiceLocator.Get<GameLevelSave>().LoadSavedGame();
    }
    public void ShowSettings()
    {
        mainMenuPanel.SetActive(false);
        SettingsPanel.SetActive(true);
    }
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);//重新加载当前场景
    }
    public void BackToMenu()
    {
        SceneManager.LoadScene(0);//加载主菜单场景
    }
    #endregion
}