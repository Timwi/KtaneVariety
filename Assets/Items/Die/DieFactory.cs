using System;
using System.Collections.Generic;
using System.Linq;

namespace Variety
{
    class DieFactory : ItemFactory
    {
        public override IEnumerable<object> Flavors
        {
            get
            {
                yield return DieFlavor.LightOnDark;
                yield return DieFlavor.DarkOnLight;
            }
        }

        public override Item Generate(VarietyModule module, HashSet<object> taken, Random rnd)
        {
            if (taken.Contains(this))
                return null;

            var locations = Enumerable.Range(0, W * H).Where(tlc => isRectAvailable(taken, tlc, 2, 2)).ToArray();
            if (locations.Length == 0)
                return null;

            var location = locations[rnd.Next(0, locations.Length)];
            claimRect(taken, location, 2, 2);
            taken.Add(this);

            return new Die(module, location, rnd.Next(2) != 0);
        }
    }
}
