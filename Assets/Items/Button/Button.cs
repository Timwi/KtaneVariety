using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class Button : Item
    {
        public ButtonColor Color { get; private set; }
        public int ColorValue { get; private set; }
        public int Vertices { get; private set; }

        public Button(VarietyModule module, int topLeftCell, ButtonColor color, int colorValue, int vertices)
            : base(module, CellRect(topLeftCell, 3, 3))
        {
            Color = color;
            ColorValue = colorValue;
            Vertices = vertices;
            State = -1;
        }

        public override IEnumerable<ItemSelectable> SetUp()
        {
            var prefab = UnityEngine.Object.Instantiate(Module.ButtonTemplate, Module.transform);
            prefab.transform.localPosition = new Vector3(GetXOfCellRect(Cells[0], 3), .01501f, GetYOfCellRect(Cells[0], 3));
            prefab.transform.localRotation = Quaternion.Euler(0, Rnd.Range(0, 360), 0);
            prefab.transform.localScale = new Vector3(1f, 1f, 1f);
            prefab.ButtonRenderer.sharedMaterial = prefab.Colors[(int) Color];
            prefab.ButtonMesh.sharedMesh = prefab.Meshes[Vertices - 3];
            SetHighlightMesh(prefab.ButtonHighlight, prefab.Meshes[Vertices - 3]);

            yield return new ItemSelectable(prefab.Button, Cells[0] + W + 1);

            Coroutine waitForSubmit = null;
            var tapped = 0;
            var heldAtTicks = -1;
            var lastTapStarted = Time.time;

            prefab.Button.OnInteract = delegate
            {
                prefab.Button.AddInteractionPunch(.25f);
                Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, prefab.Button.transform);
                Module.MoveButton(prefab.ButtonParent, .005f, ButtonMoveType.Down);

                tapped++;
                heldAtTicks = Module.TimerTicks;
                lastTapStarted = Time.time;
                if (waitForSubmit != null)
                    Module.StopCoroutine(waitForSubmit);
                return false;
            };

            prefab.Button.OnInteractEnded = delegate
            {
                prefab.Button.AddInteractionPunch(.25f);
                Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, prefab.Button.transform);
                Module.MoveButton(prefab.ButtonParent, .005f, ButtonMoveType.Up);

                if (waitForSubmit != null)
                    Module.StopCoroutine(waitForSubmit);

                if (Module.TimerTicks != heldAtTicks && Time.time - lastTapStarted > .25f)
                {
                    var value = Module.TimerTicks - heldAtTicks;
                    State = value >= ColorValue ? -1 : value;
                    tapped = 0;
                }
                else
                    waitForSubmit = Module.StartCoroutine(WaitForSubmit(() =>
                    {
                        State = tapped == 1 ? 0 : tapped + ColorValue - 2;
                        tapped = 0;
                    }));
            };
        }

        private IEnumerator MoveButton(Transform button, bool down)
        {
            var duration = .1f;
            var elapsed = 0f;
            var amount = -.005f;

            while (elapsed < duration)
            {
                button.localPosition = new Vector3(0, Easing.OutQuad(elapsed, down ? 0 : amount, down ? amount : 0, duration), 0);
                yield return null;
                elapsed += Time.deltaTime;
            }
            button.localPosition = new Vector3(0, down ? amount : 0, 0);
        }

        private void SetHighlightMesh(MeshFilter mf, Mesh highlightMesh)
        {
            mf.sharedMesh = highlightMesh;
            var child = mf.transform.Find("Highlight(Clone)");
            var filter = child == null ? null : child.GetComponent<MeshFilter>();
            if (filter != null)
                filter.sharedMesh = highlightMesh;
        }

        private IEnumerator WaitForSubmit(Action action)
        {
            var start = Module.TimerTicks;
            while (Module.TimerTicks - start < 2)
                yield return null;
            action();
        }

        private static readonly string[] _colorNames = { "red", "yellow", "blue", "white" };

        public override string ToString() { return string.Format("{0} button", _colorNames[(int) Color]); }
        public override bool CanProvideStage { get { return true; } }
        public override int NumStates { get { return ColorValue + Vertices; } }
        public override object Flavor { get { return Color; } }
        public override string DescribeWhatUserDid() { return string.Format("you interacted with the {0} button", _colorNames[(int) Color]); }

        public override string DescribeSolutionState(int state)
        {
            return state < ColorValue
                ? string.Format("hold the {0} button across {1} timer ticks", _colorNames[(int) Color], state)
                : string.Format("mash the {0} button {1} times", _colorNames[(int) Color], state - (ColorValue - 2));
        }

        public override string DescribeWhatUserShouldHaveDone(int desiredState)
        {
            var insteadOf = State == -1
                ? "you left it untouched or held it for too long"
                : State < ColorValue
                    ? string.Format("you held it across {0} timer ticks", State)
                    : string.Format("you mashed it {0} times", State - (ColorValue - 2));
            return desiredState < ColorValue
                ? string.Format("you should have held the {0} button across {1} timer ticks ({2})", _colorNames[(int) Color], desiredState, insteadOf)
                : string.Format("you should have mashed the {0} button {1} times ({2})", _colorNames[(int) Color], desiredState - (ColorValue - 2), insteadOf);
        }
    }
}