using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

namespace Variety
{
    public class MazeFactory : ItemFactory
    {
        private const int MinWidth = 3;
        private const int MaxWidth = 4;
        private const int MinHeight = 3;
        private const int MaxHeight = 4;
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
                from cell in Enumerable.Range(0, W * H)
                where isRectAvailable(taken, cell, width + 1, height + 1)
                select new { Cell = cell, Width = width, Height = height }).ToArray();

            if (availableConfigs.Length == 0)
                return null;

            var configIx = Rnd.Range(0, availableConfigs.Length);
            var config = availableConfigs[configIx];

            claimRect(taken, config.Cell, config.Width + 1, config.Height + 1);
            taken.Add(string.Format("Maze:{0}:{1}", config.Width, config.Height));

            var shape = Rnd.Range(0, NumShapes);
            return new Maze(module, config.Cell % W, config.Cell / W, config.Width, config.Height, Rnd.Range(0, config.Width * config.Height), shape,
                _mazes[shape + NumShapes * (config.Height - MinHeight + NumHeights * (config.Width - MinWidth))]);
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