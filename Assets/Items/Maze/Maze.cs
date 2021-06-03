using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Variety
{
    public class Maze : Item
    {
        public Maze(VarietyModule module, int x, int y, int width, int height, int startPos, int shape) : base(module, Enumerable.Range(0, width * height).Select(ix => x + ix % width + W * (y + ix / width)).ToArray())
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Shape = shape;
            State = startPos;
        }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Shape { get; private set; }
        public override int NumStates { get { return Width * Height; } }
        protected override bool CheckStateImmediately { get { return false; } }

        private Vector3 Pos(int cell, bool dot = false)
        {
            const float f = .6f;
            return new Vector3(VarietyModule.CellWidth * ((cell % Width) - (Width - 1) * .5f) * f, dot ? .01502f : .01503f, VarietyModule.CellHeight * ((Height - 1) * .5f - (cell / Width)) * f);
        }

        public override IEnumerable<ItemSelectable> SetUp()
        {
            var prefab = Object.Instantiate(Module.MazeTemplate, Module.transform);

            var cx = -VarietyModule.Width / 2 + (X + (Width - 1) * .5f) * VarietyModule.CellWidth;
            var cy = VarietyModule.Height / 2 - (Y + (Height - 1) * .5f) * VarietyModule.CellHeight + VarietyModule.YOffset;
            prefab.transform.localPosition = new Vector3(cx, 0, cy);

            for (var dx = 0; dx < Width; dx++)
                for (var dy = 0; dy < Height; dy++)
                {
                    var dot = dx == 0 && dy == 0 ? prefab.Dot : Object.Instantiate(prefab.Dot, prefab.transform);
                    dot.transform.localPosition = Pos(dx + Width * dy, dot: true);
                    dot.transform.localEulerAngles = new Vector3(90, 0, 0);
                    dot.transform.localScale = new Vector3(.0025f, .0025f, .0025f);
                }

            prefab.Position.transform.localPosition = Pos(State);
            prefab.PositionRenderer.material.mainTexture = prefab.PositionTextures[Shape];

            yield return new ItemSelectable(prefab.Buttons[0], new[] { X + Width / 2 + W * Y });
            yield return new ItemSelectable(prefab.Buttons[1], new[] { X + Width - 1 + W * (Y + Height / 2) });
            yield return new ItemSelectable(prefab.Buttons[2], new[] { X + Width / 2 + W * (Y + Height - 1) });
            yield return new ItemSelectable(prefab.Buttons[3], new[] { X + W * (Y + Height / 2) });

            for (var i = 0; i < 4; i++)
                prefab.Buttons[i].OnInteract = ButtonPress(i, prefab.Position);
            Module.StartCoroutine(Spin(prefab.Position));
        }

        private IEnumerator Spin(GameObject position)
        {
            var angle = 0f;
            while (Status != ItemStatus.Solved)
            {
                position.transform.localEulerAngles = new Vector3(90, angle, 0);
                yield return null;
                angle += 15 * Time.deltaTime;
            }
        }

        private static readonly int[] _dxs = { 0, 1, 0, -1 };
        private static readonly int[] _dys = { -1, 0, 1, 0 };

        private KMSelectable.OnInteractHandler ButtonPress(int btn, GameObject position)
        {
            return delegate
            {
                var x = State % Width;
                var y = State / Width;
                var nx = x + _dxs[btn];
                var ny = y + _dys[btn];

                if (nx < 0 || nx >= Width || ny < 0 || ny >= Height)
                {
                    Module.Module.HandleStrike();
                    return false;
                }

#warning TODO: Check walls of the maze

                State = nx + Width * ny;
                position.transform.localPosition = Pos(State);
                return false;
            };
        }

        public override string ToString()
        {
            return string.Format("{0}×{1} maze at {2}", Width, Height, coords(Cells[0]));
        }
    }
}