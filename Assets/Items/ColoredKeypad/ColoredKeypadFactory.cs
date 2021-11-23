using System;
using System.Collections.Generic;
using System.Linq;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class ColoredKeypadFactory : ItemFactory
    {
        private readonly Dictionary<ColoredKeypadColor, int> _numExpectedPresses = new Dictionary<ColoredKeypadColor, int>();

        public ColoredKeypadFactory(MonoRandom ruleSeedRnd)
        {
            var values = Enumerable.Range(2, Enum.GetValues(typeof(ColoredKeypadColor)).Length).ToArray();
            ruleSeedRnd.ShuffleFisherYates(values);
            for (var i = 0; i < values.Length; i++)
                _numExpectedPresses[(ColoredKeypadColor) i] = values[i];
        }

        public override Item Generate(VarietyModule module, HashSet<object> taken, System.Random rnd)
        {
            var availableConfigurations = (
                from color in (ColoredKeypadColor[]) Enum.GetValues(typeof(ColoredKeypadColor))
                where !taken.Contains(color)
                from size in (ColoredKeypadSize[]) Enum.GetValues(typeof(ColoredKeypadSize))
                where ColoredKeypad.Widths[size] * ColoredKeypad.Heights[size] > _numExpectedPresses[color]
                from topLeftCell in Enumerable.Range(0, W * H)
                where isRectAvailable(taken, topLeftCell, ColoredKeypad.CWidths[size], ColoredKeypad.CHeights[size])
                select new { Color = color, Size = size, TopLeftCell = topLeftCell }).ToArray();

            if (availableConfigurations.Length == 0)
                return null;

            var config = availableConfigurations[rnd.Next(0, availableConfigurations.Length)];
            taken.Add(config.Color);
            claimRect(taken, config.TopLeftCell, ColoredKeypad.CWidths[config.Size], ColoredKeypad.CHeights[config.Size]);

            return new ColoredKeypad(module, config.Color, config.Size, config.TopLeftCell, _numExpectedPresses[config.Color]);
        }

        public override IEnumerable<object> Flavors { get { return Enum.GetValues(typeof(ColoredKeypadColor)).Cast<object>(); } }
    }
}
