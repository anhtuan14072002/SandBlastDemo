using UnityEngine;

namespace Sand
{
    [CreateAssetMenu(fileName = "Score", menuName = "ScriptableObjects/Score", order = 1)]
    public class ScriptableScore : ScriptableObject
    {
        public int _score;
    }
}