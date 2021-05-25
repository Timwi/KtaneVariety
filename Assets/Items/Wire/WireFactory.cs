using System;
using System.Collections.Generic;
using System.Linq;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class WireFactory : ItemFactory
    {
        public override Item Generate(HashSet<object> taken)
        {
            var availableCells = Enumerable.Range(0, W * H).Where(i => !taken.Contains(i)).ToArray();
            if (availableCells.Length < 2)
                return null;
            var availableColors = ((WireColor[]) Enum.GetValues(typeof(WireColor))).Where(c => !taken.Contains(c)).ToArray();
            if (availableColors.Length == 0)
                return null;

            var cell1Ix = Rnd.Range(0, availableCells.Length);
            var cell2Ix = Rnd.Range(0, availableCells.Length - 1);
            if (cell2Ix >= cell1Ix)
                cell2Ix++;

            var cell1 = availableCells[cell1Ix];
            var cell2 = availableCells[cell2Ix];
            var color = availableColors[Rnd.Range(0, availableColors.Length)];
            taken.Add(cell1);
            taken.Add(cell2);
            taken.Add(color);
            return new Wire(color, new[] { Math.Min(cell1, cell2), Math.Max(cell1, cell2) });
        }
    }
}