using System.Collections.Generic;
using System.Linq;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class MazeFactory : ItemFactory
    {
        private const int MinWidth = 3;
        private const int MaxWidth = 3;
        private const int MinHeight = 3;
        private const int MaxHeight = 3;

        public override Item Generate(HashSet<object> taken)
        {
            var availableConfigs = (
                from width in Enumerable.Range(MinWidth, MaxWidth - MinWidth + 1)
                from height in Enumerable.Range(MinHeight, MaxHeight - MinHeight + 1)
                where !taken.Contains(string.Format("{0}×{1} maze", width, height))
                from x in Enumerable.Range(0, W - width + 1)
                from y in Enumerable.Range(0, H - height + 1)
                where (
                    from dx in Enumerable.Range(0, W - width + 1)
                    from dy in Enumerable.Range(0, H - height + 1)
                    select taken.Contains(x + dx + W * (y + dy))).All(b => !b)
                select new { x, y, width, height }).ToArray();

            if (availableConfigs.Length == 0)
                return null;

            var configIx = Rnd.Range(0, availableConfigs.Length);
            var config = availableConfigs[configIx];

            for (var dx = 0; dx < config.width; dx++)
                for (var dy = 0; dy < config.height; dy++)
                    taken.Add(config.x + dx + W * (config.y + dy));
            taken.Add(string.Format("{0}×{1} maze", config.width, config.height));

            return new Maze(config.x, config.y, config.width, config.height);
        }
    }
}