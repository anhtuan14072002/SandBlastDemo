using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class PictureDrawData
    {
        private readonly UserData _userData;
        private readonly SaveService _saveService;

        [Inject]
        public PictureDrawData(UserData userData, SaveService saveService)
        {
            _userData = userData;
            _saveService = saveService;

            if (_userData.PictureFillProgress == null)
                _userData.PictureFillProgress = new Dictionary<int, int>();
            if (_userData.CompletedPictureIndices == null)
                _userData.CompletedPictureIndices = new List<int>();
        }
        
        public int GetFilledRegionCount(int pictureIndex, int totalRegions)
        {
            if (pictureIndex < 0 || totalRegions <= 0)
                return 0;

            if (_userData.CompletedPictureIndices.Contains(pictureIndex))
            {
                return totalRegions;
            }

            if (_userData.PictureFillProgress != null &&
                _userData.PictureFillProgress.TryGetValue(pictureIndex, out var count))
            {
                return Mathf.Clamp(count, 0, totalRegions);
            }

            return 0;
        }
        
        public void UpdateFillProgress(int pictureIndex, int currentRegionCount, int totalRegions)
        {
            if (pictureIndex < 0 || totalRegions <= 0)
                return;

            if (_userData.PictureFillProgress == null)
                _userData.PictureFillProgress = new Dictionary<int, int>();

            int clamped = Mathf.Clamp(currentRegionCount, 0, totalRegions);

            if (clamped >= totalRegions)
            {
                UpdatePictureCollections(pictureIndex);
                if (_userData.PictureFillProgress.ContainsKey(pictureIndex))
                    _userData.PictureFillProgress.Remove(pictureIndex);
            }
            else
            {
                _userData.PictureFillProgress[pictureIndex] = clamped;
            }
        }
        
        public void UpdatePictureCollections(int pictureIndex)
        {
            if (pictureIndex < 0) return;

            if (!_userData.CompletedPictureIndices.Contains(pictureIndex))
            {
                _userData.CompletedPictureIndices.Add(pictureIndex); 
            }
        }
        public void ResetPictureCollections()
        {
            _userData.CompletedPictureIndices.Clear();
            _userData.PictureFillProgress?.Clear();
            _userData.SoldPictureIndices?.Clear();
        }
    }
}
