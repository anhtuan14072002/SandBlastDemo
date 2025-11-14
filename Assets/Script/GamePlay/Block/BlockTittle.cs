using UnityEngine;

namespace Sand
{
    public class BlockTittle : MonoBehaviour
    {
        [SerializeField] private TypeBlock _typeBlock;
        [SerializeField] private int _idColor;
        public TypeBlock TypeBlock => _typeBlock;
        public int IdColor => _idColor;
    }
}