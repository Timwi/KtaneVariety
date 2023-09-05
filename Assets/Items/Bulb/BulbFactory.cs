using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Variety
{
    public class BulbFactory : ItemFactory
    {
        public BulbFactory(MonoRandom ruleSeedRnd)
        {
            _aFirst = ruleSeedRnd.Next(0, 2) != 0;
        }

        private readonly bool _aFirst;

        public override Item Generate(VarietyModule module, HashSet<object> taken, System.Random rnd)
        {
            var availableColors = ((BulbColor[]) Enum.GetValues(typeof(BulbColor))).Where(col => !taken.Contains(col)).ToArray();
            if (availableColors.Length == 0)
                return null;

            var availableSpots = Enumerable.Range(0, W * H).Where(spot => isRectAvailable(taken, spot, 2, 2)).ToArray();
            if (availableSpots.Length == 0)
                return null;

            var topLeftCell = availableSpots[rnd.Next(0, availableSpots.Length)];
            var color = availableColors[rnd.Next(0, availableColors.Length)];
            claimRect(taken, topLeftCell, 2, 2);
            taken.Add(color);

            return new Bulb(module, topLeftCell, color, (color == BulbColor.YellowBulb ^ _aFirst) ? 'A' : 'N',
                // Determine N between 5 and 13, with higher numbers slightly less likely
                n: Mathf.CeilToInt(Mathf.Pow(2, 5f - (float) rnd.NextDouble() * 1.5f) * 13f / 32f));
        }

        public override IEnumerable<object> Flavors => Enum.GetValues(typeof(BulbColor)).Cast<object>();
    }
}
