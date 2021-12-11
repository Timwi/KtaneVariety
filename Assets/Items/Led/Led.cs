using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Variety
{
    public class Led : Item
    {
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

        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            var prefab = UnityEngine.Object.Instantiate(Module.LedTemplate, Module.transform);
            prefab.transform.localPosition = new Vector3(GetXOfCellRect(Cells[0], 2), .015f, GetYOfCellRect(Cells[0], 2));
            prefab.transform.localRotation = Quaternion.identity;
            prefab.transform.localScale = new Vector3(2, 2, 2);

            var coroutine = Module.StartCoroutine(CycleLed(prefab.Led, prefab.LedColors));
            _led = prefab.Selectable;

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
                coroutine = _cyclingState == LedCyclingState.SetColor ? null : Module.StartCoroutine(CycleLed(prefab.Led, prefab.LedColors));
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
                yield return new WaitForSeconds(.7f);
            }
        }

        private static readonly string[] _colorNames = { "black", "red", "yellow", "blue", "white" };

        public override string ToString() { return string.Format("LED flashing {0} and {1}", _colorNames[(int) Color1], _colorNames[(int) Color2]); }
        public override int NumStates { get { return Answers.Length; } }
        public override object Flavor { get { return "LED"; } }
        public override string DescribeSolutionState(int state) { return string.Format("set the LED to {0}", _colorNames[(int) Answers[state]]); }
        public override string DescribeWhatUserDid() { return "you set the LED to a color"; }
        public override string DescribeWhatUserShouldHaveDone(int desiredState) { return string.Format("you should have set the LED to {0} ({1})", _colorNames[(int) Answers[desiredState]], State == -1 ? "you left it cycling" : "instead of " + _colorNames[(int) _curShownColor]); }

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, @"^\s*led\s+reset\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (m.Success)
                return TwitchReset().GetEnumerator();

            m = Regex.Match(command, string.Format(@"^\s*led\s+({0})\s*$", _colorNames.Join("|")), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (m.Success)
                return TwitchSet((LedColor) _colorNames.IndexOf(str => str.Equals(m.Groups[1].Value, StringComparison.InvariantCultureIgnoreCase))).GetEnumerator();

            return null;
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState)
        {
            return TwitchSet(Answers[desiredState]);
        }

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