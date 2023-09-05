using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Variety
{
    public class Led : Item
    {
        public override string TwitchHelpMessage => "!{0} led white [set the LED to white] | !{0} led reset [show flashing colors again]";

        private bool _colorblind;
        public override void SetColorblind(bool on)
        {
            _colorblind = on;
            ColorblindText.gameObject.SetActive(on);
            ColorblindText.text = _colorblindNames[(int) _curShownColor];
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
        public LedColor Color1 { get; private set; }
        public LedColor Color2 { get; private set; }
        public LedColor[] Answers { get; private set; }

        public Led(VarietyModule module, int topLeftCell, LedColor color1, LedColor color2, LedColor[] answers)
            : base(module, CellRect(topLeftCell, 2, 2))
        {
            TopLeftCell = topLeftCell;
            Color1 = color1;
            Color2 = color2;
            Answers = answers;
            SetState(-1, automatic: true);
        }

        private LedCyclingState _cyclingState = LedCyclingState.TableColors;
        private LedColor _curShownColor;
        private KMSelectable _led;
        private LedPrefab _prefab;

        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            _prefab = UnityEngine.Object.Instantiate(Module.LedTemplate, Module.transform);
            _prefab.transform.localPosition = new Vector3(GetXOfCellRect(Cells[0], 2), .015f, GetYOfCellRect(Cells[0], 2));
            _prefab.transform.localRotation = Quaternion.identity;
            _prefab.transform.localScale = new Vector3(2, 2, 2);

            var coroutine = Module.StartCoroutine(CycleLed(_prefab.Led, _prefab.LedColors));
            _led = _prefab.Selectable;

            _led.OnInteract = delegate
            {
                if (coroutine != null)
                    Module.StopCoroutine(coroutine);
                switch (_cyclingState)
                {
                    case LedCyclingState.TableColors:
                        _cyclingState = LedCyclingState.PossibleColors;
                        SetState(-1);
                        break;

                    case LedCyclingState.PossibleColors:
                        _cyclingState = LedCyclingState.SetColor;
                        SetState(Array.IndexOf(Answers, _curShownColor));
                        break;

                    case LedCyclingState.SetColor:
                        _cyclingState = LedCyclingState.TableColors;
                        SetState(-1);
                        break;
                }
                coroutine = _cyclingState == LedCyclingState.SetColor ? null : Module.StartCoroutine(CycleLed(_prefab.Led, _prefab.LedColors));
                return false;
            };
            yield return new ItemSelectable(_led, Cells[0]);
        }

        private IEnumerator CycleLed(MeshRenderer led, Material[] ledColors)
        {
            var i = 0;
            var n = _cyclingState == LedCyclingState.TableColors ? 2 : ledColors.Length;
            while (true)
            {
                i = (i + 1) % n;
                _curShownColor = _cyclingState == LedCyclingState.TableColors ? (i == 0 ? Color1 : Color2) : (LedColor) i;
                led.sharedMaterial = ledColors[(int) _curShownColor];
                if (_colorblind)
                    ColorblindText.text = _colorblindNames[(int) _curShownColor];
                yield return new WaitForSeconds(.7f);
            }
        }

        private static readonly string[] _colorNames = { "black", "red", "yellow", "blue", "white" };
        private static readonly string[] _colorblindNames = { "", "R", "Y", "B", "W" };

        public override string ToString() => $"LED flashing {_colorNames[(int) Color1]} and {_colorNames[(int) Color2]}";
        public override int NumStates => Answers.Length;
        public override object Flavor => "LED";
        public override string DescribeSolutionState(int state) => $"set the LED to {_colorNames[(int) Answers[state]]}";
        public override string DescribeWhatUserDid() => "you set the LED to a color";
        public override string DescribeWhatUserShouldHaveDone(int desiredState) => $"you should have set the LED to {_colorNames[(int) Answers[desiredState]]} ({(State == -1 ? "you left it cycling" : $"instead of {_colorNames[(int) _curShownColor]}")})";

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, @"^\s*led\s+reset\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (m.Success)
                return TwitchReset().GetEnumerator();

            m = Regex.Match(command, $@"^\s*led\s+({_colorNames.Join("|")})\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (m.Success)
                return TwitchSet((LedColor) _colorNames.IndexOf(str => str.Equals(m.Groups[1].Value, StringComparison.InvariantCultureIgnoreCase))).GetEnumerator();

            return null;
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState) => TwitchSet(Answers[desiredState]);

        private IEnumerable<object> TwitchSet(LedColor ledColor)
        {
            if (_cyclingState == LedCyclingState.SetColor)
            {
                _led.OnInteract();
                yield return new WaitForSeconds(.1f);
            }

            if (_cyclingState == LedCyclingState.TableColors)
            {
                _led.OnInteract();
                yield return new WaitForSeconds(.1f);
            }

            while (_curShownColor != ledColor)
                yield return true;

            _led.OnInteract();
            yield return new WaitForSeconds(.1f);
        }

        private IEnumerable<object> TwitchReset()
        {
            if (_cyclingState != LedCyclingState.SetColor)
                yield break;
            _led.OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }
}