using UnityEngine;

namespace Sand
{
    public static class ColoringSave
    {
        private const string PROGRESS_KEY = "coloring_progress_db";
        private const string SAVE_FILE = "coloringbook.es3";
        public static void SaveProgress(ColoringProgressDatabase progressDb)
        {
            if (progressDb == null) return;
            ES3.Save(PROGRESS_KEY, progressDb, SAVE_FILE);
            Debug.Log("đã lưu tiến độ oy nhe");
        }
        
        public static ColoringProgressDatabase LoadProgress()
        {
            if (ES3.KeyExists(PROGRESS_KEY, SAVE_FILE))
            {
                return ES3.Load<ColoringProgressDatabase>(PROGRESS_KEY, SAVE_FILE);
            }
            return new ColoringProgressDatabase();
        }

        /// <summary>
        /// Xóa tiến độ ảnh hiện tại
        /// </summary>
        public static void ClearCurrentPictureProgress(ColoringProgressDatabase progressDb, int pictureIndex)
        {
            if (progressDb == null) return;
            
            var progress = progressDb.GetProgress(pictureIndex);
            if (progress != null)
            {
                progressDb.progressList.Remove(progress);
                SaveProgress(progressDb);
                Debug.Log($"Đã xóa dữ liệu ảnh òy: {pictureIndex}");
            }
        }
        /// <summary>
        /// Xóa toàn bộ tiến độ
        /// </summary>
        public static void ClearAllProgress(ColoringProgressDatabase progressDb)
        {
            if (progressDb == null) return;
            
            progressDb.progressList.Clear();
            SaveProgress(progressDb);
            Debug.Log("Đa xóa hết màu của ảnh");
        }
        public static bool HasSaveData() => ES3.KeyExists(PROGRESS_KEY, SAVE_FILE);
        public static void DeleteSaveFile()
        {
            if (ES3.FileExists(SAVE_FILE))
            {
                ES3.DeleteFile(SAVE_FILE);
                Debug.Log("Đã xóa file save");
            };
        }
    }
}