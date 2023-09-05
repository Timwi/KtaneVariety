using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Variety
{
    public class ColoredKnob : Item
    {
        public override string TwitchHelpMessage => "!{0} red knob 0 [turn knob that many times] !{0} red knob cycle [turn the knob slowly]";

        public override void SetColorblind(bool on)
        {
            _coloredKnob.GetComponentInChildren<TextMesh>(true).gameObject.SetActive(on);
        }

        public bool[] RealTicks { get; private set; }
        public int[] States { get; private set; }
        public int Rotation { get; private set; }
        public ColoredKnobColor Color { get; private set; }

        private Coroutine _turning;
        private KMSelectable _coloredKnob;
        private int _baseRotation;

        public ColoredKnob(VarietyModule module, int topLeftCell, ColoredKnobColor color, int baseRotation, int n, System.Random rnd)
            : base(module, CellRect(topLeftCell, 2, 2))
        {
            RealTicks = Enumerable.Repeat(true, n).Concat(Enumerable.Repeat(false, 8 - n)).ToArray().Shuffle(rnd);
            Rotation = rnd.Next(0, 8);
            States = Enumerable.Range(0, 8).Select(i => RealTicks[i] ? RealTicks.Take(i).Count(b => b) : -1).ToArray();
            Color = color;
            _baseRotation = baseRotation;

            SetState(States[Rotation], automatic: true);
        }

        private void SetPosition(int pos)
        {
            SetState(States[pos]);
            if (_turning != null)
                Module.StopCoroutine(_turning);
            _turning = Module.StartCoroutine(turnTo(pos));
        }

        private IEnumerator turnTo(int pos)
        {
            var oldRot = _coloredKnob.transform.localRotation;
            var newRot = Quaternion.Euler(0f, 45f * pos, 0f);
            var duration = .1f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                _coloredKnob.transform.localRotation = Quaternion.Slerp(oldRot, newRot, Easing.InOutQuad(elapsed, 0, 1, duration));
                yield return null;
                elapsed += Time.deltaTime;
            }
            _coloredKnob.transform.localRotation = newRot;
            _turning = null;
        }

        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            var prefab = Object.Instantiate(Module.ColoredKnobTemplate, Module.transform);
            prefab.transform.localPosition = new Vector3(GetXOfCellRect(Cells[0], 2), .015f, GetYOfCellRect(Cells[0], 2));
            prefab.transform.localRotation = Quaternion.Euler(0f, 90f * _baseRotation, 0f);
            prefab.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

            _coloredKnob = prefab.Dial;
            _coloredKnob.GetComponentInChildren<TextMesh>(true).text = Color == ColoredKnobColor.BlackKnob ? "" : Color.ToString().Substring(0, 1);
            _coloredKnob.transform.GetChild(0).GetComponent<Renderer>().sharedMaterial = prefab.Materials[(int) Color];
            _coloredKnob.transform.localRotation = Quaternion.Euler(0f, 45f * Rotation, 0f);
            _coloredKnob.OnInteract = delegate
            {
                _coloredKnob.AddInteractionPunch(.25f);
                Rotation = (Rotation + 1) % 8;
                Module.Audio.PlayGameSoundAtTransform(RealTicks[Rotation] ? KMSoundOverride.SoundEffect.ButtonPress : KMSoundOverride.SoundEffect.ButtonRelease, _coloredKnob.transform);
#if UNITY_EDITOR
                Debug.Log(RealTicks[Rotation] ? "Click" : "No click");
#endif
                SetPosition(Rotation);
                return false;
            };
            yield return new ItemSelectable(_coloredKnob, Cells[0]);
        }

        public override string ToString() => $"{ColorName} knob ({RealTicks.Count(b => b)} clicks)";
        public override int NumStates => RealTicks.Count(b => b);
        public override object Flavor => Color;
        private static readonly string[] Ord = new string[] { "0th", "1st", "2nd", "3rd", "4th", "5th", "6th", "7th" };
        private string ColorName => new string[] { "red", "black", "blue", "yellow" }[(int) Color];
        public override string DescribeSolutionState(int state) => $"set the {ColorName} knob to the {Ord[state]} clicking spot from {new[] { "North", "East", "South", "West" }[_baseRotation]}";
        public override string DescribeWhatUserDid() => $"you turned the {ColorName} knob";
        public override string DescribeWhatUserShouldHaveDone(int desiredState) => $"you should have set the {ColorName} knob to the {Ord[desiredState]} clicking spot (instead of {(State != -1 ? "the " : "")}{(State != -1 ? Ord[State] : "a non-clicking spot")})";

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, $@"^\s*{ColorName}\s+knob\s+(?:cycle|listen)(?<fast>\s*fast)?\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (m.Success)
                return TwitchCycle(m.Groups["fast"].Success).GetEnumerator();
            m = Regex.Match(command, $@"^\s*{ColorName}\s+knob\s+(\d)\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            int val;
            if (m.Success && int.TryParse(m.Groups[1].Value, out val) && val > 0 && val < 8)
                return TwitchPress(val).GetEnumerator();
            return null;
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState) => TwitchPress((System.Array.IndexOf(States, desiredState) - Rotation + 8) % 8);

        private IEnumerable<object> TwitchCycle(bool fast)
        {
            for (int i = 0; i < 8; i++)
            {
                _coloredKnob.OnInteract();
                yield return new WaitForSeconds(fast ? .3f : .9f);
            }
        }

        private IEnumerable<object> TwitchPress(int val)
        {
            for (int i = 0; i < val; i++)
            {
                _coloredKnob.OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
    }
}