using System.Collections.Generic;
using System.Linq;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class DummyFactory : ItemFactory
    {
        public override Item Generate(VarietyModule module, HashSet<object> taken)
        {
            var availableCells = Enumerable.Range(0, W * H).Where(i => !taken.Contains(i)).ToArray();
            if (availableCells.Length == 0)
                return null;

            var cell = availableCells[Rnd.Range(0, availableCells.Length)];
            taken.Add(cell);
            return new Dummy(module, cell);
        }
    }
}