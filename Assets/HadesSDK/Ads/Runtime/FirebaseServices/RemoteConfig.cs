namespace HadesSDK.Ads.Runtime.FirebaseServices
{
    public class RemoteConfig
    {
        public float inter_ad_capping_time = 20f;
        public float open_ad_capping_time = 30f;
        public float level_show_rate = 3;
        
        public bool open_ad_on = true;
        public bool first_open_ad_on = false;
        public bool inter_ad_on = true;
        public bool mrec_ad_on = true;
        public bool banner_ad_on = true;
        public bool banner_collap_on = true;
        public bool offline_play_on = false;
    }
}