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

            var availableCells = Enumerable.Range(0, W * H).Where(i => isRectAvailable(taken, i % W, i / W, 2, 2)).ToArray();
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

        public override IEnumerable<object> Flavors { get { yield return "Key"; } }
    }
}