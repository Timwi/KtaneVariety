using System.Collections.Generic;
using System.Linq;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class DigitDisplayFactory : ItemFactory
    {
        public override Item Generate(VarietyModule module, HashSet<object> taken)
        {
            if (taken.Contains(this))
                return null;

            var locations = Enumerable.Range(0, W * H).Where(tlc => isRectAvailable(taken, tlc, 2, 3)).ToArray();
            if (locations.Length == 0)
                return null;

            var location = locations[Rnd.Range(0, locations.Length)];
            claimRect(taken, location, 2, 3);
            taken.Add(this);

            return new DigitDisplay(module, location);
        }

        public override IEnumerable<object> Flavors { get { yield return "DigitDisplay"; } }
    }
}