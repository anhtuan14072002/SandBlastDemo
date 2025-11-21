using UnityEngine;

namespace Sand
{
    public class BlockInfo : MonoBehaviour
    {
        [SerializeField] private BlockType blockType;
        [SerializeField] private int _idColor;
        public BlockType BlockType => blockType;
        public int IdColor => _idColor;
    }
}