using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Variety
{
    public class LetterDisplay : Item
    {
        public int Location { get; private set; }
        public char[][] Letters { get; private set; }
        public string[] FormableWords { get; private set; }

        private static readonly string[] _segmentMapping = { "abfimgh", "adfhkmn", "abin", "adfkmn", "abghin", "abgih", "abinmh", "bighfm", "adkn", "fmn", "bigel", "bin", "ibcefm", "ibclmf", "abfimn", "biafgh", "abfilmn", "abfghil", "abghmn", "adk", "binmf", "bije", "bijlmf", "cejl", "cek", "aejn" };
        private readonly int[] _curPos = { 0, 0, 0 };
        private LetterDisplayPrefab _prefab;

        public LetterDisplay(VarietyModule module, int location, char[][] letters, string[] formableWords)
            : base(module, CellRect(location, 4, 3))
        {
            Location = location;
            Letters = letters;
            FormableWords = formableWords;
            SetState(Array.IndexOf(FormableWords, letters.Select(slot => slot[0]).Join("")), automatic: true);
        }

        private void ShowLetters()
        {
            for (var slot = 0; slot < 3; slot++)
                for (var sg = 0; sg < 14; sg++)
                    _prefab.Segments[slot][sg].sharedMaterial = _segmentMapping[Letters[slot][_curPos[slot]] - 'A'].Contains((char) ('a' + sg)) ? _prefab.SegmentOn : _prefab.SegmentOff;
        }

        public override IEnumerable<ItemSelectable> SetUp(System.Random rnd)
        {
            _prefab = UnityEngine.Object.Instantiate(Module.LetterDisplayTemplate, Module.transform);
            _prefab.transform.localPosition = new Vector3(GetXOfCellRect(Cells[0], 4), .015f, GetYOfCellRect(Cells[0], 3));
            for (var i = 0; i < 3; i++)
            {
                _prefab.DownButtons[i].OnInteract = ButtonPress(i);
                yield return new ItemSelectable(_prefab.DownButtons[i], Cells[0] + i + W * 2);
            }
            ShowLetters();
        }

        private KMSelectable.OnInteractHandler ButtonPress(int btn)
        {
            return delegate
            {
                _prefab.DownButtons[btn].AddInteractionPunch(.25f);
                Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, _prefab.DownButtons[btn].transform);
                Module.MoveButton(_prefab.DownButtonParents[btn], .001f, ButtonMoveType.DownThenUp);

                _curPos[btn] = (_curPos[btn] + 1) % Letters[btn].Length;
                ShowLetters();
                SetState(Array.IndexOf(FormableWords, Enumerable.Range(0, 3).Select(slot => Letters[slot][_curPos[slot]]).Join("")));
                return false;
            };
        }

        public override string ToString() { return string.Format("letter display which can spell {0}", FormableWords.Join(", ")); }
        public override int NumStates { get { return FormableWords.Length; } }
        public override object Flavor { get { return "LetterDisplay"; } }
        public override string DescribeSolutionState(int state) { return string.Format("set the letter display to {0}", FormableWords[state]); }
        public override string DescribeWhatUserDid() { return "you changed the letter display"; }
        public override string DescribeWhatUserShouldHaveDone(int desiredState) { return string.Format("you should have set the letter display to {0} (you set it to {1})", FormableWords[desiredState], State == -1 ? "an invalid word" : FormableWords[State]); }

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, @"^\s*letters\s+cycle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (m.Success)
                return TwitchCycle().GetEnumerator();

            m = Regex.Match(command, @"^\s*letters\s+([a-z]{3})\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (m.Success && m.Groups[1].Value.ToUpperInvariant().Select((ltr, ix) => Letters[ix].Contains(ltr)).All(b => b))
                return TwitchSetLetters(m.Groups[1].Value.ToUpperInvariant()).GetEnumerator();

            return null;
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState)
        {
            return TwitchSetLetters(FormableWords[desiredState]);
        }

        private IEnumerable<object> TwitchCycle()
        {
            for (var slotIx = 0; slotIx < 3; slotIx++)
            {
                for (var iter = 0; iter < 3; iter++)
                {
                    _prefab.DownButtons[slotIx].OnInteract();
                    yield return new WaitForSeconds(.7f);
                }
                yield return new WaitForSeconds(.4f);
            }
        }

        private IEnumerable<object> TwitchSetLetters(string value)
        {
            for (var slotIx = 0; slotIx < 3; slotIx++)
            {
                for (var iter = 0; iter < 3 && Letters[slotIx][_curPos[slotIx]] != value[slotIx]; iter++)
                {
                    _prefab.DownButtons[slotIx].OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
            }
        }
    }
}