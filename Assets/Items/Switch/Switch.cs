using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Variety
{
    public class Switch : Item
    {
        public int Cell { get; private set; }
        public SwitchColor Color { get; private set; }
        public int NumPositions { get; private set; }

        private bool _currentDirectionDown = true;

        private static readonly string[][] _positionNames = {
            new[] { "up", "down" },
            new[] { "up", "middle", "down" },
            new[] { "up", "half-up", "half-down", "down" }
        };

        public Switch(VarietyModule module, int cell, SwitchColor color, int numPositions)
            : base(module, new[] { cell })
        {
            Cell = cell;
            Color = color;
            NumPositions = numPositions;
            State = 0;
        }

        public override IEnumerable<ItemSelectable> SetUp()
        {
            var prefab = Object.Instantiate(Module.SwitchTemplate, Module.transform);
            prefab.transform.localPosition = new Vector3(GetXOfCellRect(Cells[0], 1), .015f, GetYOfCellRect(Cells[0], 4));
            prefab.Selectable.OnInteract = ToggleSwitch(prefab.Selectable.transform);
            prefab.MeshRenderer.sharedMaterial = prefab.SwitchMaterials[(int) Color];
            yield return new ItemSelectable(prefab.Selectable, Cells[0]);
        }

        private KMSelectable.OnInteractHandler ToggleSwitch(Transform switchObj)
        {
            Coroutine _coroutine = null;
            return delegate
            {
                if (State == 0)
                    _currentDirectionDown = false;
                if (State == NumPositions - 1)
                    _currentDirectionDown = true;
                State += _currentDirectionDown ? -1 : 1;

                if (_coroutine != null)
                    Module.StopCoroutine(_coroutine);
                _coroutine = Module.StartCoroutine(MoveSwitch(switchObj, State));
                return false;
            };
        }

        private IEnumerator MoveSwitch(Transform switchObj, int state)
        {
            var duration = .2f;
            var elapsed = 0f;
            var prevRotation = switchObj.localRotation;
            var newRotation = Quaternion.Euler(60f - state * 120f / (NumPositions - 1), 0, 0);

            while (elapsed < duration)
            {
                switchObj.transform.localRotation = Quaternion.Slerp(prevRotation, newRotation, Easing.InOutQuad(elapsed, 0, 1, duration));
                yield return null;
                elapsed += Time.deltaTime;
            }
            switchObj.transform.localRotation = newRotation;
        }

        public override string ToString() { return string.Format("{0} switch with {1} positions at {2}", Color, NumPositions, coords(Cells[0])); }
        public override bool CanProvideStage { get { return true; } }
        public override int NumStates { get { return NumPositions; } }
        public override object Flavor { get { return Color; } }
        public override string DescribeSolutionState(int state) { return string.Format("set the {0} switch (which has {1} positions) to {2}", Color.ToString().ToLowerInvariant(), NumPositions, _positionNames[NumPositions - 2][state]); }
        public override string DescribeWhatUserDid() { return string.Format("you toggled the {0} switch", Color.ToString().ToLowerInvariant()); }
        public override string DescribeWhatUserShouldHaveDone(int desiredState) { return string.Format("you should have toggled the {0} switch to {1} (instead of {2})", Color.ToString().ToLowerInvariant(), _positionNames[NumPositions - 2][desiredState], _positionNames[NumPositions - 2][State]); }
    }
}