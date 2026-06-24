using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Core.Runtime
{
    public sealed class QteController : MonoBehaviour
        
    {

        //注：QTE方式为先离散按键再连续按键，如A B N JJJJJJJ
        //按键数X 就是离散数，按键数Y 就是连续数，X+Y就是QTE总数

        //下为对接事件

        /// <summary>
        /// 完成QTE后，触发推特完成事件
        /// </summary>
        private void CompleteQte()
        {
            isActive = false;
            qteText.enabled = false;
            Debug.Log(string.Format("推特{0}完成", twitterId));
            GameEventBus.Publish(GameEventNames.TwitterPosted, twitterId, GameChangeSource.System);//事件名称，当前推特编号，事件来源标记
        }


        //下为调整参数
        [Tooltip("当前推特编号，完成QTE后会通过事件发送")]
        [SerializeField] private int twitterId = 1;

        [Header("核心组件")]
        [Tooltip("显示QTE按键的文本组件，需要在Inspector中绑定！")]
        [SerializeField] private TextMeshProUGUI qteText;

        [Header("QTE配置")]
        [Tooltip("离散QTE按键数量（X），从字母池中随机抽取")]
        [SerializeField] private int discreteCount = 3;

        [Tooltip("连续QTE按键数量（Y），连续按同一个键")]
        [SerializeField] private int continuousCount = 5;

        

        [Header("字体动画配置")]
        [Tooltip("字体正常大小")]
        [SerializeField] private float baseFontSize = 60f;

        [Tooltip("字体最大放大倍数（相对于正常大小）")]
        [SerializeField] private float maxScale = 2f;

        [Tooltip("放大/缩小动画速度（每秒变化倍率）")]
        [SerializeField] private float scaleSpeed = 3f;

        [Tooltip("最后连续QTE完成后的淡去速度")]
        [SerializeField] private float fadeSpeed = 2f;

        [Header("状态")]
        [Tooltip("是否正在进行QTE")]
        [SerializeField] private bool isActive;

        [Tooltip("启用时自动启动QTE")]
        [SerializeField] private bool startOnEnable;

        private readonly List<char> _discreteSequence = new List<char>();
        private char _continuousKey;
        private int _currentDiscreteIndex;
        private int _currentContinuousIndex;
        private bool _isContinuousPhase;
        private bool _isFinalFade;
        private bool _isScalingUp;
        private bool _isScalingDown;
        private float _currentScale;
        private float _currentAlpha;
        private readonly char[] _letterPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private float _targetScale;

        private void Awake()
        {
            if (qteText != null)
            {
                qteText.enabled = false;
                qteText.fontSize = baseFontSize;
                _currentScale = 1f;
                _currentAlpha = 1f;
                qteText.transform.localScale = Vector3.one;
            }
        }

        private void OnEnable()
        {
            if (startOnEnable)
            {
                StartQte();
            }
        }

        private void Update()
        {
            if (!isActive || qteText == null)
            {
                return;
            }

            if (_isFinalFade)
            {
                ProcessFinalFade();
                return;
            }

            ProcessScaleAnimation();
            ProcessKeyInput();
        }

        /// <summary>
        /// UI动效
        /// </summary>
        private void ProcessScaleAnimation()
        {
            if (_isScalingUp)
            {
                _currentScale += Time.deltaTime * scaleSpeed;
                if (_currentScale >= _targetScale)
                {
                    _currentScale = _targetScale;
                    _isScalingUp = false;

                    if (_isContinuousPhase && _currentContinuousIndex >= continuousCount)
                    {
                        StartFinalFade();
                    }
                    else
                    {
                        _isScalingDown = true;

                        if (!_isContinuousPhase)
                        {
                            ShowNextDiscreteKey();
                        }
                    }
                }
                qteText.transform.localScale = Vector3.one * _currentScale;
            }
            else if (_isScalingDown)
            {
                _currentScale -= Time.deltaTime * scaleSpeed;
                if (_currentScale <= 1f)
                {
                    _currentScale = 1f;
                    _isScalingDown = false;

                    if (_isContinuousPhase && _currentContinuousIndex == 0)
                    {
                        EnterContinuousPhase();
                    }
                }
                qteText.transform.localScale = Vector3.one * _currentScale;
            }
        }

        /// <summary>
        /// 监测键盘输入
        /// </summary>
        private void ProcessKeyInput()
        {
            if (Input.anyKeyDown)
            {
                foreach (var key in _letterPool)
                {
                    if (Input.GetKeyDown(char.ToLower(key).ToString()))
                    {
                        OnKeyPressed(char.ToUpper(key));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// QTE按键处理逻辑
        /// </summary>
        private void OnKeyPressed(char key)
        {
            if (_isContinuousPhase)
            {
                HandleContinuousInput(key);
            }
            else
            {
                HandleDiscreteInput(key);
            }
        }

        /// <summary>
        /// 处理离散QTE输入：验证按键是否正确，若有动画则跳过，然后开始放大动画
        /// </summary>
        private void HandleDiscreteInput(char key)
        {
            if (key != _discreteSequence[_currentDiscreteIndex])
            {
                return;
            }

            if (_isScalingUp || _isScalingDown)
            {
                SkipCurrentAnimation();
            }

            _targetScale = maxScale;
            _isScalingUp = true;
        }

        /// <summary>
        /// 处理连续QTE输入：验证按键是否正确，若有动画则跳过，计数+1后开始放大动画
        /// </summary>
        private void HandleContinuousInput(char key)
        {
            if (key != _continuousKey)
            {
                return;
            }

            if (_isScalingUp || _isScalingDown)
            {
                SkipCurrentAnimation();
            }

            _currentContinuousIndex++;

            _targetScale = maxScale;
            _isScalingUp = true;
        }

        /// <summary>
        /// 状态过度：切换到下一个离散按键，若离散阶段结束则进入连续阶段
        /// </summary>
        private void ShowNextDiscreteKey()
        {
            _currentDiscreteIndex++;

            if (_currentDiscreteIndex >= discreteCount)
            {
                _isContinuousPhase = true;
                _currentContinuousIndex = 0;
                qteText.text = _continuousKey.ToString();
                return;
            }

            qteText.text = _discreteSequence[_currentDiscreteIndex].ToString();
        }

        /// <summary>
        /// 跳过按键动画：立即设置到目标状态并切换到下一阶段
        /// </summary>
        private void SkipCurrentAnimation()
        {
            if (_isScalingUp)
            {
                _currentScale = _targetScale;
                qteText.transform.localScale = Vector3.one * _currentScale;
                _isScalingUp = false;

                if (_isContinuousPhase && _currentContinuousIndex >= continuousCount)
                {
                    StartFinalFade();
                    return;
                }
            }
            else if (_isScalingDown)
            {
                _currentScale = 1f;
                qteText.transform.localScale = Vector3.one;
                _isScalingDown = false;

                if (_isContinuousPhase && _currentContinuousIndex == 0)
                {
                    EnterContinuousPhase();
                    return;
                }
            }

            if (!_isContinuousPhase)
            {
                ShowNextDiscreteKey();
            }
        }

        /// <summary>
        /// 进入连续QTE阶段：重置缩放状态，显示连续按键
        /// </summary>
        private void EnterContinuousPhase()
        {
            _isContinuousPhase = true;
            _currentContinuousIndex = 0;
            qteText.text = _continuousKey.ToString();
            _currentScale = 1f;
            qteText.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// UI动效：开始最终淡出阶段
        /// </summary>
        private void StartFinalFade()
        {
            _isFinalFade = true;
            _currentAlpha = 1f;
        }

        /// <summary>
        /// UI动效：完成后发送推特事件
        /// </summary>
        private void ProcessFinalFade()
        {
            _currentScale += Time.deltaTime * scaleSpeed * 2f;
            qteText.transform.localScale = Vector3.one * _currentScale;

            _currentAlpha -= Time.deltaTime * fadeSpeed;
            var color = qteText.color;
            color.a = _currentAlpha;
            qteText.color = color;

            if (_currentAlpha <= 0f)
            {
                CompleteQte();
            }
        }

     
        /// <summary>
        /// 启动QTE，可指定推特编号
        /// </summary>
        public void StartQte(int newTwitterId = -1)
        {
            if (newTwitterId >= 0)
            {
                twitterId = newTwitterId;
            }

            GenerateQteSequence();

            _currentDiscreteIndex = 0;
            _currentContinuousIndex = 0;
            _isContinuousPhase = false;
            _isFinalFade = false;
            _isScalingUp = false;
            _isScalingDown = false;
            _currentScale = 1f;
            _currentAlpha = 1f;
            isActive = true;

            qteText.enabled = true;
            qteText.text = _discreteSequence[0].ToString();
            qteText.fontSize = baseFontSize;
            qteText.transform.localScale = Vector3.one;
            var color = qteText.color;
            color.a = 1f;
            qteText.color = color;
        }

        /// <summary>
        /// 生成QTE序列：随机抽取X个离散按键和1个连续按键
        /// </summary>
        private void GenerateQteSequence()
        {
            _discreteSequence.Clear();

            for (var i = 0; i < discreteCount; i++)
            {
                var randomIndex = UnityEngine.Random.Range(0, _letterPool.Length);
                _discreteSequence.Add(_letterPool[randomIndex]);
            }

            var continuousIndex = UnityEngine.Random.Range(0, _letterPool.Length);
            _continuousKey = _letterPool[continuousIndex];
        }

        /// <summary>
        /// 停止QTE
        /// </summary>
        public void StopQte()
        {
            isActive = false;
            if (qteText != null)
            {
                qteText.enabled = false;
            }
        }

        /// <summary>
        /// 获取总QTE按键数量（X+Y）
        /// </summary>
        public int GetTotalQteCount()
        {
            return discreteCount + continuousCount;
        }
    }
}