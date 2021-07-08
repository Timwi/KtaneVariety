using System.Collections;
using System.Collections.Generic;
using KModkit;
using UnityEngine;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class Knob : Item
    {
        public int TopLeftCell { get; private set; }
        public int NumTicks { get; private set; }
        public int Offset { get; private set; }

        private Coroutine _turning;
        private Transform _knob;

        public Knob(VarietyModule module, int topLeftCell, int numTicks)
            : base(module, CellRect(topLeftCell, 4, 4))
        {
            TopLeftCell = topLeftCell;
            NumTicks = numTicks;
            State = Rnd.Range(0, NumTicks);
        }

        private void SetPosition(int pos)
        {
            State = pos;
            if (_turning != null)
                Module.StopCoroutine(_turning);
            _turning = Module.StartCoroutine(turnTo(pos));
        }

        public override bool DecideStates(int numPriorNonWireItems)
        {
            var snFirstChar = Module.Bomb.GetSerialNumber()[0];
            Offset = (snFirstChar >= 'A' && snFirstChar <= 'Z' ? snFirstChar - 'A' + 1 : snFirstChar - '0') % NumTicks;
            _knob.localRotation = Quaternion.Euler(0, 360f * (State + Offset) / NumTicks, 0);
            return true;
        }

        private IEnumerator turnTo(int pos)
        {
            var oldRot = _knob.localRotation;
            var newRot = Quaternion.Euler(0, 360f * (pos + Offset) / NumTicks, 0);
            var duration = .2f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                _knob.localRotation = Quaternion.Slerp(oldRot, newRot, Easing.InOutQuad(elapsed, 0, 1, duration));
                yield return null;
                elapsed += Time.deltaTime;
            }
            _knob.localRotation = newRot;
        }

        public override IEnumerable<ItemSelectable> SetUp()
        {
            var prefab = Object.Instantiate(Module.KnobTemplate, Module.transform);
            prefab.transform.localPosition = new Vector3(GetXOfCellRect(TopLeftCell, 3), .01501f, GetYOfCellRect(TopLeftCell, 3));
            prefab.transform.localRotation = Quaternion.identity;
            prefab.transform.localScale = new Vector3(1.09f, 1.09f, 1.09f);

            for (var i = 0; i < NumTicks; i++)
            {
                var tick = i == 0 ? prefab.TickTemplate : Object.Instantiate(prefab.TickTemplate, prefab.transform);
                tick.transform.localPosition = new Vector3(0, .0002f, 0);
                tick.transform.localEulerAngles = new Vector3(0, 360f * i / NumTicks, 0);
                tick.transform.localScale = new Vector3(1, 1, 1);
            }

            _knob = prefab.Knob.transform;
            prefab.Knob.OnInteract = delegate
            {
                prefab.Knob.AddInteractionPunch(.25f);
                Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, prefab.Knob.transform);
                SetPosition((State + 1) % NumTicks);
                return false;
            };
            yield return new ItemSelectable(prefab.Knob, TopLeftCell);
        }

        public override string ToString() { return string.Format("knob (north is {0})", (NumTicks - Offset) % NumTicks); }
        public override int NumStates { get { return NumTicks; } }
        public override object Flavor { get { return "Knob"; } }
        public override string DescribeSolutionState(int state) { return string.Format("set the knob to {0}", state, NumTicks, (NumTicks - Offset) % NumTicks); }
        public override string DescribeWhatUserDid() { return "you twisted the knob"; }
        public override string DescribeWhatUserShouldHaveDone(int desiredState) { return string.Format("you should have set the knob to {0} (instead of {1})", desiredState, State); }
    }
}