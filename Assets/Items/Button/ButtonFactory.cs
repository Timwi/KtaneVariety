using System;
using System.Collections.Generic;
using System.Linq;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class ButtonFactory : ItemFactory
    {
        private readonly int[] _buttonValues;

        public ButtonFactory(MonoRandom rnd)
        {
            _buttonValues = rnd.ShuffleFisherYates(Enumerable.Range(0, Enum.GetValues(typeof(ButtonColor)).Length).ToArray());
        }

        public override Item Generate(VarietyModule module, HashSet<object> taken, System.Random rnd)
        {
            var availableColors = ((ButtonColor[]) Enum.GetValues(typeof(ButtonColor))).Where(col => !taken.Contains(col)).ToArray();
            if (availableColors.Length == 0)
                return null;

            var availableSpots = Enumerable.Range(0, W * H).Where(topleft => isRectAvailable(taken, topleft, 3, 3)).ToArray();
            if (availableSpots.Length == 0)
                return null;

            var topLeftCell = availableSpots[rnd.Next(0, availableSpots.Length)];
            var color = availableColors[rnd.Next(0, availableColors.Length)];
            claimRect(taken, topLeftCell, 3, 3);
            taken.Add(color);

            return new Button(module, topLeftCell, color, _buttonValues[(int) color], rnd.Next(3, 7));
        }

        public override IEnumerable<object> Flavors => Enum.GetValues(typeof(ButtonColor)).Cast<object>();
    }
}