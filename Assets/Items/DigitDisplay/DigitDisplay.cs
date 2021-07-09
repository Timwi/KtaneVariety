using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Variety
{
    public class DigitDisplay : Item
    {
        public int TopLeftCell { get; private set; }
        public override int NumStates { get { return _numStates; } }

        private int _numStates;
        private int _curDisplay;
        private int[] _displayedDigitPerState;
        private DigitDisplayPrefab _prefab;

        private static readonly string[] _segmentMap = new[] { "0000000", "1111101", "1001000", "0111011", "1011011", "1001110", "1010111", "1110111", "1001001", "1111111", "1011111" };

        public DigitDisplay(VarietyModule module, int topLeftCell)
            : base(module, CellRect(topLeftCell, 2, 3))
        {
            TopLeftCell = topLeftCell;
            State = -1;
            _curDisplay = -1;
        }

        public override bool DecideStates(int numPriorNonWireItems)
        {
            if (numPriorNonWireItems < 2 || numPriorNonWireItems > 10)
                return false;
            _numStates = numPriorNonWireItems;
            _displayedDigitPerState = Enumerable.Range(0, 9).ToArray().Shuffle();
            return true;
        }

        public override object Flavor { get { return "DigitDisplay"; } }
        public override string ToString() { return "digit display"; }
        public override string DescribeSolutionState(int state) { return string.Format("set the digit display to {0} (the digit at stage {1})", _displayedDigitPerState[state], state); }
        public override string DescribeWhatUserDid() { return "you changed the digit display"; }
        public override string DescribeWhatUserShouldHaveDone(int desiredState) { return string.Format("you should have changed the digit display to {0} (instead of {1})", _displayedDigitPerState[desiredState], State == -1 ? "leaving it unchanged" : _displayedDigitPerState[State].ToString()); }

        public override IEnumerable<ItemSelectable> SetUp()
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

        private KMSelectable.OnInteractHandler ButtonPressHandler(Transform buttonParent, KMSelectable button, int offset)
        {
            return delegate
            {
                button.AddInteractionPunch(.25f);
                Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
                Module.MoveButton(buttonParent, .001f, ButtonMoveType.DownThenUp);
                SetDisplay((_curDisplay + offset) % 10, yellow: true);
                State = Array.IndexOf(_displayedDigitPerState, _curDisplay);
                return false;
            };
        }

        private void SetDisplay(int value, bool yellow)
        {
            _curDisplay = value;
            for (var i = 0; i < _prefab.Segments.Length; i++)
                _prefab.Segments[i].sharedMaterial = _segmentMap[value + 1][i] == '0' ? _prefab.Black : yellow ? _prefab.Yellow : _prefab.Blue;
        }

        public override void ReceiveItemChange(int stageItemIndex)
        {
            SetDisplay(_displayedDigitPerState[stageItemIndex], yellow: false);
            State = -1;
        }
    }
}