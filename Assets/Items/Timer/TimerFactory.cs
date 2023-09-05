using System;
using System.Collections.Generic;
using System.Linq;

namespace Variety
{
    class TimerFactory : ItemFactory
    {
        public override IEnumerable<object> Flavors => Enum.GetValues(typeof(TimerType)).Cast<object>();

        public override Item Generate(VarietyModule module, HashSet<object> taken, Random rnd)
        {
            var availableCells = Enumerable.Range(0, W * H).Where(c => isRectAvailable(taken, c, 2, 2)).ToArray();
            if (availableCells.Length == 0)
                return null;

            var availableFlavors = ((TimerType[]) Enum.GetValues(typeof(TimerType))).Where(c => !taken.Contains(c)).ToArray();
            if (availableFlavors.Length == 0)
                return null;

            var cell = availableCells[rnd.Next(0, availableCells.Length)];
            claimRect(taken, cell, 2, 2);

            var flavor = availableFlavors[rnd.Next(0, availableFlavors.Length)];
            taken.Add(flavor);

            var a = rnd.Next(0, 4);
            return new Timer(module, cell, flavor, a, rnd.Next(0, 3 - a));
        }
    }
}