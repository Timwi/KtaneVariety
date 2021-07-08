using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Variety
{
    public class Keypad : Item
    {
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
        private int numKeys { get { return Widths[Size] * Heights[Size]; } }

        private readonly List<int> _presses = new List<int>();
        private MeshRenderer[] _leds;
        private KeypadPrefab _prefab;

        public Keypad(VarietyModule module, KeypadSize size, int topLeftCell)
            : base(module, CellRect(topLeftCell, 2 * Widths[size], 2 * Heights[size]))
        {
            Size = size;
            State = -1;
        }

        public override IEnumerable<ItemSelectable> SetUp()
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

            for (var keyIx = 0; keyIx < w * h; keyIx++)
            {
                var key = keyIx == 0 ? _prefab.KeyTemplate : Object.Instantiate(_prefab.KeyTemplate, _prefab.transform);
                key.name = string.Format("Key {0}", keyIx + 1);
                key.transform.localPosition = new Vector3(d * (keyIx % w - (w - 1) * .5f), 0, d * ((h - 1 - keyIx / w) - (h - 1) * .5f));
                key.transform.localRotation = Quaternion.identity;
                key.transform.localScale = new Vector3(s, s, s);
                key.OnInteract = pressed(key, keyIx);

                yield return new ItemSelectable(key, Cells[0] + 2 * (keyIx % w) + W * 2 * (keyIx / w));

                _leds[keyIx] = key.transform.Find("Led").GetComponent<MeshRenderer>();
            }
        }

        private KMSelectable.OnInteractHandler pressed(KMSelectable key, int keyIx)
        {
            return delegate
            {
                key.AddInteractionPunch(.25f);
                Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, key.transform);
                if (_presses.Contains(keyIx))
                    _presses.Clear();
                else
                    _presses.Add(keyIx);
                for (var i = 0; i < numKeys; i++)
                    _leds[i].sharedMaterial = _presses.Contains(i) ? _prefab.LedOn : _prefab.LedOff;
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
                    State = newState;
                }
                else
                    State = -1;
                return false;
            };
        }

        public override string ToString() { return string.Format("{0}×{1} keypad", Widths[Size], Heights[Size]); }
        public override int NumStates { get { return factorial(numKeys); } }
        public override object Flavor { get { return Size; } }

        private int factorial(int n) { return n < 2 ? 1 : n * factorial(n - 1); }

        public override string DescribeSolutionState(int state) { return string.Format("press the keys on the {0}×{1} keypad in the order {2}", Widths[Size], Heights[Size], getSequence(state).Join(", ")); }
        public override string DescribeWhatUserDid() { return string.Format("you pressed keys on the {0}×{1} keypad", Widths[Size], Heights[Size]); }
        public override string DescribeWhatUserShouldHaveDone(int desiredState) { return string.Format("you should have pressed the keys on the {0}×{1} keypad in the order {2} ({3})", Widths[Size], Heights[Size], getSequence(desiredState).Join(", "), State == -1 ? "you left it unfinished" : string.Format("instead of {0}", getSequence(State).Join(", "))); }

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
    }
}