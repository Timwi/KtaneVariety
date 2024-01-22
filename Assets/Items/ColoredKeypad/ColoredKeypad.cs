using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Variety
{
    public class ColoredKeypad : Item
    {
        public override string TwitchHelpMessage => "!{0} red keys 01 [press those keys on the red keypad]";

        public override void SetColorblind(bool on)
        {
            foreach (var text in _prefab.GetComponentsInChildren<TextMesh>(true))
                text.gameObject.SetActive(on);
        }

        public static readonly Dictionary<ColoredKeypadSize, int> Widths = new Dictionary<ColoredKeypadSize, int>
        {
            { ColoredKeypadSize.ColoredKeypad1x4, 1 },
            { ColoredKeypadSize.ColoredKeypad1x5, 1 },
            { ColoredKeypadSize.ColoredKeypad1x6, 1 },
            { ColoredKeypadSize.ColoredKeypad2x2, 2 },
            { ColoredKeypadSize.ColoredKeypad2x3, 2 },
            { ColoredKeypadSize.ColoredKeypad3x2, 3 },
            { ColoredKeypadSize.ColoredKeypad4x1, 4 },
            { ColoredKeypadSize.ColoredKeypad5x1, 5 },
            { ColoredKeypadSize.ColoredKeypad6x1, 6 }
        };

        public static readonly Dictionary<ColoredKeypadSize, int> Heights = new Dictionary<ColoredKeypadSize, int>
        {
            { ColoredKeypadSize.ColoredKeypad1x4, 4 },
            { ColoredKeypadSize.ColoredKeypad1x5, 5 },
            { ColoredKeypadSize.ColoredKeypad1x6, 6 },
            { ColoredKeypadSize.ColoredKeypad2x2, 2 },
            { ColoredKeypadSize.ColoredKeypad2x3, 3 },
            { ColoredKeypadSize.ColoredKeypad3x2, 2 },
            { ColoredKeypadSize.ColoredKeypad4x1, 1 },
            { ColoredKeypadSize.ColoredKeypad5x1, 1 },
            { ColoredKeypadSize.ColoredKeypad6x1, 1 }
        };

        public static readonly Dictionary<ColoredKeypadSize, int> CWidths = new Dictionary<ColoredKeypadSize, int>
        {
            { ColoredKeypadSize.ColoredKeypad1x4, 2 },
            { ColoredKeypadSize.ColoredKeypad1x5, 2 },
            { ColoredKeypadSize.ColoredKeypad1x6, 2 },
            { ColoredKeypadSize.ColoredKeypad2x2, 3 },
            { ColoredKeypadSize.ColoredKeypad2x3, 3 },
            { ColoredKeypadSize.ColoredKeypad3x2, 4 },
            { ColoredKeypadSize.ColoredKeypad4x1, 6 },
            { ColoredKeypadSize.ColoredKeypad5x1, 7 },
            { ColoredKeypadSize.ColoredKeypad6x1, 8 }
        };

        public static readonly Dictionary<ColoredKeypadSize, int> CHeights = new Dictionary<ColoredKeypadSize, int>
        {
            { ColoredKeypadSize.ColoredKeypad1x4, 6 },
            { ColoredKeypadSize.ColoredKeypad1x5, 7 },
            { ColoredKeypadSize.ColoredKeypad1x6, 8 },
            { ColoredKeypadSize.ColoredKeypad2x2, 3 },
            { ColoredKeypadSize.ColoredKeypad2x3, 4 },
            { ColoredKeypadSize.ColoredKeypad3x2, 3 },
            { ColoredKeypadSize.ColoredKeypad4x1, 2 },
            { ColoredKeypadSize.ColoredKeypad5x1, 2 },
            { ColoredKeypadSize.ColoredKeypad6x1, 2 }
        };

        private static readonly string[] _colorNames = { "red", "yellow", "blue" };

        public ColoredKeypadSize Size { get; private set; }
        public ColoredKeypadColor Color { get; private set; }
        private int numKeys => Widths[Size] * Heights[Size];

        private readonly HashSet<int> _presses = new HashSet<int>();
        private MeshRenderer[] _leds;
        private ColoredKeypadPrefab _prefab;
        private KMSelectable[] _buttons;
        private Transform[] _buttonParents;
        private readonly int _expectedPresses;
        private int[] _combinations;

        public ColoredKeypad(VarietyModule module, ColoredKeypadColor color, ColoredKeypadSize size, int topLeftCell, int expectedPresses)
            : base(module, CellRect(topLeftCell, Widths[size] * 4 / 3, Heights[size] * 4 / 3))
        {
            Color = color;
            Size = size;
            SetState(-1, automatic: true);
            _expectedPresses = expectedPresses;
        }

        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            var w = Widths[Size];
            var h = Heights[Size];

            const float d = .0225f;
            const float s = .033f;

            _prefab = Object.Instantiate(Module.ColoredKeypadTemplate, Module.transform);
            _prefab.transform.localPosition = new Vector3(GetXOfCellRect(Cells[0], CWidths[Size]), .015f, GetYOfCellRect(Cells[0], CHeights[Size]));
            _prefab.transform.localRotation = Quaternion.identity;
            _prefab.transform.localScale = new Vector3(.95f, .95f, .95f);
            _leds = new MeshRenderer[w * h];

            _prefab.Backing.localScale = new Vector3(d * w + .001f, d * h + .001f);
            _buttons = new KMSelectable[w * h];
            _buttonParents = new Transform[w * h];

            _combinations = Enumerable.Range(0, 1 << numKeys).Where(val => numBits(val) == _expectedPresses).ToArray();

            for (var keyIx = 0; keyIx < w * h; keyIx++)
            {
                _buttons[keyIx] = keyIx == 0 ? _prefab.KeyTemplate : Object.Instantiate(_prefab.KeyTemplate, _prefab.transform);
                _buttons[keyIx].name = $"Key {keyIx + 1}";
                _buttons[keyIx].transform.localPosition = new Vector3(d * (keyIx % w - (w - 1) * .5f), 0, d * ((h - 1 - keyIx / w) - (h - 1) * .5f));
                _buttons[keyIx].transform.localRotation = Quaternion.identity;
                _buttons[keyIx].transform.localScale = new Vector3(s, s, s);
                _buttons[keyIx].OnInteract = pressed(_buttons[keyIx], keyIx);
                _buttonParents[keyIx] = _buttons[keyIx].transform.Find("KeyCapParent");
                _leds[keyIx] = _buttonParents[keyIx].Find("Led").GetComponent<MeshRenderer>();
                _buttonParents[keyIx].Find("KeyCap").GetComponent<MeshRenderer>().sharedMaterial = _prefab.KeycapColors[(int) Color];
                _buttonParents[keyIx].GetComponentInChildren<TextMesh>(true).text = _colorNames[(int) Color][0].ToString().ToUpperInvariant();

                yield return new ItemSelectable(_buttons[keyIx], Cells[0] + (keyIx % w) + W * (keyIx / w));
            }
        }

        private int numBits(int val)
        {
            var num = 0;
            while (val > 0)
            {
                num++;
                val &= val - 1;
            }
            return num;
        }

        private KMSelectable.OnInteractHandler pressed(KMSelectable key, int keyIx) => delegate
        {
            key.AddInteractionPunch(.25f);
            Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, key.transform);
            Module.MoveButton(_buttonParents[keyIx], .1f, ButtonMoveType.DownThenUp);

            if (_presses.Contains(keyIx))
                _presses.Clear();
            else
                _presses.Add(keyIx);

            for (var i = 0; i < numKeys; i++)
                _leds[i].sharedMaterial = _presses.Contains(i) ? _prefab.LedOn : _prefab.LedOff;

            SetState(_presses.Count == _expectedPresses ? _combinations.IndexOf(i => _presses.All(p => (i & (1 << (numKeys - 1 - p))) != 0)) : -1);
            return false;
        };

        public override string ToString() => $"{_colorNames[(int) Color]} keypad with {numKeys} keys";
        public override int NumStates => _combinations.Length;
        public override object Flavor => Color;

        public override string DescribeSolutionState(int state) => $"press keys {getSequence(state).Join(", ")} on the {_colorNames[(int) Color]} keypad";
        public override string DescribeWhatUserDid() => $"you pressed keys on the {_colorNames[(int) Color]} keypad";
        public override string DescribeWhatUserShouldHaveDone(int desiredState) => $"you should have pressed keys {getSequence(desiredState).Join(", ")} on the {_colorNames[(int) Color]} keypad ({(_presses.Count == 0 ? "you didn’t press any" : $"instead of {_presses.Join(", ")}")})";

        private int[] getSequence(int state) => Enumerable.Range(0, numKeys).Where(key => (_combinations[state] & (1 << (numKeys - 1 - key))) != 0).ToArray();

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, $@"^\s*{_colorNames[(int) Color]}\s+keys\s+(\d+)\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (!m.Success || m.Groups[1].Value.Any(ch => ch < '0' || ch >= '0' + numKeys))
                return null;
            var numbers = m.Groups[1].Value.Select(ch => ch - '0').ToArray();
            return TwitchPress(numbers).GetEnumerator();
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState) => TwitchPress(getSequence(desiredState));

        private IEnumerable<object> TwitchPress(int[] indexes)
        {
            if (_presses.Any())
            {
                _buttons[_presses.First()].OnInteract();
                yield return new WaitForSeconds(.4f);
            }

            foreach (var index in indexes)
            {
                _buttons[index].OnInteract();
                yield return new WaitForSeconds(.25f);
            }
        }
    }
}