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

        public override string TwitchHelpMessage => "!{0} red button hold 2 [hold the red button over that many timer ticks] | !{0} red button mash 3 [mash the red button that many times]";

        public override void SetColorblind(bool on)
        {
            _button.GetComponentInChildren<TextMesh>(true).gameObject.SetActive(on);
        }

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
            var rotation = rnd.Next(0, 360);
            prefab.transform.localRotation = Quaternion.Euler(0, rotation, 0);
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

            _button.GetComponentInChildren<TextMesh>(true).text = _colorNames[(int) Color][0].ToString().ToUpperInvariant();
            _button.GetComponentInChildren<TextMesh>(true).transform.localRotation = Quaternion.Euler(90f, 0f, rotation);

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

        public override string ToString() => $"{_colorNames[(int) Color]} button";
        public override bool CanProvideStage => true;
        public override int NumStates => ColorValue + Vertices;
        public override object Flavor => Color;
        public override string DescribeWhatUserDid() => $"you interacted with the {_colorNames[(int) Color]} button";

        public override string DescribeSolutionState(int state) => state < ColorValue
            ? $"hold the {_colorNames[(int) Color]} button across {state} timer ticks"
            : $"mash the {_colorNames[(int) Color]} button {state - (ColorValue - 2)} times";

        public override string DescribeWhatUserShouldHaveDone(int desiredState)
        {
            var insteadOf = State == -1
                ? "you left it untouched, held it for too long, or moved on too quickly"
                : State < ColorValue
                    ? $"you held it across {State} timer ticks"
                    : $"you mashed it {State - (ColorValue - 2)} times";
            return desiredState < ColorValue
                ? $"you should have held the {_colorNames[(int) Color]} button across {desiredState} timer ticks{(desiredState == 0 ? " and then waited two timer ticks" : "")} ({insteadOf})"
                : $"you should have mashed the {_colorNames[(int) Color]} button {desiredState - (ColorValue - 2)} times and then waited two timer ticks ({insteadOf})";
        }

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, $@"^\s*{_colorNames[(int) Color]}\s+button\s+mash\s+(\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            int amount;
            if (m.Success && int.TryParse(m.Groups[1].Value, out amount) && amount > 0 && amount <= 10)
                return TwitchMash(amount).GetEnumerator();

            m = Regex.Match(command, $@"^\s*{_colorNames[(int) Color]}\s+button\s+hold\s+(\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
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

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState) => desiredState == 0 && ColorValue != 0
            ? TwitchMash(1)
            : desiredState < ColorValue
                ? TwitchHold(desiredState)
                : TwitchMash(desiredState - (ColorValue - 2));
    }
}