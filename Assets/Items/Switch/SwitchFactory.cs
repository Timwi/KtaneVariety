using System;
using System.Collections.Generic;
using System.Linq;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class SwitchFactory : ItemFactory
    {
        public override Item Generate(VarietyModule module, HashSet<object> taken, System.Random rnd)
        {
            var availableCells = Enumerable.Range(0, W * H).Where(c => isRectAvailable(taken, c, 1, 4)).ToArray();
            if (availableCells.Length == 0)
                return null;

            var availableColors = ((SwitchColor[]) Enum.GetValues(typeof(SwitchColor))).Where(c => !taken.Contains(c)).ToArray();
            if (availableColors.Length == 0)
                return null;

            var cell = availableCells[rnd.Next(0, availableCells.Length)];
            claimRect(taken, cell, 1, 4);

            var color = availableColors[rnd.Next(0, availableColors.Length)];
            taken.Add(color);

            return new Switch(module, cell, color, rnd.Next(2, 5));
        }

        public override IEnumerable<object> Flavors => Enum.GetValues(typeof(SwitchColor)).Cast<object>();
    }
}