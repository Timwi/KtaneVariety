using System;
using System.Collections.Generic;
using UnityEngine;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class Wire : Item
    {
        public Wire(VarietyModule module, WireColor color, int[] cells, bool conditionFlipped) : base(module, cells)
        {
            Color = color;
            State = conditionFlipped ? 1 : 0;
            _conditionFlipped = conditionFlipped;
        }

        public WireColor Color { get; private set; }

        private bool _isStuck = false;
        private bool _conditionFlipped;
        public override bool IsStuck { get { return _isStuck; } }
        public override void Checked() { _isStuck = State == 1; }

        public override IEnumerable<ItemSelectable> SetUp()
        {
            Debug.LogFormat("<><> 1: {0} ({1})", Module.WireTemplate == null, Module.WireTemplate);
            var prefab = UnityEngine.Object.Instantiate(Module.WireTemplate, Module.transform);
            Debug.LogFormat("<><> 2");
            var seed = Rnd.Range(0, int.MaxValue);

            var x1 = GetX(Cells[0]);
            var x2 = GetX(Cells[1]);
            var y1 = GetY(Cells[0]);
            var y2 = GetY(Cells[1]);
            var length = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            var numSegments = Math.Max(2, (int) Math.Floor(length / .04));
            prefab.WireMeshFilter.sharedMesh = WireMeshGenerator.GenerateWire(length, numSegments, WireMeshGenerator.WirePiece.Uncut, highlight: false, seed: seed);
            var hl = WireMeshGenerator.GenerateWire(length, numSegments, WireMeshGenerator.WirePiece.Uncut, highlight: true, seed: seed);
            prefab.WireHighlightMeshFilter.sharedMesh = hl;
            prefab.WireCollider.sharedMesh = hl;
            prefab.WireMeshRenderer.sharedMaterial = prefab.WireMaterials[(int) Color];

            prefab.Base1.transform.localPosition = new Vector3(x1, 0.015f, y1);
            prefab.Base2.transform.localPosition = new Vector3(x2, 0.015f, y2);
            prefab.Wire.transform.localPosition = new Vector3(x1, 0.035f, y1);
            prefab.Wire.transform.localEulerAngles = new Vector3(0, Mathf.Atan2(y1 - y2, x2 - x1) / Mathf.PI * 180, 0);

            yield return new ItemSelectable(prefab.Wire, Cells[0]);

            prefab.Wire.OnInteract = delegate
            {
                prefab.WireMeshFilter.sharedMesh = WireMeshGenerator.GenerateWire(length, numSegments, WireMeshGenerator.WirePiece.Cut, highlight: false, seed: seed);
                prefab.WireCopperMeshFilter.sharedMesh = WireMeshGenerator.GenerateWire(length, numSegments, WireMeshGenerator.WirePiece.Copper, highlight: false, seed: seed);
                var highlightMesh = WireMeshGenerator.GenerateWire(length, numSegments, WireMeshGenerator.WirePiece.Cut, highlight: true, seed: seed);
                prefab.WireHighlightMeshFilter.sharedMesh = highlightMesh;
                var child = prefab.WireHighlightMeshFilter.transform.Find("Highlight(Clone)");
                var filter = child == null ? null : child.GetComponent<MeshFilter>();
                if (filter != null)
                    filter.sharedMesh = highlightMesh;
                State ^= 1;
                return false;
            };
        }

        private static readonly string[] _colorNames = { "black", "blue", "red", "yellow", "white" };

        public override string ToString() { return string.Format("{0} wire from {1} to {2}", _colorNames[(int) Color], coords(Cells[0]), coords(Cells[1])); }
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
    }
}