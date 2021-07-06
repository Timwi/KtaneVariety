using System;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using Rnd = UnityEngine.Random;

namespace Variety
{
    public class WireFactory : ItemFactory
    {
        private const double _allowedDistance = .5;
        private Func<KMBombInfo, bool>[] _edgeworkConditions;

        public WireFactory(MonoRandom ruleSeedRnd)
        {
            _edgeworkConditions = new Func<KMBombInfo, bool>[]
            {
                bomb => bomb.GetOnIndicators().Count() > bomb.GetOffIndicators().Count(),
                bomb => bomb.GetBatteryCount(Battery.D) > bomb.GetBatteryCount(Battery.AA)+bomb.GetBatteryCount(Battery.AAx3)+bomb.GetBatteryCount(Battery.AAx4),
                bomb => bomb.GetPorts().Count(p => p=="Parallel"||p=="Serial") > bomb.GetPorts().Count(p => p!="Parallel"&&p!="Serial"),
                bomb => bomb.GetSerialNumberLetters().Count() > bomb.GetSerialNumberNumbers ().Count(),
                bomb => bomb.GetBatteryHolderCount() > bomb.GetPortPlateCount()
            };
            ruleSeedRnd.ShuffleFisherYates(_edgeworkConditions);
        }

        public override Item Generate(VarietyModule module, HashSet<object> taken)
        {
            var availableColors = ((WireColor[]) Enum.GetValues(typeof(WireColor))).Where(c => !taken.Contains(c)).ToArray();
            if (availableColors.Length == 0)
                return null;

            var existingWires = taken.OfType<string>().Where(s => s.StartsWith("Wire:")).Select(s => s.Split(':')).Select(arr => new { Cell1 = int.Parse(arr[1]), Cell2 = int.Parse(arr[2]) }).ToArray();

            var availableCells = Enumerable.Range(0, W * H).Where(cell => !taken.Contains(cell) && !existingWires.Any(ew => liesOn(cell, ew.Cell1, ew.Cell2))).ToArray();

            var availableWires = new List<int>();
            for (var startIx = 0; startIx < availableCells.Length; startIx++)
                for (var endIx = startIx + 1; endIx < availableCells.Length; endIx++)
                    if ((Math.Abs(availableCells[startIx] % W - availableCells[endIx] % W) > 1 || Math.Abs(availableCells[startIx] / W - availableCells[endIx] / W) > 1)
                        && !existingWires.Any(ew => doIntersect(ew.Cell1, ew.Cell2, availableCells[startIx], availableCells[endIx]))
                        && Enumerable.Range(0, W * H).All(cell => distance(availableCells[startIx] % W, availableCells[startIx] / W, availableCells[endIx] % W, availableCells[endIx] / W, cell % W, cell / W) >= _allowedDistance || !taken.Contains(cell)))
                        availableWires.Add(availableCells[endIx] * W * H + availableCells[startIx]);

            if (availableWires.Count == 0)
                return null;

            var color = availableColors[Rnd.Range(0, availableColors.Length)];
            var wire = availableWires[Rnd.Range(0, availableWires.Count)];
            var cell1 = wire % (W * H);
            var cell2 = wire / (W * H);
            taken.Add(cell1);
            taken.Add(cell2);
            taken.Add(color);
            taken.Add(string.Format("Wire:{0}:{1}", cell1, cell2));
            for (var cell = 0; cell < W * H; cell++)
                if (distance(cell1 % W, cell1 / W, cell2 % W, cell2 / W, cell % W, cell / W) < _allowedDistance)
                    taken.Add(cell);
            return new Wire(module, color, new[] { Math.Min(cell1, cell2), Math.Max(cell1, cell2) }, _edgeworkConditions[(int) color]);
        }

        public override IEnumerable<object> Flavors { get { return Enum.GetValues(typeof(WireColor)).Cast<object>(); } }

        private static bool doIntersect(int wire1s, int wire1e, int wire2s, int wire2e)
        {
            var w1sx = wire1s % W;
            var w1sy = wire1s / W;
            var w1ex = wire1e % W;
            var w1ey = wire1e / W;
            var w2sx = wire2s % W;
            var w2sy = wire2s / W;
            var w2ex = wire2e % W;
            var w2ey = wire2e / W;

            var l1dx = w1ex - w1sx;
            var l1dy = w1ey - w1sy;
            var l2dx = w2ex - w2sx;
            var l2dy = w2ey - w2sy;

            var det = l1dx * l2dy - l1dy * l2dx;
            var l1 = l2dx * (w1sy - w2sy) - l2dy * (w1sx - w2sx);
            var l2 = l1dx * (w1sy - w2sy) - l1dy * (w1sx - w2sx);
            return det != 0 && (det > 0
                ? l1 >= 0 && l1 <= det && l2 >= 0 && l2 <= det
                : l1 <= 0 && l1 >= det && l2 <= 0 && l2 >= det);
        }

        private static bool liesOn(int point, int start, int end)
        {
            var x = point % W;
            var y = point / W;
            var sx = start % W;
            var sy = start / W;
            var ex = end % W;
            var ey = end / W;

            if (sx == ex)
                return x == sx && y >= sy && y <= ey;
            if (sx > ex)
                return liesOn(point, end, start);
            return x >= sx && x <= ex && (x - sx) * (ey - sy) == (ex - sx) * (y - sy);
        }

        private static double lambdaOfPointDroppedPerpendicularly(int lx1, int ly1, int lx2, int ly2, int px, int py)
        {
            double dirX = lx2 - lx1;
            double dirY = ly2 - ly1;
            return (dirX * (px - lx1) + dirY * (py - ly1)) / (dirX * dirX + dirY * dirY);
        }

        private static double distance(int lx1, int ly1, int lx2, int ly2, int px, int py)
        {
            var lambda = lambdaOfPointDroppedPerpendicularly(lx1, ly1, lx2, ly2, px, py);
            var dx = lx1 + lambda * (lx2 - lx1);
            var dy = ly1 + lambda * (ly2 - ly1);
            return
                lambda <= 0 ? Math.Sqrt(Math.Pow(px - lx1, 2) + Math.Pow(py - ly1, 2)) :
                lambda >= 1 ? Math.Sqrt(Math.Pow(px - lx2, 2) + Math.Pow(py - ly2, 2)) :
                Math.Sqrt(Math.Pow(px - dx, 2) + Math.Pow(py - dy, 2));
        }
    }
}