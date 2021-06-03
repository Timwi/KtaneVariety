using System.Collections.Generic;
using UnityEngine;

namespace Variety
{
    public class Dummy : Item
    {
        public Dummy(VarietyModule module, int cell) : base(module, new[] { cell })
        {
            Cell = cell;
        }

        public int Cell { get; private set; }

        public override IEnumerable<ItemSelectable> SetUp()
        {
            var prefab = Object.Instantiate(Module.DummyTemplate, Module.transform);
            prefab.transform.localPosition = new Vector3(GetX(Cell), .01502f, GetY(Cell));
            prefab.transform.localEulerAngles = new Vector3(90, 0, 0);
            yield break;
        }

        public override string ToString()
        {
            return string.Format("Dummy at {0}", coords(Cell));
        }

        public override int NumStates { get { return 0; } }
        protected override bool CheckStateImmediately { get { return true; } }
    }
}