using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class Wire : Item
    {
        public Wire(VarietyModule module, WireColor color, int[] cells, Func<KMBombInfo, bool> edgeworkCondition) : base(module, cells)
        {
            Color = color;
            EdgeworkCondition = edgeworkCondition;
        }

        public override bool DecideStates(int numPriorNonWireItems)
        {
            _conditionFlipped = EdgeworkCondition(Module.Bomb);
            State = _conditionFlipped ? 1 : 0;
            return true;
        }

        public WireColor Color { get; private set; }
        public Func<KMBombInfo, bool> EdgeworkCondition { get; private set; }

        private bool _isStuck = false;
        private bool _isCut = false;
        private bool _conditionFlipped;
        private KMSelectable _wire;

        public override bool IsStuck { get { return _isStuck; } }
        public override void Checked() { _isStuck = _isCut; }

        public override IEnumerable<ItemSelectable> SetUp()
        {
            var prefab = UnityEngine.Object.Instantiate(Module.WireTemplate, Module.transform);
            var seed = Rnd.Range(0, int.MaxValue);

            var x1 = GetX(Cells[0]);
            var x2 = GetX(Cells[1]);
            var y1 = GetY(Cells[0]);
            var y2 = GetY(Cells[1]);

            var length = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            var numSegments = Math.Max(2, (int) Math.Floor(Math.Sqrt(Math.Pow((Cells[1] % W) - (Cells[0] % W), 2) + Math.Pow((Cells[1] / W) - (Cells[0] / W), 2))));

            prefab.WireMeshFilter.sharedMesh = WireMeshGenerator.GenerateWire(length, numSegments, WireMeshGenerator.WirePiece.Uncut, highlight: false, seed: seed);
            var hl = WireMeshGenerator.GenerateWire(length, numSegments, WireMeshGenerator.WirePiece.Uncut, highlight: true, seed: seed);
            SetHighlightMesh(prefab.WireHighlightMeshFilter, hl);
            prefab.WireCollider.sharedMesh = hl;
            prefab.WireMeshRenderer.sharedMaterial = prefab.WireMaterials[(int) Color];

            prefab.Base1.transform.localPosition = new Vector3(x1, 0.015f, y1);
            prefab.Base2.transform.localPosition = new Vector3(x2, 0.015f, y2);
            _wire = prefab.Wire;
            _wire.transform.localPosition = new Vector3(x1, 0.035f, y1);
            _wire.transform.localEulerAngles = new Vector3(0, Mathf.Atan2(y1 - y2, x2 - x1) / Mathf.PI * 180, 0);

            yield return new ItemSelectable(_wire, Cells[0]);

            _wire.OnInteract = delegate
            {
                if (_isCut)
                    return false;
                _isCut = true;

                _wire.AddInteractionPunch(.5f);
                Module.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, _wire.transform);

                prefab.WireMeshFilter.sharedMesh = WireMeshGenerator.GenerateWire(length, numSegments, WireMeshGenerator.WirePiece.Cut, highlight: false, seed: seed);
                prefab.WireCopperMeshFilter.sharedMesh = WireMeshGenerator.GenerateWire(length, numSegments, WireMeshGenerator.WirePiece.Copper, highlight: false, seed: seed);
                var highlightMesh = WireMeshGenerator.GenerateWire(length, numSegments, WireMeshGenerator.WirePiece.Cut, highlight: true, seed: seed);
                SetHighlightMesh(prefab.WireHighlightMeshFilter, highlightMesh);
                State = _conditionFlipped ? 0 : 1;
                return false;
            };
        }

        private void SetHighlightMesh(MeshFilter mf, Mesh highlightMesh)
        {
            mf.sharedMesh = highlightMesh;
            var child = mf.transform.Find("Highlight(Clone)");
            var filter = child == null ? null : child.GetComponent<MeshFilter>();
            if (filter != null)
                filter.sharedMesh = highlightMesh;
        }

        private static readonly string[] _colorNames = { "black", "blue", "red", "yellow", "white" };

        public override string ToString() { return string.Format("{0} wire", _colorNames[(int) Color]); }
        public override bool CanProvideStage { get { return false; } }
        public override int NumStates { get { return 2; } }
        public override object Flavor { get { return Color; } }
        public override string DescribeSolutionState(int state) { return string.Format((state == 0) ^ _conditionFlipped ? "don’t cut the {0} wire" : "cut the {0} wire", _colorNames[(int) Color]); }
        public override string DescribeWhatUserDid() { return string.Format("you cut the {0} wire", _colorNames[(int) Color]); }
        public override string DescribeWhatUserShouldHaveDone(int desiredState)
        {
            return string.Format(
                (State == 0 && desiredState == 1) ^ _conditionFlipped ? "you should have cut the {0} wire" :
                (State == 1 && desiredState == 0) ^ _conditionFlipped ? "you should not have cut the {0} wire" :
                "[ERROR]",
                _colorNames[(int) Color]);
        }

        public override IEnumerator ProcessTwitchCommand(string command)
        {
            var m = Regex.Match(command, string.Format(@"^\s*cut\s+{0}\s*$", _colorNames[(int) Color]), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!m.Success || _isCut)
                return null;
            return TwitchCut().GetEnumerator();
        }

        private IEnumerable<object> TwitchCut()
        {
            _wire.OnInteract();
            yield return new WaitForSeconds(.1f);
        }

        public override IEnumerable<object> TwitchHandleForcedSolve(int desiredState)
        {
            return State != (_conditionFlipped ? 0 : 1) ? TwitchCut() : null;
        }
    }
}