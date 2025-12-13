using System.Collections.Generic;
using UnityEngine;

namespace HootyBird.ColoringBook.Serialized
{
    [CreateAssetMenu(fileName = "Category", menuName = "HootyBird/ColoringBook/Create Category Asset")]
    public class Category : ScriptableObject
    {
        [SerializeField]
        private List<ColoringBookDataBase> coloringBooks;

        public List<ColoringBookDataBase> ColoringBooks => coloringBooks;
    }
}
