using System;
using System.Collections.Generic;
using UnityEngine;

using Rnd = UnityEngine.Random;

namespace Variety
{
    public class Wire : Item
    {
        public Wire(WireColor color, int[] cells) : base(cells)
        {
            Color = color;
        }

        public WireColor Color { get; private set; }

        public override IEnumerable<ItemSelectable> SetUp(VarietyModule module)
        {
            var prefab = UnityEngine.Object.Instantiate(module.WireTemplate, module.transform);
            var seed = Rnd.Range(0, int.MaxValue);

            var x1 = GetX(Cells[0]);
            var x2 = GetX(Cells[1]);
            var y1 = GetY(Cells[0]);
            var y2 = GetY(Cells[1]);
            var length = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            var numSegments = Math.Max(2, (int) Math.Floor(length / .02));
            prefab.WireMeshFilter.sharedMesh = WireMeshGenerator.GenerateWire(length, numSegments, WireMeshGenerator.WirePiece.Uncut, highlight: false, seed: seed);
            prefab.WireHighlightMeshFilter.sharedMesh = WireMeshGenerator.GenerateWire(length, numSegments, WireMeshGenerator.WirePiece.Uncut, highlight: true, seed: seed);
            prefab.WireMeshRenderer.sharedMaterial = prefab.WireMaterials[(int) Color];

            prefab.Base1.transform.localPosition = new Vector3(x1, 0.015f, y1);
            prefab.Base2.transform.localPosition = new Vector3(x2, 0.015f, y2);
            prefab.Wire.transform.localPosition = new Vector3(x1, 0.035f, y1);
            prefab.Wire.transform.localEulerAngles = new Vector3(0, Mathf.Atan2(y1 - y2, x2 - x1) / Mathf.PI * 180, 0);

            yield return new ItemSelectable(prefab.Wire, Cells);
        }

        public override string ToString()
        {
            return string.Format("{0} wire from {1} to {2}", Color, coords(Cells[0]), coords(Cells[1]));
        }
    }
}