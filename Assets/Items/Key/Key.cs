using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Variety
{
    public class Key : Item
    {
        public Key(VarietyModule module, int cell) : base(module, new[] { cell, cell + 1, cell + W, cell + W + 1 })
        {
            TopLeftCell = cell;
            Turned = false;
        }

        public int TopLeftCell { get; private set; }
        public bool Turned { get; private set; }

        private Transform _core;
        public override IEnumerable<ItemSelectable> SetUp()
        {
            var prefab = Object.Instantiate(Module.KeyTemplate, Module.transform);
            _core = prefab.Core;
            prefab.transform.localPosition = new Vector3(GetX(TopLeftCell) + VarietyModule.CellWidth / 2, 0, GetY(TopLeftCell) - VarietyModule.CellHeight / 2);
            yield return new ItemSelectable(prefab.Key, Cells);
            prefab.Key.OnInteract = TurnKey;
        }

        private bool TurnKey()
        {
            if (Turned)
                return false;

            State = (int) Module.Bomb.GetTime() % 10;
            Turned = true;
            Module.StartCoroutine(KeyTurnAnimation());
            return false;
        }

        private IEnumerator KeyTurnAnimation()
        {
            var elapsed = 0f;
            var duration = .16f;

            while (elapsed < duration)
            {
                _core.transform.localEulerAngles = new Vector3(0, Mathf.Lerp(0, 60, elapsed / duration), 0);
                yield return null;
                elapsed += Time.deltaTime;
            }
            _core.transform.localEulerAngles = new Vector3(0, 60, 0);
        }

        public override string ToString()
        {
            return string.Format("Key at {0}", coords(TopLeftCell));
        }

        public override int NumStates { get { return 10; } }
        protected override bool CheckStateImmediately { get { return true; } }
    }
}