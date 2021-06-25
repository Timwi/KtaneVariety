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
                    .Where(cell => cell % W + 2 * Keypad.Widths[key] <= W && cell / W + 2 * Keypad.Heights[key] <= H)
                    .Where(topleft => Enumerable.Range(0, 4 * Keypad.Widths[key] * Keypad.Heights[key])
                        .All(subcell => !taken.Contains(subcell % (2 * Keypad.Widths[key]) + topleft % W + W * (subcell / (2 * Keypad.Widths[key]) + topleft / W))))
                    .Select(topleft => new { TopLeft = topleft, Size = key }))
                .ToArray();
            if (availableSlots.Length == 0)
                return null;

            var slot = availableSlots[Rnd.Range(0, availableSlots.Length)];
            for (var x = 0; x < 2 * Keypad.Widths[slot.Size]; x++)
                for (var y = 0; y < 2 * Keypad.Heights[slot.Size]; y++)
                    taken.Add(slot.TopLeft + x + W * y);
            taken.Add(slot.Size);

            return new Keypad(module, slot.Size, slot.TopLeft);
        }

        public override IEnumerable<object> Flavors { get { return Enum.GetValues(typeof(KeypadSize)).Cast<object>(); } }
    }
}
