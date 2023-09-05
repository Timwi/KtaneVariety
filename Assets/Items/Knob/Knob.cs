using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;

namespace Variety
{
    public class Knob : Item
    {
        public override string TwitchHelpMessage => "!{0} knob 0 [turn the white knob to that many tickmarks from north]";

        public int NumTicks { get; private set; }
        public int Offset { get; private set; }

        private Coroutine _turning;
        private KMSelectable _knob;

        public Knob(VarietyModule module, int topLeftCell, int numTicks, System.Random rnd)
            : base(module, CellRect(topLeftCell, 4, 4))
        {
            NumTicks = numTicks;
            SetState(rnd.Next(0, NumTicks), automatic: true);
        }

        private void SetPosition(int pos)
        {
            SetState(pos);
            if (_turning != null)
                Module.StopCoroutine(_turning);
            _turning = Module.StartCoroutine(turnTo(pos));
        }

        public override bool DecideStates(int numPriorNonWireItems)
        {
            var snFirstChar = Module.Bomb.GetSerialNumber()[0];
            Offset = (snFirstChar >= 'A' && snFirstChar <= 'Z' ? snFirstChar - 'A' + 1 : snFirstChar - '0') % NumTicks;
            _knob.transform.localRotation = Quaternion.Euler(0, 360f * (State + Offset) / NumTicks, 0);
            return true;
        }

        private IEnumerator turnTo(int pos)
        {
            var oldRot = _knob.transform.localRotation;
            var newRot = Quaternion.Euler(0, 360f * (pos + Offset) / NumTicks, 0);
            var duration = .2f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                _knob.transform.localRotation = Quaternion.Slerp(oldRot, newRot, Easing.InOutQuad(elapsed, 0, 1, duration));
                yield return null;
                elapsed += Time.deltaTime;
            }
            _knob.transform.localRotation = newRot;
            _turning = null;
        }

        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            var prefab = Object.Instantiate(Module.KnobTemplate, Module.transform);
            prefab.transform.localPosition = new Vector3(GetXOfCellRect(Cells[0], 3), .01501f, GetYOfCellRect(Cells[0], 3));
            prefab.transform.localRotation = Quaternion.identity;
            prefab.transform.localScale = new Vector3(1.09f, 1.09f, 1.09f);

            for (var i = 0; i < NumTicks; i++)
            {
                var tick = i == 0 ? prefab.TickTemplate : Object.Instantiate(prefab.TickTemplate, prefab.transform);
                tick.transform.localPosition = new Vector3(0, .0002f, 0);
                tick.transform.localEulerAngles = new Vector3(0, 360f * i / NumTicks, 0);
                tick.transform.localScale = new Vector3(1, 1, 1);
            }

            _knob = prefab.Knob;
            _knob.OnInteract = delegate
            {
                prefab.Knob.AddInteractionPunch(.25f);
                Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, prefab.Knob.transform);
                SetPosition((State + 1) % NumTicks);
                return false;
            };
            yield return new ItemSelectable(prefab.Knob, Cells[0] + W + 1);
        }

        public override string ToString() => $"white knob (north is {(NumTicks - Offset) % NumTicks})";
        public override int NumStates => NumTicks;
        public override object Flavor => "Knob";
        public override string DescribeSolutionState(int state) => $"set the white knob to {state}";
        public override string DescribeWhatUserDid() => "you twisted the white knob";
        public override string DescribeWhatUserShouldHaveDone(int desiredState) => $"you should have set the white knob to {desiredState} (instead of {State})";

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, @"^\s*knob\s+(\d+)\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            int val;
            if (!m.Success || !int.TryParse(m.Groups[1].Value, out val) || val < 0 || val >= NumTicks)
                return null;
            return TwitchPress((NumTicks - Offset + val) % NumTicks).GetEnumerator();
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState) => TwitchPress(desiredState);

        private IEnumerable<object> TwitchPress(int val)
        {
            while (State != val)
            {
                _knob.OnInteract();
                while (_turning != null)
                    yield return true;
                yield return new WaitForSeconds(.1f);
            }
        }
    }
}