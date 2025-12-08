/*namespace Sand
{
    public class CountDrawData
    {
        UserData _userData;
        SaveService _saveService;

        public CountDrawData(UserData userData, SaveService saveService)
        {
            _userData = userData;
            _saveService = saveService;
        }

        public int GetCountDraw()
        {
            return _userData.CountDraw.Value;
        }

        public void DecreaseCountDraw()
        {
            if (_userData.CountDraw.Value > 0)
            {
                _userData.CountDraw.Value--;
                _saveService.Save();
            }
        }

        public bool CanDraw()
        {
            return _userData.CountDraw.Value > 0;
        }

        public void ResetCountDraw()
        {
            _userData.CountDraw.Value = 0;
        }
    }
}*/