using System;

namespace GeoPuzzle.Core
{
    /// <summary>The player's coins, saved after every change. A new player starts with wallet.start_coins.</summary>
    public class Wallet
    {
        public const string CoinsKey = "wallet.coins";

        readonly ISaveStore store;

        public int Coins { get; private set; }

        /// <summary>Raised with the new balance whenever coins are added or spent.</summary>
        public event Action<int> Changed;

        public Wallet(ISaveStore store, int startCoins)
        {
            this.store = store;
            if (store.HasKey(CoinsKey))
            {
                Coins = store.GetInt(CoinsKey, 0);
            }
            else
            {
                Coins = startCoins;
                Persist();
            }
        }

        public bool CanAfford(int price) => price >= 0 && Coins >= price;

        public bool TrySpend(int price)
        {
            if (!CanAfford(price))
                return false;
            Coins -= price;
            Persist();
            Changed?.Invoke(Coins);
            return true;
        }

        public void Add(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Use TrySpend to remove coins");
            Coins += amount;
            Persist();
            Changed?.Invoke(Coins);
        }

        void Persist()
        {
            store.SetInt(CoinsKey, Coins);
            store.Save();
        }
    }
}
