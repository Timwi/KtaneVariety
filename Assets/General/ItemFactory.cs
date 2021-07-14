using System;
using System.Collections.Generic;
using System.Linq;

namespace Variety
{
    public abstract class ItemFactory
    {
        public abstract Item Generate(VarietyModule module, HashSet<object> taken);

        protected static int W { get { return VarietyModule.W; } }
        protected static int H { get { return VarietyModule.H; } }

        public abstract IEnumerable<object> Flavors { get; }

        protected static bool isRectAvailable(HashSet<object> taken, int cell, int width, int height)
        {
            var x = cell % W;
            var y = cell / W;
            if (x < 0 || x + width > W || y < 0 || y + height > H)
                return false;
            return Enumerable.Range(0, width * height).All(subcell => !taken.Contains(x + subcell % width + W * (y + subcell / width)));
        }

        protected static void claimRect(HashSet<object> taken, int cell, int width, int height)
        {
            var x = cell % W;
            var y = cell / W;
            for (var dx = 0; dx < width; dx++)
                for (var dy = 0; dy < height; dy++)
                    taken.Add(x + dx + W * (y + dy));
        }
    }
}