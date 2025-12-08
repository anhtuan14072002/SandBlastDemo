namespace Sand
{
    public class CountDrawData
    {
        UserData _userData;

        public CountDrawData(UserData userData)
        {
            _userData = userData;
        }

        public void IncreaseCountDrawInGame(int count)
        {
            _userData.CountDrawInGame.Value +=  count;
        }

        public void DecreaseCountDrawInGame(int count)
        {
            _userData.CountDrawInGame.Value -= count;
        }

        public void IncreaseCountDrawPicture(int count)
        {
            _userData.CountDrawPicture.Value += count;
        }

        public void DecreaseCountDrawPicture(int count)
        {
            _userData.CountDrawPicture.Value -= count;
        }

        public void CountDrawPicture()
        {
            _userData.CountDrawPicture.Value += _userData.CountDrawInGame.Value;
        }
    }
}