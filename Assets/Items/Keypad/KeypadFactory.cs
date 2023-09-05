using System;
using System.Collections.Generic;
using System.Linq;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class KeypadFactory : ItemFactory
    {
        public override Item Generate(VarietyModule module, HashSet<object> taken, System.Random rnd)
        {
            var availableSlots = Keypad.Widths.Keys
                .Where(size => !taken.Contains(size))
                .SelectMany(size => Enumerable.Range(0, W * H)
                    .Where(topleft => isRectAvailable(taken, topleft, 2 * Keypad.Widths[size], 2 * Keypad.Heights[size]))
                    .Select(topleft => new { TopLeft = topleft, Size = size }))
                .ToArray();
            if (availableSlots.Length == 0)
                return null;

            var slot = availableSlots[rnd.Next(0, availableSlots.Length)];
            claimRect(taken, slot.TopLeft, 2 * Keypad.Widths[slot.Size], 2 * Keypad.Heights[slot.Size]);
            taken.Add(slot.Size);

            return new Keypad(module, slot.Size, slot.TopLeft);
        }

        public override IEnumerable<object> Flavors => Enum.GetValues(typeof(KeypadSize)).Cast<object>();
    }
}
