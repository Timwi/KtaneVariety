using System.Collections.Generic;
using System.Linq;

namespace Variety
{
    public class SliderFactory : ItemFactory
    {
        public override Item Generate(VarietyModule module, HashSet<object> taken)
        {
            var orientations = new[] { SliderOrientation.Horizontal, SliderOrientation.Vertical }.Where(or => !taken.Contains(or)).ToArray();
            if (orientations.Length == 0)
                return null;

            var positions = orientations.SelectMany(or =>
            {
                var sw = or == SliderOrientation.Horizontal ? Slider.LongSlots : Slider.ShortSlots;
                var sh = or == SliderOrientation.Horizontal ? Slider.ShortSlots : Slider.LongSlots;
                var cells = Enumerable.Range(0, sw * sh).Select(v => v % sw + W * (v / sw)).ToArray();
                return Enumerable.Range(0, W * H)
                    .Where(c => c % W <= W - sw && c / W <= H - sh && cells.All(v => !taken.Contains(v + c)))
                    .Select(c => new { X = c % W, Y = c / W, Orientation = or, Cells = cells.Select(v => v + c).ToArray() });
            }).ToArray();
            if (positions.Length == 0)
                return null;

            var position = positions.PickRandom();
            foreach (var cell in position.Cells)
                taken.Add(cell);
            taken.Add(position.Orientation);
            return new Slider(module, position.X, position.Y, position.Orientation, position.Cells);
        }

        public override IEnumerable<object> Flavors
        {
            get
            {
                yield return SliderOrientation.Horizontal;
                yield return SliderOrientation.Vertical;
            }
        }
    }
}