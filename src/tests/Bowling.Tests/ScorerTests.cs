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

            var result = scorer.CalculateScore();

            Assert(result).IsEqualTo(0);
        }
    }
}