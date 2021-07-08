using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Variety
{
    public class Maze : Item
    {
        public Maze(VarietyModule module, int x, int y, int width, int height, int startPos, int shape, MazeLayout maze) : base(module, Enumerable.Range(0, width * height).Select(ix => x + ix % width + W * (y + ix / width)).ToArray())
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Shape = shape;
            State = startPos;
            _maze = maze;
        }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Shape { get; private set; }
        public override int NumStates { get { return Width * Height; } }

        private readonly MazeLayout _maze;

        private Vector3 Pos(int cell, bool dot = false)
        {
            return new Vector3((cell % Width) - (Width - 1) * .5f, dot ? .003f : .004f, (Height - 1) * .5f - (cell / Width));
        }

        public override IEnumerable<ItemSelectable> SetUp()
        {
            var prefab = Object.Instantiate(Module.MazeTemplate, Module.transform);

            var cx = -VarietyModule.Width / 2 + (X + Width * .5f) * VarietyModule.CellWidth;
            var cy = VarietyModule.Height / 2 - (Y + Height * .5f) * VarietyModule.CellHeight + VarietyModule.YOffset;
            prefab.transform.localPosition = new Vector3(cx, .01502f, cy);
            prefab.transform.localRotation = Quaternion.identity;
            prefab.transform.localScale = new Vector3(VarietyModule.CellWidth * .75f, VarietyModule.CellWidth * .75f, VarietyModule.CellWidth * .75f);

            var dots = new GameObject[Width * Height];
            for (var dx = 0; dx < Width; dx++)
                for (var dy = 0; dy < Height; dy++)
                {
                    var dot = dx == 0 && dy == 0 ? prefab.Dot : Object.Instantiate(prefab.Dot, prefab.transform);
                    dot.transform.localPosition = Pos(dx + Width * dy, dot: true);
                    dot.transform.localEulerAngles = new Vector3(90, 0, 0);
                    dot.transform.localScale = new Vector3(.3f, .3f, .3f);
                    dot.SetActive(dx + Width * dy != State);
                    dots[dx + Width * dy] = dot;
                }

            prefab.Position.transform.localPosition = Pos(State);
            prefab.Position.transform.localScale = new Vector3(1, 1, 1);
            prefab.PositionRenderer.material.mainTexture = prefab.PositionTextures[Shape];

            var frameMeshName = string.Format("Frame{0}x{1}", Width, Height);
            prefab.Frame.sharedMesh = prefab.FrameMeshes.First(m => m.name == frameMeshName);
            var backMeshName = string.Format("Back{0}x{1}", Width, Height);
            prefab.Back.sharedMesh = prefab.BackMeshes.First(m => m.name == backMeshName);

            prefab.ButtonPos[0].localPosition = new Vector3(0, 0, -.5f - .5f * Height);
            prefab.ButtonPos[1].localPosition = new Vector3(0, 0, -.5f - .5f * Width);
            prefab.ButtonPos[2].localPosition = new Vector3(0, 0, -.5f - .5f * Height);
            prefab.ButtonPos[3].localPosition = new Vector3(0, 0, -.5f - .5f * Width);

            prefab.ButtonColliders[0].center = new Vector3(0, 0, -.25f - .5f * Height);
            prefab.ButtonColliders[1].center = new Vector3(0, 0, -.25f - .5f * Width);
            prefab.ButtonColliders[2].center = new Vector3(0, 0, -.25f - .5f * Height);
            prefab.ButtonColliders[3].center = new Vector3(0, 0, -.25f - .5f * Width);

            yield return new ItemSelectable(prefab.Buttons[0], X + Width / 2 + W * Y);
            yield return new ItemSelectable(prefab.Buttons[1], X + Width - 1 + W * (Y + Height / 2));
            yield return new ItemSelectable(prefab.Buttons[2], X + Width / 2 + W * (Y + Height - 1));
            yield return new ItemSelectable(prefab.Buttons[3], X + W * (Y + Height / 2));

            for (var i = 0; i < 4; i++)
                prefab.Buttons[i].OnInteract = ButtonPress(prefab.Buttons[i], i, prefab.Position, dots);
            Module.StartCoroutine(Spin(prefab.Position));
        }

        private IEnumerator Spin(GameObject position)
        {
            var angle = 0f;
            while (true)
            {
                position.transform.localEulerAngles = new Vector3(90, angle, 0);
                yield return null;
                angle += 15 * Time.deltaTime;
            }
        }

        private static readonly int[] _dxs = { 0, 1, 0, -1 };
        private static readonly int[] _dys = { -1, 0, 1, 0 };

        private KMSelectable.OnInteractHandler ButtonPress(KMSelectable button, int btnIx, GameObject position, GameObject[] dots)
        {
            return delegate
            {
                button.AddInteractionPunch(.25f);
                Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);

                var x = State % Width;
                var y = State / Width;
                var nx = x + _dxs[btnIx];
                var ny = y + _dys[btnIx];

                if (nx < 0 || nx >= Width || ny < 0 || ny >= Height || !_maze.CanGo(State, btnIx))
                {
                    Module.Module.HandleStrike();
                    Debug.LogFormat(@"[Variety #{0}] In the {1}×{2} maze, you tried to go from {3}{4} to {5}{6} but there’s a wall there.",
                        Module.ModuleID, Width, Height, (char) ('A' + x), y + 1, (char) ('A' + nx), ny + 1);
                    return false;
                }

                State = nx + Width * ny;
                position.transform.localPosition = Pos(State);
                foreach (var dot in dots)
                    dot.SetActive(true);
                dots[State].SetActive(false);
                return false;
            };
        }

        private static readonly string[] _symbolColors = { "red", "yellow", "blue" };
        private static readonly string[] _symbolNames = { "plus", "star", "triangle" };

        public override string ToString() { return string.Format("{0}×{1} maze with a {2} {3}", Width, Height, _symbolColors[Shape % 3], _symbolNames[Shape / 3]); }
        public override object Flavor { get { return string.Format("Maze:{0}:{1}", Width, Height); } }
        public override string DescribeSolutionState(int state) { return string.Format("go to {0}{1} in the {2}×{3} maze", (char) (state % Width + 'A'), state / Width + 1, Width, Height); }
        public override string DescribeWhatUserDid() { return string.Format("you moved in the {0}×{1} maze", Width, Height); }
        public override string DescribeWhatUserShouldHaveDone(int desiredState) { return string.Format("you should have moved to {0}{1} in the {2}×{3} maze (instead of {4}{5})", (char) (desiredState % Width + 'A'), desiredState / Width + 1, Width, Height, (char) (State % Width + 'A'), State / Width + 1); }
    }
}