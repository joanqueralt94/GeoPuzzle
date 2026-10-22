using System;
using GeoPuzzle.Core;
using GeoPuzzle.Quiz;
using UnityEngine;
using UnityEngine.UI;

namespace GeoPuzzle.UI
{
    /// <summary>
    /// The "keep playing?" screen shown when a level is lost. Works with any <see cref="IPlayOnLevel"/>:
    /// pay coins for the full boost, watch a rewarded video for a smaller one, or give up.
    /// </summary>
    public class PlayOnPanel
    {
        readonly GameObject root;
        readonly Text title;
        readonly Text balance;
        readonly Text status;
        readonly Button coinButton;
        readonly Button adButton;
        readonly Button giveUpButton;
        readonly Localizer texts;

        PlayOnSession session;
        IPlayOnLevel level;
        Action onContinued;
        Action onGaveUp;
        bool waitingForAd;

        public bool IsOpen => root.activeSelf;

        public PlayOnPanel(Transform canvas, Localizer texts)
        {
            this.texts = texts;
            var panel = UiKit.Panel(canvas, "PlayOn", new Color(0f, 0f, 0f, 0.85f));
            root = panel.gameObject;

            title = UiKit.Label(panel.transform, "Title", 96, TextAnchor.MiddleCenter, UiKit.TextLight);
            UiKit.Place(title.rectTransform, 0.1f, 0.72f, 0.9f, 0.90f);
            var ask = UiKit.Label(panel.transform, "Ask", 52, TextAnchor.MiddleCenter, UiKit.TextLight);
            UiKit.Place(ask.rectTransform, 0.1f, 0.62f, 0.9f, 0.72f);
            ask.text = texts.Get("ui.playon.ask");
            balance = UiKit.Label(panel.transform, "Balance", 44, TextAnchor.MiddleCenter, UiKit.Coin);
            UiKit.Place(balance.rectTransform, 0.1f, 0.54f, 0.9f, 0.62f);

            coinButton = UiKit.Button(panel.transform, "PayCoins", "", 48, PayWithCoins);
            UiKit.Place((RectTransform)coinButton.transform, 0.10f, 0.32f, 0.48f, 0.48f);
            adButton = UiKit.Button(panel.transform, "WatchAd", "", 48, WatchAd);
            UiKit.Place((RectTransform)adButton.transform, 0.52f, 0.32f, 0.90f, 0.48f);

            status = UiKit.Label(panel.transform, "Status", 40, TextAnchor.MiddleCenter, UiKit.TextLight);
            UiKit.Place(status.rectTransform, 0.1f, 0.24f, 0.9f, 0.31f);

            giveUpButton = UiKit.Button(panel.transform, "GiveUp", texts.Get("ui.playon.give_up"), 44, GiveUp);
            UiKit.Place((RectTransform)giveUpButton.transform, 0.38f, 0.07f, 0.62f, 0.19f);

            root.SetActive(false);
        }

        public void Show(PlayOnSession playOnSession, IPlayOnLevel lostLevel, Action continued, Action gaveUp)
        {
            session = playOnSession;
            level = lostLevel;
            onContinued = continued;
            onGaveUp = gaveUp;
            waitingForAd = false;
            status.text = "";
            root.SetActive(true);
            Refresh();
        }

        void Refresh()
        {
            var offer = session.OfferFor(level);
            var hearts = offer.Reason == LoseReason.OutOfHearts;
            title.text = texts.Get(hearts ? "ui.quiz.lost_hearts" : "ui.quiz.lost_time");
            balance.text = texts.Format("ui.playon.balance", GameServices.Wallet.Coins);

            UiKit.ButtonLabel(coinButton).text = hearts
                ? texts.Format("ui.playon.coins_hearts", offer.CoinHearts, offer.CoinPrice)
                : texts.Format("ui.playon.coins_time", offer.CoinSeconds, offer.CoinPrice);
            coinButton.interactable = !waitingForAd && GameServices.Wallet.CanAfford(offer.CoinPrice);

            UiKit.ButtonLabel(adButton).text = hearts
                ? texts.Format("ui.playon.ad_hearts", offer.RewardedHearts)
                : texts.Format("ui.playon.ad_time", offer.RewardedSeconds);
            adButton.gameObject.SetActive(GameServices.RewardedAds.IsReady || waitingForAd);
            adButton.interactable = !waitingForAd;
            giveUpButton.interactable = !waitingForAd;
        }

        void PayWithCoins()
        {
            var offer = session.OfferFor(level);
            if (!GameServices.Wallet.TrySpend(offer.CoinPrice))
            {
                Refresh();
                return;
            }
            session.ApplyCoins(level);
            Close(onContinued);
        }

        void WatchAd()
        {
            waitingForAd = true;
            status.text = texts.Get("ui.playon.ad_playing");
            Refresh();
            GameServices.RewardedAds.Show(watched =>
            {
                waitingForAd = false;
                if (!watched)
                {
                    status.text = texts.Get("ui.playon.ad_failed");
                    Refresh();
                    return;
                }
                session.ApplyRewarded(level);
                Close(onContinued);
            });
        }

        void GiveUp() => Close(onGaveUp);

        void Close(Action then)
        {
            root.SetActive(false);
            then?.Invoke();
        }
    }
}
