using System;
using System.Collections;
using UnityEngine;

namespace GeoPuzzle.Core
{
    /// <summary>Stand-in for the ad network: waits a moment, then always grants the reward.</summary>
    public class TestRewardedAds : MonoBehaviour, IRewardedAds
    {
        [SerializeField] float fakeAdSeconds = 1.5f;

        public bool Enabled { get; set; } = true;

        public bool IsReady => Enabled;

        public static TestRewardedAds Create(bool enabled)
        {
            var go = new GameObject("TestRewardedAds");
            DontDestroyOnLoad(go);
            var ads = go.AddComponent<TestRewardedAds>();
            ads.Enabled = enabled;
            return ads;
        }

        public void Show(Action<bool> onFinished)
        {
            if (!IsReady)
            {
                onFinished(false);
                return;
            }
            StartCoroutine(Play(onFinished));
        }

        IEnumerator Play(Action<bool> onFinished)
        {
            Debug.Log("Test rewarded ad playing");
            yield return new WaitForSecondsRealtime(fakeAdSeconds);
            onFinished(true);
        }
    }
}
