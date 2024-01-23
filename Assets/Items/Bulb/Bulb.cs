using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Variety
{
    public class Bulb : Item
    {
        public override string TwitchHelpMessage => "!{0} red bulb ..- [transmit ..- on the red bulb] | !{0} red bulb reset [show flashing code again]";

        public override void SetColorblind(bool on)
        {
            ColorblindText.gameObject.SetActive(on);
            ColorblindText.text = _colorblindNames[(int) Color];
        }
        private TextMesh _colorblindText;
        private TextMesh ColorblindText
        {
            get
            {
                if (_colorblindText == null)
                    _colorblindText = _prefab.GetComponentInChildren<TextMesh>(true);
                return _colorblindText;
            }
        }

        public int TopLeftCell { get; private set; }
        public BulbColor Color { get; private set; }
        public int N { get; private set; }

        public Bulb(VarietyModule module, int topLeftCell, BulbColor color, char baseLetter, int n)
            : base(module, CellRect(topLeftCell, 2, 2))
        {
            TopLeftCell = topLeftCell;
            Color = color;
            N = n;
            SetState(-1, automatic: true);
            _baseLetter = baseLetter;
        }

        private enum BulbState
        {
            Flashing,
            Inputting,
            Echoing
        }

        private BulbState _cyclingState = BulbState.Flashing;
        private KMSelectable _button;
        private BulbPrefab _prefab;
        private Coroutine _morseCycle = null;
        private string _inputs;
        private char _baseLetter;

        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            _prefab = UnityEngine.Object.Instantiate(Module.BulbTemplate, Module.transform);
            _prefab.transform.localPosition = new Vector3(GetXOfCellRect(Cells[0], 2), .015f, GetYOfCellRect(Cells[0], 2));
            var r = UnityEngine.Random.Range(0f, 360f);
            _prefab.transform.localRotation = Quaternion.Euler(0f, r, 0f);
            ColorblindText.transform.localRotation = Quaternion.Euler(90f, -r, 0f);
            _prefab.transform.localScale = new Vector3(2, 2, 2);

            var t = Time.time + UnityEngine.Random.Range(0f, 2f);
            Module.StartCoroutine(Delay(() => Time.time < t, () => false, () => _morseCycle = Module.StartCoroutine(CycleMorse(Morse(N.ToString())))));
            _button = _prefab.Selectable;

            var lastTime = -1f;
            var held = false;
            var delays = new List<float>();
            Coroutine wait = null;
            _button.OnInteract = delegate
            {
                if (_morseCycle != null)
                    Module.StopCoroutine(_morseCycle);
                switch (_cyclingState)
                {
                    case BulbState.Flashing:
                        _cyclingState = BulbState.Inputting;
                        lastTime = Time.time;
                        held = true;
                        if (wait != null)
                            Module.StopCoroutine(wait);
                        _prefab.Bulb.sharedMaterial = _prefab.BulbColors[(int) Color + 2];
                        break;
                    case BulbState.Inputting:
                        delays.Add(Time.time - lastTime);
                        lastTime = Time.time;
                        held = true;
                        if (wait != null)
                            Module.StopCoroutine(wait);
                        _prefab.Bulb.sharedMaterial = _prefab.BulbColors[(int) Color + 2];
                        break;
                    case BulbState.Echoing:
                        _cyclingState = BulbState.Flashing;
                        SetState(-1);
                        break;
                }
                if (_cyclingState == BulbState.Flashing)
                    _morseCycle = Module.StartCoroutine(CycleMorse(Morse(N.ToString())));
                return false;
            };
            _button.OnInteractEnded = delegate
            {
                if (_cyclingState != BulbState.Inputting)
                    return;
                _prefab.Bulb.sharedMaterial = _prefab.BulbColors[(int) Color];
                delays.Add(Time.time - lastTime);
                lastTime = Time.time;
                held = false;
                wait = Module.StartCoroutine(Delay(() => Time.time - lastTime < 2f, () => held, () => { if (held) return; ProcessInput(delays); delays.Clear(); }));
            };
            yield return new ItemSelectable(_button, Cells[0]);
        }

        private void ProcessInput(List<float> delays)
        {
            if (delays.Count % 2 == 0 || delays.Count <= 0)
                throw new ArgumentException("This should be unreachable. " + delays.Join(", "));

            char letter;
            if (delays.Count == 1)
            {
                letter = delays[0] < 0.5 ? 'E' : 'T';
                _inputs = delays[0] < 0.5 ? "." : "-";
            }
            else
            {
                var total = 0f;
                for (var i = delays.Count - 2; i >= 0; i -= 2)
                    total += delays[i];
                var gapSize = total / ((delays.Count - 1) / 2);
                _inputs = "";
                for (var i = 0; i < delays.Count; i += 2)
                    _inputs += delays[i] > 2 * gapSize ? "-" : ".";
                letter = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".FirstOrDefault(c => Morse(c) == _inputs);
            }

            var state = letter - _baseLetter;
            if (state >= N || state < 0)
                state = -1;
            SetState(state);
            _cyclingState = BulbState.Echoing;
            _morseCycle = Module.StartCoroutine(CycleMorse(_inputs));
        }

        IEnumerator Delay(Func<bool> check, Func<bool> stop, Action callback)
        {
            while (check())
            {
                if (stop())
                    yield break;
                yield return null;
            }
            if (stop())
                yield break;
            callback();
        }

        private static string Morse(string v) => v.ToUpperInvariant().Select(Morse).Join(" ");

        private static string Morse(char v)
        {
            switch (v)
            {
                case 'A': return ".-";
                case 'B': return "-...";
                case 'C': return "-.-.";
                case 'D': return "-..";
                case 'E': return ".";
                case 'F': return "..-.";
                case 'G': return "--.";
                case 'H': return "....";
                case 'I': return "..";
                case 'J': return ".---";
                case 'K': return "-.-";
                case 'L': return ".-..";
                case 'M': return "--";
                case 'N': return "-.";
                case 'O': return "---";
                case 'P': return ".--.";
                case 'Q': return "--.-";
                case 'R': return ".-.";
                case 'S': return "...";
                case 'T': return "-";
                case 'U': return "..-";
                case 'V': return "...-";
                case 'W': return ".--";
                case 'X': return "-..-";
                case 'Y': return "-.--";
                case 'Z': return "--..";
                case '0': return "-----";
                case '1': return ".----";
                case '2': return "..---";
                case '3': return "...--";
                case '4': return "....-";
                case '5': return ".....";
                case '6': return "-....";
                case '7': return "--...";
                case '8': return "---..";
                case '9': return "----.";
            }
            return "";
        }

        private IEnumerator CycleMorse(string morse)
        {
            if (morse.Length == 0)
            {
                _prefab.Bulb.sharedMaterial = _prefab.BulbColors[(int) Color];
                yield break;
            }
            var i = 0;
            while (true)
            {
                _prefab.Bulb.sharedMaterial = _prefab.BulbColors[(int) Color + (morse[i] != ' ' ? 2 : 0)];
                yield return new WaitForSeconds(morse[i] == '.' ? 0.3f : 0.9f);
                _prefab.Bulb.sharedMaterial = _prefab.BulbColors[(int) Color];
                yield return new WaitForSeconds(.3f);
                i = (i + 1) % morse.Length;
                if (i == 0)
                    yield return new WaitForSeconds(1.8f);
            }
        }

        private static readonly string[] _colorNames = { "red", "yellow" };
        private static readonly string[] _colorblindNames = { "R", "Y" };

        public override string ToString() => $"{_colorNames[(int) Color]} bulb flashing {Morse(N.ToString())} ({N})";
        public override int NumStates => N;
        public override object Flavor => Color;
        public override string DescribeSolutionState(int state) => $"transmit {Morse((char) (_baseLetter + state))} ({(char) (_baseLetter + state)}) on the {_colorNames[(int) Color]} bulb";
        public override string DescribeWhatUserDid() => $"you transmitted {_inputs} ({(State == -1 ? "invalid" : $"{(char) (_baseLetter + State)}")}) on the {_colorNames[(int) Color]} bulb";
        public override string DescribeWhatUserShouldHaveDone(int desiredState) =>
            $"you should have transmitted {Morse((char) (_baseLetter + desiredState))} ({(char) (_baseLetter + desiredState)}) on the {_colorNames[(int) Color]} bulb ({(_cyclingState == BulbState.Flashing ? "you left it transmitting the digits" : $"instead of {_inputs}")})";

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, $@"^\s*{_colorNames[(int) Color]}\s+bulb\s+reset\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (m.Success)
                return TwitchReset().GetEnumerator();

            m = Regex.Match(command, $@"^\s*{_colorNames[(int) Color]}\s+bulb\s+([.-]{{1,4}})\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (m.Success)
                return TwitchSet(m.Groups[1].Value).GetEnumerator();

            return null;
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState)
        {
            foreach (var e in TwitchSet(Morse((char) (_baseLetter + desiredState)), true))
                yield return e;
            while (_cyclingState == BulbState.Inputting)
                yield return true;
        }

        private IEnumerable<object> TwitchSet(string mc, bool forceSolve = false)
        {
            if (_cyclingState == BulbState.Echoing)
            {
                _button.OnInteract();
                yield return new WaitForSeconds(.1f);
                _button.OnInteractEnded();
                yield return new WaitForSeconds(.1f);
            }

            if (_cyclingState == BulbState.Inputting)
            {
                while (_cyclingState == BulbState.Inputting)
                    yield return forceSolve ? (object) true : "trycancel";
                yield return new WaitForSeconds(.1f);
                _button.OnInteract();
                yield return new WaitForSeconds(.1f);
                _button.OnInteractEnded();
                yield return new WaitForSeconds(.1f);
            }

            foreach (var c in mc)
            {
                _button.OnInteract();
                yield return new WaitForSeconds(c == '.' ? .3f : .9f);
                _button.OnInteractEnded();
                yield return new WaitForSeconds(.3f);
            }
        }

        private IEnumerable<object> TwitchReset()
        {
            if (_cyclingState == BulbState.Flashing)
                yield break;
            if (_cyclingState == BulbState.Inputting)
            {
                while (_cyclingState == BulbState.Inputting)
                    yield return true;
                yield return new WaitForSeconds(.1f);
            }
            _button.OnInteract();
            yield return new WaitForSeconds(.1f);
            _button.OnInteractEnded();
            yield return new WaitForSeconds(.1f);
        }
    }
}