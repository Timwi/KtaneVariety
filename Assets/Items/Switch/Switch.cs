using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Variety
{
    public class Switch : Item
    {
        public override string TwitchHelpMessage => "!{0} red switch 0 [toggle red switch to up]";

        public override void SetColorblind(bool on)
        {
            foreach (var cb in _switch.GetComponentsInChildren<TextMesh>(true))
                cb.gameObject.SetActive(on);
        }

        public int Cell { get; private set; }
        public SwitchColor Color { get; private set; }
        public int NumPositions { get; private set; }

        private bool _currentDirectionDown = true;
        private KMSelectable _switch;
        Coroutine _switchToggling = null;

        private static readonly string[][] _positionNames = {
            new[] { "up", "down" },
            new[] { "up", "middle", "down" },
            new[] { "up", "half-up", "half-down", "down" }
        };

        public Switch(VarietyModule module, int cell, SwitchColor color, int numPositions)
            : base(module, new[] { cell })
        {
            Cell = cell;
            Color = color;
            NumPositions = numPositions;
            SetState(0, automatic: true);
        }

        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            var prefab = Object.Instantiate(Module.SwitchTemplate, Module.transform);
            prefab.transform.localPosition = new Vector3(GetXOfCellRect(Cells[0], 1), .015f, GetYOfCellRect(Cells[0], 4));
            prefab.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            _switch = prefab.Selectable;
            _switch.OnInteract = ToggleSwitch();
            prefab.MeshRenderer.sharedMaterial = prefab.SwitchMaterials[(int) Color];
            foreach (var cb in _switch.GetComponentsInChildren<TextMesh>(true))
                cb.text = _colorNames[(int) Color][0].ToString().ToUpperInvariant();
            yield return new ItemSelectable(_switch, Cells[0] + W);
        }

        private KMSelectable.OnInteractHandler ToggleSwitch() => delegate
        {
            _switch.AddInteractionPunch(.5f);
            Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, _switch.transform);

            if (State == 0)
                _currentDirectionDown = false;
            if (State == NumPositions - 1)
                _currentDirectionDown = true;
            SetState(State + (_currentDirectionDown ? -1 : 1));

            if (_switchToggling != null)
                Module.StopCoroutine(_switchToggling);
            _switchToggling = Module.StartCoroutine(MoveSwitch(State));
            return false;
        };

        private IEnumerator MoveSwitch(int state)
        {
            var duration = .2f;
            var elapsed = 0f;
            var prevRotation = _switch.transform.localRotation;
            var newRotation = Quaternion.Euler(60f - state * 120f / (NumPositions - 1), 0, 0);

            while (elapsed < duration)
            {
                _switch.transform.transform.localRotation = Quaternion.Slerp(prevRotation, newRotation, Easing.InOutQuad(elapsed, 0, 1, duration));
                yield return null;
                elapsed += Time.deltaTime;
            }
            _switch.transform.transform.localRotation = newRotation;
            _switchToggling = null;
        }

        private static readonly string[] _colorNames = { "blue", "red", "yellow", "white" };

        public override string ToString() => $"{_colorNames[(int) Color]} switch";
        public override bool CanProvideStage => true;
        public override int NumStates => NumPositions;
        public override object Flavor => Color;
        public override string DescribeSolutionState(int state) => $"set the {_colorNames[(int) Color]} switch to {_positionNames[NumPositions - 2][state]}";
        public override string DescribeWhatUserDid() => $"you toggled the {_colorNames[(int) Color]} switch";
        public override string DescribeWhatUserShouldHaveDone(int desiredState) => $"you should have toggled the {_colorNames[(int) Color]} switch to {_positionNames[NumPositions - 2][desiredState]} (instead of {_positionNames[NumPositions - 2][State]})";

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, $@"^\s*{_colorNames[(int) Color]}\s+switch\s+(\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            int val;
            if (m.Success && int.TryParse(m.Groups[1].Value, out val) && val >= 0 && val < NumPositions)
                return TwitchSet(val).GetEnumerator();
            return null;
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState) => TwitchSet(desiredState);

        private IEnumerable<object> TwitchSet(int val)
        {
            while (State != val)
            {
                _switch.OnInteract();
                while (_switchToggling != null)
                    yield return true;
                yield return new WaitForSeconds(.1f);
            }
        }
    }
}