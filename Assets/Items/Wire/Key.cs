using System.Collections.Generic;
using UnityEngine;

namespace Variety
{
    public class Key : Item
    {
        public Key(int cell) : base(new[] { cell, cell + 1, cell + W, cell + W + 1 })
        {
            TopLeftCell = cell;
        }

        public int TopLeftCell { get; private set; }

        public override IEnumerable<ItemSelectable> SetUp(VarietyModule module)
        {
            var prefab = Object.Instantiate(module.KeyTemplate, module.transform);
            prefab.transform.localPosition = new Vector3(GetX(TopLeftCell) + VarietyModule.CellWidth / 2, 0, GetY(TopLeftCell) + VarietyModule.CellHeight / 2);
            yield return new ItemSelectable(prefab.Key, Cells);
        }

        public override string ToString()
        {
            return string.Format("Key at {0}", coords(TopLeftCell));
        }
    }
}