using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Variety
{
    public class Key : Item
    {
        public override string TwitchHelpMessage { get { return "!{0} key 0 [turn the key-in-lock at last timer digit]"; } }

        public Key(VarietyModule module, int cell) : base(module, new[] { cell, cell + 1, cell + W, cell + W + 1 })
        {
            TopLeftCell = cell;
            Turned = false;
            SetState(-1, automatic: true);
        }

        public int TopLeftCell { get; private set; }
        public bool Turned { get; private set; }

        private Transform _core;
        private KMSelectable _key;
        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            var prefab = Object.Instantiate(Module.KeyTemplate, Module.transform);
            _core = prefab.Core;
            _key = prefab.Key;
            prefab.transform.localPosition = new Vector3(GetX(TopLeftCell) + VarietyModule.CellWidth / 2, .015f, GetY(TopLeftCell) - VarietyModule.CellHeight / 2);
            yield return new ItemSelectable(_key, Cells[0]);
            _key.OnInteract = TurnKey;
        }

        private bool TurnKey()
        {
            _key.AddInteractionPunch(.5f);
            Module.Audio.PlaySoundAtTransform(Turned ? "KeySound2" : "KeySound1", _key.transform);

            Turned = !Turned;
            SetState(Turned ? (int) Module.Bomb.GetTime() % 10 : -1);
            Module.StartCoroutine(KeyTurnAnimation(Turned));
            return false;
        }

        private IEnumerator KeyTurnAnimation(bool forwards)
        {
            var elapsed = 0f;
            var duration = .16f;

            while (elapsed < duration)
            {
                _core.transform.localEulerAngles = new Vector3(0, Mathf.Lerp(forwards ? 0 : 60, forwards ? 60 : 0, elapsed / duration), 0);
                yield return null;
                elapsed += Time.deltaTime;
            }
            _core.transform.localEulerAngles = new Vector3(0, forwards ? 60 : 0, 0);
        }

        public override string ToString() { return "key-in-lock"; }
        public override int NumStates { get { return 10; } }
        public override object Flavor { get { return "Key"; } }
        public override string DescribeSolutionState(int state) { return string.Format("turn the key when the last digit of the timer is {0}", state); }
        public override string DescribeWhatUserDid() { return "you turned the key"; }
        public override string DescribeWhatUserShouldHaveDone(int desiredState) { return string.Format("you should have turned the key when the last digit on the timer was {0} ({1})", desiredState, State == -1 ? "you left it unturned" : string.Format("instead of {0}", State)); }

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, @"^\s*key\s+(\d+)\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            int val;
            if (!m.Success || !int.TryParse(m.Groups[1].Value, out val) || val < 0 || val >= 10)
                return null;
            return TwitchSetTo(val).GetEnumerator();
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState)
        {
            return TwitchSetTo(desiredState);
        }

        private IEnumerable<object> TwitchSetTo(int val)
        {
            if (Turned)
            {
                _key.OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            while ((int) Module.Bomb.GetTime() % 10 != val)
                yield return true;
            _key.OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }
}