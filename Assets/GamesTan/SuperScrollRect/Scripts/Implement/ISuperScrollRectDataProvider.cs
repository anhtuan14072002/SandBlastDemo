using UnityEngine;

namespace GamesTan.UI
{
    public interface ISuperScrollRectDataProvider
    {
        int GetCellCount();
        void SetCell(GameObject cell, int index);
    }
}
