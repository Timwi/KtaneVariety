using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Variety
{
    public class Maze : Item
    {
        public Maze(int x, int y, int width, int height) : base(Enumerable.Range(0, width * height).Select(ix => x + ix % width + W * (y + ix / width)).ToArray())
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public override IEnumerable<ItemSelectable> SetUp(VarietyModule module)
        {
            var prefab = Object.Instantiate(module.MazeTemplate, module.transform);
            prefab.transform.localPosition = new Vector3(GetX(X) + VarietyModule.CellWidth * (Width + 1) * .5f, 0, GetY(Y) + VarietyModule.CellHeight * (Height + 1) * .5f);
            yield return new ItemSelectable(prefab.Buttons[0], new[] { X + Width / 2 + W * Y });
            yield return new ItemSelectable(prefab.Buttons[1], new[] { X + Width - 1 + W * (Y + Height / 2) });
            yield return new ItemSelectable(prefab.Buttons[2], new[] { X + Width / 2 + W * (Y + Height - 1) });
            yield return new ItemSelectable(prefab.Buttons[3], new[] { X + W * (Y + Height / 2) });
        }

        public override string ToString()
        {
            return string.Format("{0}×{1} maze at {2}", Width, Height, coords(Cells[0]));
        }
    }
}