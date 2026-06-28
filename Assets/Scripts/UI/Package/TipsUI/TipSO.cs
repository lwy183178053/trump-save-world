using System;
using UnityEngine;

namespace CWGame
{
    [CreateAssetMenu(fileName = "NewTip", menuName = "Dialogue/TipSO")]
    public class TipSO : ScriptableObject
    {
        public TipNode[] nodes;
    }
}
