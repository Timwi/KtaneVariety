using System.Collections.Generic;
using System.Linq;
using KModkit;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class KnobFactory : ItemFactory
    {
        public override Item Generate(VarietyModule module, HashSet<object> taken, System.Random rnd)
        {
            if (taken.Contains(this))
                return null;

            var availableSpots = Enumerable.Range(0, W * H).Where(topleft => isRectAvailable(taken, topleft, 3, 3)).ToArray();
            if (availableSpots.Length == 0)
                return null;

            var spot = availableSpots[rnd.Next(0, availableSpots.Length)];
            claimRect(taken, spot, 3, 3);
            taken.Add(this);

            return new Knob(module, spot, rnd.Next(5, 11), rnd);
        }

        public override IEnumerable<object> Flavors { get { yield return "Knob"; } }
    }
}