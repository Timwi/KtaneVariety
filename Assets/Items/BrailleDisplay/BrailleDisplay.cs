using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;

namespace Variety
{
    public class BrailleDisplay : Item
    {
        public int TopLeftCell { get; private set; }
        public override int NumStates { get { return _snChars.Length; } }

        private int[] _snChars;
        private int _curDisplay;
        private BrailleDisplayPrefab _prefab;

        // Braille dots as bitfields, 0–9A–Z
        private static readonly int[] _braille = { 52, 2, 6, 18, 50, 34, 22, 54, 38, 20, 1, 3, 9, 25, 17, 11, 27, 19, 10, 26, 5, 7, 13, 29, 21, 15, 31, 23, 14, 30, 37, 39, 58, 45, 61, 53 };
        private static readonly string _chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public BrailleDisplay(VarietyModule module, int topLeftCell)
            : base(module, CellRect(topLeftCell, 2, 3))
        {
            TopLeftCell = topLeftCell;
            _curDisplay = 0;
        }

        public override bool DecideStates(int numPriorNonWireItems)
        {
            _snChars = Module.Bomb.GetSerialNumber().Distinct().Select(ch => ch < 'A' ? ch - '0' : ch - 'A' + 10).ToArray();
            return true;
        }

        public override IEnumerable<ItemSelectable> SetUp()
        {
            _prefab = UnityEngine.Object.Instantiate(Module.BrailleDisplayTemplate, Module.transform);
            _prefab.transform.localPosition = new Vector3(GetXOfCellRect(TopLeftCell, 2), .015f, GetYOfCellRect(TopLeftCell, 3));
            _prefab.transform.localRotation = Quaternion.identity;
            _prefab.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

            foreach (var dot in _prefab.Dots)
                dot.sharedMaterial = _prefab.DotOff;

            for (var i = 0; i < 6; i++)
            {
                _prefab.Selectables[i].OnInteract = DotPressed(i);
                yield return new ItemSelectable(_prefab.Selectables[i], Cells[0] + (i / 3) + W * (i % 3));
            }
        }

        private KMSelectable.OnInteractHandler DotPressed(int dotIx)
        {
            return delegate
            {
                _curDisplay ^= 1 << dotIx;
                for (var i = 0; i < 6; i++)
                    _prefab.Dots[i].sharedMaterial = (_curDisplay & (1 << i)) != 0 ? _prefab.DotOn : _prefab.DotOff;
                var charEntered = Array.IndexOf(_braille, _curDisplay);
                State = charEntered == -1 ? -1 : Array.IndexOf(_snChars, charEntered);
                return false;
            };
        }

        public override object Flavor { get { return "BrailleDisplay"; } }
        public override string ToString() { return "Braille display"; }
        public override string DescribeSolutionState(int state) { return string.Format("set the Braille display to {0}, i.e., {1}", _chars[_snChars[state]], Enumerable.Range(1, 6).Where(i => (_braille[_snChars[state]] & (1 << (i - 1))) != 0).Join("")); }
        public override string DescribeWhatUserDid() { return "you changed the Braille display"; }
        public override string DescribeWhatUserShouldHaveDone(int desiredState)
        {
            var curChar = Array.IndexOf(_braille, _curDisplay);
            return string.Format("you should have changed the Braille display to {0}, i.e., {1} (you set it to {2}, which is {3})",
                _chars[_snChars[desiredState]],
                Enumerable.Range(1, 6).Where(i => (_braille[_snChars[desiredState]] & (1 << (i - 1))) != 0).Join(""),
                Enumerable.Range(1, 6).Where(i => (_curDisplay & (1 << (i - 1))) != 0).Join(""),
                curChar == -1 ? "invalid" : _chars.Substring(curChar, 1));
        }

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, @"^\s*braille\s+(\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (m.Success && m.Groups[1].Value.All(ch => ch >= '1' && ch <= '6'))
                return TwitchSet(m.Groups[1].Value.Aggregate(0, (p, ch) => p | (1 << (ch - '1')))).GetEnumerator();
            return null;
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState)
        {
            return TwitchSet(_braille[_snChars[desiredState]]);
        }

        private IEnumerable<object> TwitchSet(int dots)
        {
            for (var i = 0; i < 6; i++)
            {
                if ((_curDisplay & (1 << i)) != (dots & (1 << i)))
                {
                    _prefab.Selectables[i].OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
            }
        }
    }
}