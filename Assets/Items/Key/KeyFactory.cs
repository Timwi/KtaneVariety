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

            var availableCells = Enumerable.Range(0, W * H).Where(i => isRectAvailable(taken, i, 2, 2)).ToArray();
            if (availableCells.Length == 0)
                return null;

            var cell = availableCells[Rnd.Range(0, availableCells.Length)];
            claimRect(taken, cell, 2, 2);
            taken.Add(this);

            return new Key(module, cell);
        }

        public override IEnumerable<object> Flavors { get { yield return "Key"; } }
    }
}