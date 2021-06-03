using System.Collections.Generic;
using System.Linq;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class KeyFactory : ItemFactory
    {
        public override Item Generate(VarietyModule module, HashSet<object> taken)
        {
            if (taken.Contains(this))
                return null;

            var availableCells = Enumerable.Range(0, W * H).Where(i => i % W < W - 1 && i / W < H - 1 && !taken.Contains(i) && !taken.Contains(i + 1) && !taken.Contains(i + W) && !taken.Contains(i + W + 1)).ToArray();
            if (availableCells.Length == 0)
                return null;

            var cellIx = Rnd.Range(0, availableCells.Length);
            var cell = availableCells[cellIx];

            taken.Add(cell);
            taken.Add(cell + 1);
            taken.Add(cell + W);
            taken.Add(cell + W + 1);
            taken.Add(this);

            return new Key(module, cell);
        }
    }
}