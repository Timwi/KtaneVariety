using System;
using System.Collections.Generic;
using System.Linq;
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
            : base(module, CellRect(location, 5, 3))
        {
            Location = location;
            Letters = letters;
            FormableWords = formableWords;
        }

        private void ShowLetters()
        {
            for (var slot = 0; slot < 3; slot++)
                for (var sg = 0; sg < 14; sg++)
                    _prefab.Segments[slot][sg].sharedMaterial = _segmentMapping[Letters[slot][_curPos[slot]] - 'A'].Contains((char) ('a' + sg)) ? _prefab.SegmentOn : _prefab.SegmentOff;
        }

        public override IEnumerable<ItemSelectable> SetUp()
        {
            _prefab = UnityEngine.Object.Instantiate(Module.LetterDisplayTemplate, Module.transform);
            _prefab.transform.localPosition = new Vector3(GetXOfCellRect(Cells[0], 4), .015f, GetYOfCellRect(Cells[0], 3));
            for (var i = 0; i < 3; i++)
            {
                _prefab.DownButtons[i].OnInteract = ButtonPress(i);
                yield return new ItemSelectable(_prefab.DownButtons[i], Cells[0] + i + 1 + 9 * 2);
            }
            ShowLetters();
        }

        private KMSelectable.OnInteractHandler ButtonPress(int btn)
        {
            return delegate
            {
                _curPos[btn] = (_curPos[btn] + 1) % Letters[btn].Length;
                ShowLetters();
                State = Array.IndexOf(FormableWords, Enumerable.Range(0, 3).Select(slot => Letters[slot][_curPos[slot]]).Join(""));
                return false;
            };
        }

        public override string ToString() { return string.Format("letter display which can spell {0}", FormableWords.Join(", ")); }
        public override int NumStates { get { return FormableWords.Length; } }
        public override object Flavor { get { return "LetterDisplay"; } }
        public override string DescribeSolutionState(int state) { return string.Format("set the letter display to {0}", FormableWords[state]); }
        public override string DescribeWhatUserDid() { return "you changed the letter display"; }
        public override string DescribeWhatUserShouldHaveDone(int desiredState) { return string.Format("you should have set the letter display to {0} (you set it to {1})", FormableWords[desiredState], State == -1 ? "an invalid word" : FormableWords[State]); }
    }
}