using Core;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class SaveMapData
    {
        private const string KEY_MAP_WIDTH  = "Map_Width";
        private const string KEY_MAP_HEIGHT = "Map_Height";
        private const string KEY_MAP_CELLS  = "Map_Cells";
        
        readonly Map _map;
        readonly RenderMap _renderMap;

        Map Map => _renderMap._map;
        Color32 Background => _renderMap._backgroundColor;
        
        [Inject]
        public SaveMapData(RenderMap renderMap)
        {
            _renderMap = renderMap;
        }
        
        [System.Serializable]
        public struct CellSaveData
        {
            public byte hasValue;
            public Color32 color;
        }

        public void SaveDataMap()
        {
            int width  = Map.Width;
            int height = Map.Height;
            int length = width * height;

            var cellsData = new CellSaveData[length];

            int i = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = Map.GetCell(x, y);
                    cellsData[i].hasValue = cell.hasValue;
                    cellsData[i].color    = cell.color;
                    i++;
                }
            }

            ES3.Save(KEY_MAP_WIDTH,  width);
            ES3.Save(KEY_MAP_HEIGHT, height);
            ES3.Save(KEY_MAP_CELLS,  cellsData);
            Debug.Log("Save map data");
        }

        public void LoadDataMap()
        {
            if (!ES3.KeyExists(KEY_MAP_CELLS)) return;
            int savedWidth  = ES3.Load<int>(KEY_MAP_WIDTH);
            int savedHeight = ES3.Load<int>(KEY_MAP_HEIGHT);

            if (savedWidth != Map.Width || savedHeight != Map.Height) return;

            var cellsData = ES3.Load<CellSaveData[]>(KEY_MAP_CELLS);
            bool hasData = false;
            for (int j = 0; j < cellsData.Length; j++)
            {
                if (cellsData[j].hasValue == 1) 
                {
                    hasData = true;
                    break;
                }
            }

            if (!hasData)
            {
                Global.Send(new SignalDisableButtonContinueMenu());
            }
            else
            {
                Global.Send(new SignalEnableButtonContinueMenu());
                Global.Send(new SignalChangTextBtnSwitchPlay(){IsChange = true});
            }
            
            Map.SetUpMap(Background);
            int i = 0;
            for (int y = 0; y < savedHeight; y++)
            {
                for (int x = 0; x < savedWidth; x++)
                {
                    var data = cellsData[i];
                    if (data.hasValue == 1) Map.SetPixelCell(x, y, data.color);
                    i++;
                }
            }
            
            Map.UpdateTexture();
            Debug.Log("Load map data");
        }
    }
}
