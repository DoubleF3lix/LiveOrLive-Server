﻿using liveorlive_server.Enums;

namespace liveorlive_server.Deck
{
    public class AmmoDeck(Settings settings) : Deck<BulletType>(settings)
    {
        public int BlankCount { get; private set; } = 0;
        public int LiveCount { get; private set; } = 0;

        public override void Refresh() {
            deck.Clear();

            Random random = new();
            BlankCount = random.Next(1, settings.MaxBlankRounds);
            LiveCount = random.Next(1, settings.MaxLiveRounds);

            for (int i = 0; i < BlankCount; i++)
            {
                deck.Add(BulletType.Blank);
            }
            for (int i = 0; i < LiveCount; i++)
            {
                deck.Add(BulletType.Live);
            }

            Shuffle();
        }

        // Used by check bullet item
        public BulletType Peek() {
            return deck[^1]; // From the end
        }
    }
}
