using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Variety
{
    public class Keypad : Item
    {
        public override string TwitchHelpMessage => "!{0} 1x3 keys 012 [press keys on the 1×3 white keypad in that order]";

        public override void SetColorblind(bool on)
        {
            foreach (var text in _prefab.GetComponentsInChildren<TextMesh>(true))
                text.gameObject.SetActive(on);
        }

        public static readonly Dictionary<KeypadSize, int> Widths = new Dictionary<KeypadSize, int>
        {
            { KeypadSize.Keypad2x2, 2 },
            { KeypadSize.Keypad1x3, 1 },
            { KeypadSize.Keypad1x4, 1 },
            { KeypadSize.Keypad3x1, 3 },
            { KeypadSize.Keypad4x1, 4 }
        };

        public static readonly Dictionary<KeypadSize, int> Heights = new Dictionary<KeypadSize, int>
        {
            { KeypadSize.Keypad2x2, 2 },
            { KeypadSize.Keypad1x3, 3 },
            { KeypadSize.Keypad1x4, 4 },
            { KeypadSize.Keypad3x1, 1 },
            { KeypadSize.Keypad4x1, 1 }
        };

        public KeypadSize Size { get; private set; }
        private int numKeys => Widths[Size] * Heights[Size];

        private readonly List<int> _presses = new List<int>();
        private MeshRenderer[] _leds;
        private KeypadPrefab _prefab;
        private KMSelectable[] _buttons;
        private Transform[] _buttonParents;

        public Keypad(VarietyModule module, KeypadSize size, int topLeftCell)
            : base(module, CellRect(topLeftCell, 2 * Widths[size], 2 * Heights[size]))
        {
            Size = size;
            SetState(-1, automatic: true);
        }

        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            var w = Widths[Size];
            var h = Heights[Size];

            const float d = .0225f;
            const float s = .033f;

            _prefab = Object.Instantiate(Module.KeypadTemplate, Module.transform);
            _prefab.transform.localPosition = new Vector3(GetX(Cells[0]) + VarietyModule.CellWidth * (2 * w - 1) * .5f, .015f, GetY(Cells[0]) - VarietyModule.CellHeight * (2 * h - 1) * .5f);
            _prefab.transform.localRotation = Quaternion.identity;
            _prefab.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
            _leds = new MeshRenderer[w * h];

            _prefab.Backing.localScale = new Vector3(d * w + .001f, d * h + .001f);
            _buttonParents = new Transform[w * h];
            _buttons = new KMSelectable[w * h];

            for (var keyIx = 0; keyIx < w * h; keyIx++)
            {
                _buttons[keyIx] = keyIx == 0 ? _prefab.KeyTemplate : Object.Instantiate(_prefab.KeyTemplate, _prefab.transform);
                _buttons[keyIx].name = $"Key {keyIx + 1}";
                _buttons[keyIx].transform.localPosition = new Vector3(d * (keyIx % w - (w - 1) * .5f), 0, d * ((h - 1 - keyIx / w) - (h - 1) * .5f));
                _buttons[keyIx].transform.localRotation = Quaternion.identity;
                _buttons[keyIx].transform.localScale = new Vector3(s, s, s);
                _buttons[keyIx].OnInteract = pressed(keyIx);
                _buttonParents[keyIx] = _buttons[keyIx].transform.Find("KeyCapParent");
                _leds[keyIx] = _buttonParents[keyIx].Find("Led").GetComponent<MeshRenderer>();

                yield return new ItemSelectable(_buttons[keyIx], Cells[0] + 2 * (keyIx % w) + W * 2 * (keyIx / w));
            }
        }

        private KMSelectable.OnInteractHandler pressed(int keyIx) => delegate
        {
            _buttons[keyIx].AddInteractionPunch(.25f);
            Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, _buttons[keyIx].transform);
            Module.MoveButton(_buttonParents[keyIx], .1f, ButtonMoveType.DownThenUp);

            if (_presses.Contains(keyIx))
                _presses.Clear();
            else
                _presses.Add(keyIx);

            for (var i = 0; i < numKeys; i++)
                _leds[i].sharedMaterial = _prefab.LedColors[_presses.IndexOf(i) + 1];

            if (_presses.Count == numKeys)
            {
                var newState = 0;
                var m = 1;
                var list = Enumerable.Range(0, numKeys).ToList();
                var ix = 0;
                while (list.Count > 0)
                {
                    var lIx = list.IndexOf(_presses[ix++]);
                    newState += m * lIx;
                    m *= list.Count;
                    list.RemoveAt(lIx);
                }
                SetState(newState);
            }
            else
                SetState(-1);
            return false;
        };

        public override string ToString() => $"{Widths[Size]}×{Heights[Size]} white keypad";
        public override int NumStates => factorial(numKeys);
        public override object Flavor => Size;

        private int factorial(int n) => n < 2 ? 1 : n * factorial(n - 1);

        public override string DescribeSolutionState(int state) => $"press the keys on the {Widths[Size]}×{Heights[Size]}{(Widths[Size] > Heights[Size] ? " (wide)" : Widths[Size] < Heights[Size] ? " (tall)" : "")} white keypad in the order {getSequence(state).Join(", ")}";
        public override string DescribeWhatUserDid() => $"you pressed keys on the {Widths[Size]}×{Heights[Size]}{(Widths[Size] > Heights[Size] ? " (wide)" : Widths[Size] < Heights[Size] ? " (tall)" : "")} white keypad";
        public override string DescribeWhatUserShouldHaveDone(int desiredState) => $"you should have pressed the keys on the {Widths[Size]}×{Heights[Size]} white keypad in the order {getSequence(desiredState).Join(", ")} ({(State == -1 ? "you left it unfinished" : $"instead of {getSequence(State).Join(", ")}")})";

        private int[] getSequence(int state)
        {
            var list = Enumerable.Range(0, numKeys).ToList();
            var answer = new int[list.Count];
            var ix = 0;
            while (list.Count > 0)
            {
                var nx = state % list.Count;
                answer[ix++] = list[nx];
                state /= list.Count;
                list.RemoveAt(nx);
            }
            return answer;
        }

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, $@"^\s*{Widths[Size]}[x×]{Heights[Size]}\s+keys\s+(\d+)\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
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
                _buttons[_presses[0]].OnInteract();
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