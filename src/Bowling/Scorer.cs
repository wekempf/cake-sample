using System;

namespace Bowling
{
    public class Scorer
    {
        private readonly int[] rolls = new int[21];
        private int rollIndex;
        private int logicalIndex;

        public int CalculateScore()
        {
            var score = 0;
            var currentRoll = 0;
            for (var i = 0; i < 10; ++i)
            {
                if (rolls[currentRoll] == 10)
                {
                    score += rolls[currentRoll] + rolls[currentRoll + 1] + rolls[currentRoll + 2];
                    currentRoll++;
                }
                else if (rolls[currentRoll] + rolls[currentRoll + 1] == 10)
                {
                    score += rolls[currentRoll] + rolls[currentRoll + 1] + rolls[currentRoll + 2];
                    currentRoll += 2;
                }
                else
                {
                    score += rolls[currentRoll] + rolls[currentRoll + 1];
                    currentRoll += 2;
                }
            }

            return score;
        }

        public void Roll(int pins)
        {
            if (logicalIndex >= 22)
            {
                throw new InvalidOperationException();
            }

            if (pins == 10 && this.logicalIndex < 19 && this.logicalIndex % 2 != 0)
            {
                throw new InvalidOperationException();
            }

            this.rolls[this.rollIndex++] = pins;
            this.logicalIndex += pins == 10 && this.logicalIndex < 19 ? 2 : 1;
        }
    }
}