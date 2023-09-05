using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Variety
{
    public class DigitDisplay : Item
    {
        public override string TwitchHelpMessage => "!{0} digit 0 [set the digit display]";

        public int TopLeftCell { get; private set; }
        public override int NumStates => _numStates;

        private int _numStates;
        private int _curDisplay;
        private bool _curYellow;
        private readonly int[] _displayedDigitPerState;
        private DigitDisplayPrefab _prefab;

        private static readonly string[] _segmentMap = new[] { "0000000", "1111101", "1001000", "0111011", "1011011", "1001110", "1010111", "1110111", "1001001", "1111111", "1011111" };

        public DigitDisplay(VarietyModule module, int topLeftCell, System.Random rnd)
            : base(module, CellRect(topLeftCell, 2, 3))
        {
            TopLeftCell = topLeftCell;
            SetState(-1, automatic: true);
            _curDisplay = -1;
            _curYellow = false;
            _displayedDigitPerState = Enumerable.Range(0, 9).ToArray().Shuffle(rnd);
        }

        public override bool DecideStates(int numPriorNonWireItems)
        {
            if (numPriorNonWireItems < 2 || numPriorNonWireItems > 10)
                return false;
            _numStates = numPriorNonWireItems;
            return true;
        }

        public override object Flavor => "DigitDisplay";
        public override string ToString() => "digit display";
        public override string DescribeSolutionState(int state) => $"set the digit display to {_displayedDigitPerState[state]} (the digit at stage {state})";
        public override string DescribeWhatUserDid() => "you changed the digit display";
        public override string DescribeWhatUserShouldHaveDone(int desiredState) => $"you should have changed the digit display to {_displayedDigitPerState[desiredState]} (instead of {(State == -1 ? "leaving it unchanged" : _displayedDigitPerState[State].ToString())})";

        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            _prefab = UnityEngine.Object.Instantiate(Module.DigitDisplayTemplate, Module.transform);
            _prefab.transform.localPosition = new Vector3(GetXOfCellRect(TopLeftCell, 2), .015f, GetYOfCellRect(TopLeftCell, 3));
            _prefab.transform.localRotation = Quaternion.identity;
            _prefab.transform.localScale = new Vector3(1f, 1f, 1f);
            _prefab.UpButton.OnInteract = ButtonPressHandler(_prefab.UpButtonParent, _prefab.UpButton, 1);
            _prefab.DownButton.OnInteract = ButtonPressHandler(_prefab.DownButtonParent, _prefab.DownButton, 9);

            foreach (var seg in _prefab.Segments)
                seg.sharedMaterial = _prefab.Black;

            yield return new ItemSelectable(_prefab.UpButton, Cells[0]);
            yield return new ItemSelectable(_prefab.DownButton, Cells[0] + 2 * W);

            SetDisplay(-1, yellow: false);
        }

        private KMSelectable.OnInteractHandler ButtonPressHandler(Transform buttonParent, KMSelectable button, int offset) => delegate
        {
            button.AddInteractionPunch(.25f);
            Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
            Module.MoveButton(buttonParent, .001f, ButtonMoveType.DownThenUp);
            SetDisplay((_curDisplay + offset) % 10, yellow: true);
            SetState(Array.IndexOf(_displayedDigitPerState, _curDisplay));
            return false;
        };

        private void SetDisplay(int value, bool yellow)
        {
            _curDisplay = value;
            _curYellow = yellow;
            for (var i = 0; i < _prefab.Segments.Length; i++)
                _prefab.Segments[i].sharedMaterial = _segmentMap[value + 1][i] == '0' ? _prefab.Black : yellow ? _prefab.Yellow : _prefab.Blue;
        }

        public override void ReceiveItemChange(int stageItemIndex)
        {
            SetDisplay(_displayedDigitPerState[stageItemIndex], yellow: false);
            SetState(-1, automatic: true);
        }

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, @"^\s*digit\s+(\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            int number;
            if (m.Success && int.TryParse(m.Groups[1].Value, out number) && number >= 0 && number <= 9)
                return TwitchSet(number).GetEnumerator();
            return null;
        }

        private IEnumerable<object> TwitchSet(int number)
        {
            while (_curDisplay != number || !_curYellow)
            {
                _prefab.UpButton.OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState) => TwitchSet(_displayedDigitPerState[desiredState]);
    }
}