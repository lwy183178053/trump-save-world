using System;
using UnityEngine;

namespace CWGame
{
    [Serializable]
    public class TipNode
    {
        [Header("角色")]
        public string CharacterName;
        public Sprite CharacterSprite;
        [Header("提示内容")]
        [TextArea(3, 10)]
        public string DialogueText;
        [Header("选项")]
        public TipChoice[] Branches;
        [Header("是否结束")]
        public bool IsEnd;
    }

    [Serializable]
    public class TipChoice
    {
        public string choiceText;
        public int nextNodeIndex;
    }
}
