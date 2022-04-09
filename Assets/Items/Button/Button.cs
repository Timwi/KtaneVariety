using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Variety
{
    public class Button : Item
    {
        public ButtonColor Color { get; private set; }
        public int ColorValue { get; private set; }
        public int Vertices { get; private set; }

        public override string TwitchHelpMessage { get { return "!{0} red button hold 2 [hold the red button over that many timer ticks] | !{0} red button mash 3 [mash the red button that many times]"; } }

        private KMSelectable _button;

        public Button(VarietyModule module, int topLeftCell, ButtonColor color, int colorValue, int vertices)
            : base(module, CellRect(topLeftCell, 3, 3))
        {
            Color = color;
            ColorValue = colorValue;
            Vertices = vertices;
            SetState(-1, automatic: true);
        }

        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            var prefab = UnityEngine.Object.Instantiate(Module.ButtonTemplate, Module.transform);
            prefab.transform.localPosition = new Vector3(GetXOfCellRect(Cells[0], 3), .01501f, GetYOfCellRect(Cells[0], 3));
            prefab.transform.localRotation = Quaternion.Euler(0, rnd.Next(0, 360), 0);
            prefab.transform.localScale = new Vector3(1f, 1f, 1f);
            prefab.ButtonRenderer.sharedMaterial = prefab.Colors[(int) Color];
            prefab.ButtonMesh.sharedMesh = prefab.Meshes[Vertices - 3];
            SetHighlightMesh(prefab.ButtonHighlight, prefab.Meshes[Vertices - 3]);

            Coroutine waitForSubmit = null;
            var tapped = 0;
            var heldAtTicks = -1;
            var lastTapStarted = Time.time;

            _button = prefab.Button;
            _button.OnInteract = delegate
            {
                _button.AddInteractionPunch(.25f);
                Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, _button.transform);
                Module.MoveButton(prefab.ButtonParent, .005f, ButtonMoveType.Down);

                tapped++;
                Debug.LogFormat("<Variety #{0}> Held at {1}", Module.ModuleID, Module.TimerTicks);
                heldAtTicks = Module.TimerTicks;
                lastTapStarted = Time.time;
                if (waitForSubmit != null)
                    Module.StopCoroutine(waitForSubmit);
                return false;
            };

            _button.OnInteractEnded = delegate
            {
                Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, _button.transform);
                Module.MoveButton(prefab.ButtonParent, .005f, ButtonMoveType.Up);

                if (waitForSubmit != null)
                    Module.StopCoroutine(waitForSubmit);

                if (Module.TimerTicks != heldAtTicks && Time.time - lastTapStarted > .25f)
                {
                    var numTicksHeld = Module.TimerTicks - heldAtTicks;
                    SetState(numTicksHeld >= ColorValue ? -1 : numTicksHeld);
                    tapped = 0;
                }
                else
                {
                    waitForSubmit = Module.StartCoroutine(WaitForSubmit(() =>
                    {
                        SetState(tapped == 1 ? 0 : tapped + ColorValue - 2);
                        tapped = 0;
                    }));
                }
            };

            yield return new ItemSelectable(_button, Cells[0] + W + 1);
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
                ? "you left it untouched, held it for too long, or moved on too quickly"
                : State < ColorValue
                    ? string.Format("you held it across {0} timer ticks", State)
                    : string.Format("you mashed it {0} times", State - (ColorValue - 2));
            return desiredState < ColorValue
                ? string.Format("you should have held the {0} button across {1} timer ticks{3} ({2})", _colorNames[(int) Color], desiredState, insteadOf, desiredState == 0 ? " and then waited two timer ticks" : "")
                : string.Format("you should have mashed the {0} button {1} times and then waited two timer ticks ({2})", _colorNames[(int) Color], desiredState - (ColorValue - 2), insteadOf);
        }

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, string.Format(@"^\s*{0}\s+button\s+mash\s+(\d+)\s*$", _colorNames[(int) Color]), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            int amount;
            if (m.Success && int.TryParse(m.Groups[1].Value, out amount) && amount > 0 && amount <= 10)
                return TwitchMash(amount).GetEnumerator();

            m = Regex.Match(command, string.Format(@"^\s*{0}\s+button\s+hold\s+(\d+)\s*$", _colorNames[(int) Color]), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (m.Success && int.TryParse(m.Groups[1].Value, out amount) && amount >= 0 && amount <= 10)
                return (amount == 0 ? TwitchMash(1) : TwitchHold(amount)).GetEnumerator();

            return null;
        }

        private IEnumerable<object> TwitchHold(int amount)
        {
            var startTicks = Module.TimerTicks;
            var holdStarted = Time.time;
            _button.OnInteract();
            yield return new WaitForSeconds(.05f);
            while (Module.TimerTicks - startTicks < amount || Time.time - holdStarted <= .25f)
                yield return null;
            _button.OnInteractEnded();
            yield return new WaitForSeconds(.1f);
        }

        private IEnumerable<object> TwitchMash(int amount)
        {
            for (var i = 0; i < amount; i++)
            {
                _button.OnInteract();
                yield return new WaitForSeconds(.05f);
                _button.OnInteractEnded();
                yield return new WaitForSeconds(.1f);
            }
            var startTicks = Module.TimerTicks;
            while (Module.TimerTicks - startTicks < 2)
                yield return true;
            yield return new WaitForSeconds(.1f);
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState)
        {
            return desiredState == 0 && ColorValue != 0 ? TwitchMash(1) : desiredState < ColorValue ? TwitchHold(desiredState) : TwitchMash(desiredState - (ColorValue - 2));
        }
    }
}