using System;
using System.Collections.Generic;
using System.Linq;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class KeypadFactory : ItemFactory
    {
        public override Item Generate(VarietyModule module, HashSet<object> taken)
        {
            var availableSlots = Keypad.Widths.Keys
                .Where(key => !taken.Contains(key))
                .SelectMany(key => Enumerable.Range(0, W * H)
                    .Where(topleft => isRectAvailable(taken, topleft, 2 * Keypad.Widths[key], 2 * Keypad.Heights[key]))
                    .Select(topleft => new { TopLeft = topleft, Size = key }))
                .ToArray();
            if (availableSlots.Length == 0)
                return null;

            var slot = availableSlots[Rnd.Range(0, availableSlots.Length)];
            claimRect(taken, slot.TopLeft, 2 * Keypad.Widths[slot.Size], 2 * Keypad.Heights[slot.Size]);
            taken.Add(slot.Size);

            return new Keypad(module, slot.Size, slot.TopLeft);
        }

        public override IEnumerable<object> Flavors { get { return Enum.GetValues(typeof(KeypadSize)).Cast<object>(); } }
    }
}
