using Testify;
using Xunit;
using static Testify.Assertions;

namespace Bowling.Tests
{
    public class ScorerTests
    {
        [Fact]
        public void CalculateScore_AllBallsAreGutterBalls_0()
        {
            var scorer = new Scorer();
            RollMany(scorer, 0, 20);

            var result = scorer.CalculateScore();

            Assert(result).IsEqualTo(0);
        }

        [Fact]
        public void CalculateScore_AllBallsKnockDownOnePin_20()
        {
            var scorer = new Scorer();
            RollMany(scorer, 1, 20);

            var result = scorer.CalculateScore();

            Assert(result).IsEqualTo(20);
        }

        [Fact]
        public void CalculateScore_FirstFrameIsStrikeRestOnes_30()
        {
            var scorer = new Scorer();
            scorer.Roll(10);
            RollMany(scorer, 1, 18);

            var result = scorer.CalculateScore();

            Assert(result).IsEqualTo(30);
        }

        [Fact]
        public void CalculateScore_AllStrikes_300()
        {
            var scorer = new Scorer();
            RollMany(scorer, 10, 12);

            var result = scorer.CalculateScore();

            Assert(result).IsEqualTo(300);
        }

        [Fact]
        public void CalculateScore_FirstFrameIsSpareRestOnes_29()
        {
            var scorer = new Scorer();
            RollMany(scorer, 5, 2);
            RollMany(scorer, 1, 18);

            var result = scorer.CalculateScore();

            Assert(result).IsEqualTo(29);
        }

        [Fact]
        public void CalculateScore_AllSpares_150()
        {
            var scorer = new Scorer();
            RollMany(scorer, 5, 21);

            var result = scorer.CalculateScore();

            Assert(result).IsEqualTo(150);
        }

        private void RollMany(Scorer scorer, int pins, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                scorer.Roll(pins);
            }
        }
    }
}