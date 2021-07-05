using System.Collections;
using System.Collections.Generic;
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

        public Knob(VarietyModule module, int topLeftCell, int numTicks, char snFirstChar)
            : base(module, CellRect(topLeftCell, 4, 4))
        {
            TopLeftCell = topLeftCell;
            NumTicks = numTicks;
            Offset = (snFirstChar >= 'A' && snFirstChar <= 'Z' ? snFirstChar - 'A' + 1 : snFirstChar - '0') % NumTicks;
            State = Rnd.Range(0, NumTicks);
        }

        private void SetPosition(int pos)
        {
            State = pos;
            if (_turning != null)
                Module.StopCoroutine(_turning);
            _turning = Module.StartCoroutine(turnTo(pos));
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
            prefab.transform.localPosition = new Vector3(GetXOfCellRect(TopLeftCell, 3), .015f, GetYOfCellRect(TopLeftCell, 3));
            prefab.transform.localRotation = Quaternion.identity;
            prefab.transform.localScale = new Vector3(1, 1, 1);

            for (var i = 0; i < NumTicks; i++)
            {
                var tick = i == 0 ? prefab.TickTemplate : Object.Instantiate(prefab.TickTemplate, prefab.transform);
                tick.transform.localPosition = new Vector3(0, .0001f, 0);
                tick.transform.localEulerAngles = new Vector3(0, 360f * i / NumTicks, 0);
                tick.transform.localScale = new Vector3(1, 1, 1);
            }

            _knob = prefab.Knob.transform;
            _knob.localRotation = Quaternion.Euler(0, 360f * (State + Offset) / NumTicks, 0);
            prefab.Knob.OnInteract = delegate { SetPosition((State + 1) % NumTicks); return false; };
            yield return new ItemSelectable(prefab.Knob, TopLeftCell);
        }

        public override string ToString() { return string.Format("Knob with {0} ticks at {1}", NumTicks, coords(Cells[0])); }
        public override int NumStates { get { return NumTicks; } }
        public override object Flavor { get { return "Knob"; } }
        public override string DescribeSolutionState(int state) { return string.Format("set the knob (which has {1} ticks and where North is {2}) to {0}", state, NumTicks, (NumTicks - Offset) % NumTicks); }
        public override string DescribeWhatUserDid() { return "you twisted the knob"; }
        public override string DescribeWhatUserShouldHaveDone(int desiredState) { return string.Format("you should have set the knob to {0} (instead of {1})", desiredState, State); }
    }
}