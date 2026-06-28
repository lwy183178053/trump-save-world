using UnityEngine;
using System;
using System.Collections.Generic;

namespace CWGame
{
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    [SerializeField]
    private UIBase[] uiPanels;

    [Header("暂停设置")]
    [SerializeField]
    private KeyCode cancelKey = KeyCode.Escape;

    private readonly Dictionary<Type, UIBase> _panelDict = new();

    public bool triggerOnStart { get; set; } = false;

    /// <summary>
    /// 暂停请求事件，由 PauseUI 等订阅
    /// </summary>
    public static event Action OnPauseRequested;

    private void Awake()
    {
        Instance = this;
        // ServiceLocator.Register<IUIManager>(this);
        RegisterPanels();
    }

    private void Start()
    {
        // 只关闭Inspector中手动拖入的面板，不影响面板自注册（面板在Start中自注册，时序不确定）
        if (uiPanels != null)
        {
            foreach (var panel in uiPanels)
            {
                panel?.Close();
            }
        }
    }

    /// <summary>
    /// 注册Inspector中配置的面板
    /// </summary>
    private void RegisterPanels()
    {
        if (uiPanels == null) return;

        foreach (var panel in uiPanels)
        {
            if (panel == null) continue;

            var type = panel.GetType();
            if (!_panelDict.ContainsKey(type))
            {
                _panelDict.Add(type, panel);
            }
        }
    }

    /// <summary>
    /// 面板自注册（面板在Start中调用）
    /// </summary>
    public void RegisterPanel(UIBase panel)
    {
        if (panel == null) return;

        var type = panel.GetType();
        if (!_panelDict.ContainsKey(type))
        {
            _panelDict.Add(type, panel);
        }
    }
    public void UnRegisterPanel(UIBase panel)
    {
        if (panel == null) return;

        var type = panel.GetType();
        if (_panelDict.ContainsKey(type))
        {
            _panelDict.Remove(type);
        }
    }

    /// <summary>
    /// 获取面板（按类型）
    /// </summary>
    public T GetPanel<T>() where T : UIBase
    {
        var type = typeof(T);
        if (_panelDict.TryGetValue(type, out var panel))
        {
            return panel as T;
        }
        return null;
    }

    void Update()
    {
        if (Input.GetKeyDown(cancelKey))
        {
            OnPauseRequested?.Invoke(); // C# event，非 Unity object，null propagation 安全
        }
    }

    // private void OnDestroy()
    // {
    //     ServiceLocator.Unregister<IUIManager>();
    // }
}
}
