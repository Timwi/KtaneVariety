using System.Collections.Generic;
using System.Linq;

namespace Variety
{
    public class SliderFactory : ItemFactory
    {
        public override Item Generate(VarietyModule module, HashSet<object> taken)
        {
            var orientations = new[] { SliderOrientation.HorizontalSlider, SliderOrientation.VerticalSlider }.Where(or => !taken.Contains(or)).ToArray();
            if (orientations.Length == 0)
                return null;

            var positions = orientations
                .SelectMany(or => Enumerable.Range(0, W * H)
                    .Where(c => isRectAvailable(taken, c, Slider.SW(or), Slider.SH(or)))
                    .Select(c => new { Cell = c, Orientation = or }))
                .ToArray();
            if (positions.Length == 0)
                return null;

            var position = positions.PickRandom();
            claimRect(taken, position.Cell, Slider.SW(position.Orientation), Slider.SH(position.Orientation));
            taken.Add(position.Orientation);
            return new Slider(module, position.Cell, position.Orientation);
        }

        public override IEnumerable<object> Flavors
        {
            get
            {
                yield return SliderOrientation.HorizontalSlider;
                yield return SliderOrientation.VerticalSlider;
            }
        }
    }
}