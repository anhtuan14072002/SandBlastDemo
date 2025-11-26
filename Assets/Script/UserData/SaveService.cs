using Zenject;

namespace Sand
{
    public class SaveService
    {
        [Inject] UserData _userData;
        public void Save()
        {
            ES3.Save(SaveKeys.HighScore, _userData.HighScore.Value);
            ES3.Save(SaveKeys.Gems, _userData.Gems.Value);
            ES3.Save(SaveKeys.MagicBrush, _userData.MagicBrush.Value);
            ES3.Save(SaveKeys.Boom, _userData.Boom.Value);
        }
    }
}