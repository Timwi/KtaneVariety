using System;
using System.Collections.Generic;
using System.Linq;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class LedFactory : ItemFactory
    {
        // Lower-valued LedColor first
        private Dictionary<LedColor, Dictionary<LedColor, LedColor[]>> _answers;

        public LedFactory(MonoRandom rnd)
        {
            _answers = new Dictionary<LedColor, Dictionary<LedColor, LedColor[]>>();
            var allAnswers = new List<LedColor[]>();
            var colors = (LedColor[]) Enum.GetValues(typeof(LedColor));
            for (var c1ix = 0; c1ix < colors.Length - 1; c1ix++)
            {
                _answers[colors[c1ix]] = new Dictionary<LedColor, LedColor[]>();
                for (var c2ix = c1ix + 1; c2ix < colors.Length; c2ix++)
                {
                    LedColor[] answers;
                    do
                        answers = rnd.ShuffleFisherYates(colors.ToArray()).Take(rnd.Next(2, 6)).ToArray();
                    while (allAnswers.Any(aa => aa.SequenceEqual(answers)));
                    allAnswers.Add(answers);
                    _answers[colors[c1ix]][colors[c2ix]] = answers;
                }
            }
        }

        public override Item Generate(VarietyModule module, HashSet<object> taken, System.Random rnd)
        {
            if (taken.Contains(this))
                return null;

            var availableSpots = Enumerable.Range(0, W * H).Where(spot => isRectAvailable(taken, spot, 2, 2)).ToArray();
            if (availableSpots.Length == 0)
                return null;

            var topLeftCell = availableSpots[rnd.Next(0, availableSpots.Length)];
            var colors = (LedColor[]) Enum.GetValues(typeof(LedColor));
            var color1Ix = rnd.Next(0, colors.Length);
            var color2Ix = rnd.Next(0, colors.Length - 1);
            if (color2Ix >= color1Ix)
                color2Ix++;
            var color1 = colors[Math.Min(color1Ix, color2Ix)];
            var color2 = colors[Math.Max(color1Ix, color2Ix)];
            claimRect(taken, topLeftCell, 2, 2);
            taken.Add(this);
            return new Led(module, topLeftCell, color1, color2, _answers[color1][color2]);
        }

        public override IEnumerable<object> Flavors { get { yield return "LED"; } }
    }
}
