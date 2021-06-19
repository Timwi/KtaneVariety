using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

namespace Variety
{
    public class MazeFactory : ItemFactory
    {
        private const int MinWidth = 3;
        private const int MaxWidth = 3;
        private const int MinHeight = 3;
        private const int MaxHeight = 3;
        private const int NumWidths = MaxWidth - MinWidth + 1;
        private const int NumHeights = MaxHeight - MinHeight + 1;
        private const int NumShapes = 3 * 3;

        private readonly MazeLayout[] _mazes = new MazeLayout[NumWidths * NumHeights * NumShapes];

        public MazeFactory(MonoRandom rnd)
        {
            for (var i = 0; i < _mazes.Length; i++)
                _mazes[i] = MazeLayout.Generate(i / NumShapes / NumHeights + MinWidth, (i / NumShapes) % NumHeights + MinHeight, rnd);
        }

        public override Item Generate(VarietyModule module, HashSet<object> taken)
        {
            var availableConfigs = (
                from width in Enumerable.Range(MinWidth, MaxWidth - MinWidth + 1)
                from height in Enumerable.Range(MinHeight, MaxHeight - MinHeight + 1)
                where !taken.Contains(string.Format("Maze:{0}:{1}", width, height))
                from x in Enumerable.Range(0, W - width + 1)
                from y in Enumerable.Range(0, H - height + 1)
                where (
                    from dx in Enumerable.Range(0, width)
                    from dy in Enumerable.Range(0, height)
                    select taken.Contains(x + dx + W * (y + dy))).All(b => !b)
                select new { x, y, width, height }).ToArray();

            if (availableConfigs.Length == 0)
                return null;

            var configIx = Rnd.Range(0, availableConfigs.Length);
            var config = availableConfigs[configIx];

            for (var dx = 0; dx < config.width; dx++)
                for (var dy = 0; dy < config.height; dy++)
                    taken.Add(config.x + dx + W * (config.y + dy));
            taken.Add(string.Format("Maze:{0}:{1}", config.width, config.height));

            var shape = Rnd.Range(0, NumShapes);
            return new Maze(module, config.x, config.y, config.width, config.height, Rnd.Range(0, config.width * config.height), shape, _mazes[shape + NumShapes * ((config.height - MinHeight) * NumHeights + (config.width - MinWidth))]);
        }

        public override IEnumerable<object> Flavors
        {
            get
            {
                for (var w = MinWidth; w <= MaxWidth; w++)
                    for (var h = MinHeight; h <= MaxHeight; h++)
                        yield return string.Format("Maze:{0}:{1}", w, h);
            }
        }
    }
}