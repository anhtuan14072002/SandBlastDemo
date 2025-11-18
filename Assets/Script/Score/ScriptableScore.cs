using UnityEngine;
using UnityEngine.Serialization;

namespace Sand
{
    [CreateAssetMenu(fileName = "Score", menuName = "ScriptableObjects/Score", order = 1)]
    public class ScriptableScore : ScriptableObject
    {
        [SerializeField] public int _highScore;

        public int HighScore
        {
            get => _highScore;
            set
            {
                if (value > _highScore)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
                    UnityEditor.AssetDatabase.SaveAssets();
#endif
                }
            }
        }
        public void RestScore()
        {
            _highScore = 0;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

    }
}