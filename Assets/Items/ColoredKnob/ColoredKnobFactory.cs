using System;
using System.Collections.Generic;
using System.Linq;

namespace Variety
{
    public class ColoredKnobFactory : ItemFactory
    {
        private readonly int[] _baseRotations;

        public ColoredKnobFactory(MonoRandom ruleSeedRnd)
        {
            _baseRotations = ruleSeedRnd.ShuffleFisherYates(Enumerable.Range(0, 4).ToArray());
        }

        public override Item Generate(VarietyModule module, HashSet<object> taken, Random rnd)
        {
            var available = Enum.GetValues(typeof(ColoredKnobColor)).Cast<ColoredKnobColor>().Where(c => !taken.Contains(c)).ToArray();
            if (available.Length == 0)
                return null;

            var availableSpots = Enumerable.Range(0, W * H).Where(topleft => isRectAvailable(taken, topleft, 2, 2)).ToArray();
            if (availableSpots.Length == 0)
                return null;

            var spot = availableSpots[rnd.Next(0, availableSpots.Length)];
            claimRect(taken, spot, 2, 2);
            var color = available[rnd.Next(0, available.Length)];
            taken.Add(color);
            int n = rnd.Next(3, 7);

            return new ColoredKnob(module, spot, color, _baseRotations[(int) color], n, rnd);
        }

        public override IEnumerable<object> Flavors => Enum.GetValues(typeof(ColoredKnobColor)).Cast<object>();
    }
}