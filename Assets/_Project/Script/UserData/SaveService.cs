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
            ES3.Save(SaveKeys.LevelModClassic, _userData.LevelModClassic.Value);
            ES3.Save(SaveKeys.CurrentScore, _userData.CurrentScore.Value);
            ES3.Save(SaveKeys.CompletedPictureIndices, _userData.CompletedPictureIndices);
            ES3.Save(SaveKeys.PictureFillProgress, _userData.PictureFillProgress);

            ES3.Save(SaveKeys.CountDrawInGame, _userData.CountDrawInGame.Value);
            ES3.Save(SaveKeys.CountDrawPicture, _userData.CountDrawPicture.Value);
            ES3.Save(SaveKeys.BoxSpawnIsLockState, _userData.BoxSpawnIsLockState);
            ES3.Save(SaveKeys.PictureIsSoldState, _userData.PictureIsSoldState);
        }

        public void DeleteAllSavedData()
        {
            ES3.DeleteKey(SaveKeys.HighScore);
            ES3.DeleteKey(SaveKeys.Gems);
            ES3.DeleteKey(SaveKeys.MagicBrush);
            ES3.DeleteKey(SaveKeys.Boom);
            ES3.DeleteKey(SaveKeys.LevelModClassic);
            ES3.DeleteKey(SaveKeys.CurrentScore);
            ES3.DeleteKey(SaveKeys.CountDrawInGame);
            ES3.DeleteKey(SaveKeys.CountDrawPicture);
            ES3.DeleteKey(SaveKeys.CompletedPictureIndices);
            ES3.DeleteKey(SaveKeys.PictureFillProgress);
            ES3.DeleteKey(SaveKeys.BoxSpawnIsLockState);
            ES3.DeleteKey(SaveKeys.PictureIsSoldState);
        }
    }
}