using System;

namespace GeoPuzzle.Core
{
    /// <summary>
    /// A rewarded video. The real ad network plugs in behind this interface later; until then
    /// <see cref="TestRewardedAds"/> stands in for it.
    /// </summary>
    public interface IRewardedAds
    {
        /// <summary>True when an ad can be shown now (enabled in the config and loaded).</summary>
        bool IsReady { get; }

        /// <summary>Shows an ad and reports whether the player watched it to the end and earned the reward.</summary>
        void Show(Action<bool> onFinished);
    }
}
