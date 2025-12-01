using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HadesSDK.Ads.Core
{
    public abstract class AdService
    {
        public Action OnAdServiceInitializeFinished { get; set; }
        
        public Action OnInterAdDisplay { get; set; }
        public Action OnInterAdDisplayFail { get; set; }
        public Action OnInterAdClose { get; set; }

        public Action OnRewardAdDisplay { get; set; }
        public Action OnRewardAdDisplayFail { get; set; }
        public Action OnRewardAdClose { get; set; }
        public Action OnRewardReceive { get; set; }
        
        public Action OnBannerClicked { get; set; }

        public Action<AdValue> OnAdPaid { get; set; }
        public Action<AdValueNonDetermine> OnAdNonDeterminePaid { get; set; }
        

        protected bool _isScheduleReloadReward;
        protected bool _isScheduleReloadInter;

        protected int _rewardRetryAttempt = 0;
        protected int _interRetryAttempt = 0;
        
        public bool IsInit { get; protected set; }
        
        public abstract void Init();

        public abstract void LoadInterstitial();
        public abstract bool IsInterstitialReady();
        public abstract void ShowInterstitial();

        public async UniTaskVoid ScheduleReloadInterstitial()
        {
            if(_isScheduleReloadInter) return;
            _isScheduleReloadInter = true;

            _interRetryAttempt = Mathf.Min(_interRetryAttempt + 1, 5);
            float duration = Mathf.Pow(2f, _interRetryAttempt);

            await UniTask.WaitForSeconds(duration);
            _isScheduleReloadInter = false;
            
            if(IsInterstitialReady()) return;
            LoadInterstitial();
        }

        public abstract void LoadReward();
        public abstract bool IsRewardReady();
        public abstract void ShowReward();
        
        public async UniTaskVoid ScheduleReloadReward()
        {
            if(_isScheduleReloadReward) return;
            _isScheduleReloadReward = true;

            _rewardRetryAttempt = Mathf.Min(_rewardRetryAttempt + 1, 5);
            float duration = Mathf.Pow(2f, _rewardRetryAttempt);

            await UniTask.WaitForSeconds(duration);
            _isScheduleReloadReward = false;
            
            if(IsRewardReady()) return;
            LoadReward();
        }

        public abstract void LoadBanner();
        public abstract void ShowBanner();
        public abstract void HideBanner();

        public abstract void DestroyBanner();

        public abstract void LoadMrec();
        public abstract void ShowMrec();
        public abstract void HideMrec();
        public abstract bool IsMrecReady();
        public abstract void SetMrecPosition(Vector2 dpPos);

        public virtual void Dispose()
        {
            
        }
        
        public struct AdValue
        {
            public double value;
            public AdType adType;
            public MediationNetwork adPlatform;
            public string adNetwork;
            public string adIdentifier;
            public string adCurrency;
            public string placement;
        }
        
        public struct AdValueNonDetermine
        {
            public double value;
            public string adType;
            public MediationNetwork adPlatform;
            public string adNetwork;
            public string adIdentifier;
            public string adCurrency;
        }
        
        public enum AdType
        {
            reward = 0,
            interstitial = 1,
            banner = 2,
            banner_collapsible = 3,
            mrec = 4,
            aoa = 5,
        }
    }
}