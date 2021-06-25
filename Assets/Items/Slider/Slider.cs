using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Variety
{
    public class Slider : Item
    {
        public const int LongSlots = 6;
        public const int ShortSlots = 2;

        public int X { get; private set; }
        public int Y { get; private set; }
        public SliderOrientation Orientation { get; private set; }
        public int NumTicks { get; private set; }

        public static int SW(SliderOrientation orientation) { return orientation == SliderOrientation.Horizontal ? LongSlots : ShortSlots; }
        public static int SH(SliderOrientation orientation) { return orientation == SliderOrientation.Horizontal ? ShortSlots : LongSlots; }

        public Slider(VarietyModule module, int x, int y, SliderOrientation orientation) : base(module, CellRect(x + W * y, SW(orientation), SH(orientation)))
        {
            X = x;
            Y = y;
            Orientation = orientation;
            NumTicks = Random.Range(3, 8);
        }

        public override IEnumerable<ItemSelectable> SetUp()
        {
            var prefab = Object.Instantiate(Module.SliderTemplate, Module.transform);

            if (Orientation == SliderOrientation.Horizontal)
            {
                var x = -VarietyModule.Width / 2 + (X + (LongSlots - 1) * .5f) * VarietyModule.CellWidth;
                var y = VarietyModule.Height / 2 - (Y + (ShortSlots - 1) * .5f) * VarietyModule.CellHeight + VarietyModule.YOffset;
                prefab.transform.localPosition = new Vector3(x, 0, y);
                prefab.transform.localRotation = Quaternion.identity;
            }
            else
            {
                var x = -VarietyModule.Width / 2 + (X + (ShortSlots - 1) * .5f) * VarietyModule.CellWidth;
                var y = VarietyModule.Height / 2 - (Y + (LongSlots - 1) * .5f) * VarietyModule.CellHeight + VarietyModule.YOffset;
                prefab.transform.localPosition = new Vector3(x, 0, y);
                prefab.transform.localRotation = Quaternion.Euler(0, 90, 0);
            }

            State = Random.Range(0, NumTicks);
            prefab.Knob.transform.localPosition = new Vector3(XPosition(State), .021f, 0);

            for (var tick = 0; tick < NumTicks; tick++)
            {
                var obj = tick == 0 ? prefab.TickTemplate : Object.Instantiate(prefab.TickTemplate, prefab.transform);
                obj.transform.localPosition = new Vector3(XPosition(tick), .0151f, .0075f);
                obj.transform.localRotation = Quaternion.Euler(90, 0, 0);
                obj.transform.localScale = new Vector3(.001f, .005f, 1);
            }

            var movingRight = State != NumTicks - 1;
            Coroutine coroutine = null;

            prefab.Knob.OnInteract += delegate
            {
                State += movingRight ? 1 : -1;
                if (State == 0)
                    movingRight = true;
                else if (State == NumTicks - 1)
                    movingRight = false;

                if (coroutine != null)
                    Module.StopCoroutine(coroutine);
                coroutine = Module.StartCoroutine(MoveKnob(prefab.Knob.transform.localPosition.x, XPosition(State), prefab.Knob.transform));
                return false;
            };

            yield return new ItemSelectable(prefab.Knob, X + SW(Orientation) / 2 + W * (Y + SH(Orientation) / 2));
        }

        private IEnumerator MoveKnob(float startX, float endX, Transform knob)
        {
            var duration = .1f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                knob.transform.localPosition = new Vector3(Easing.InOutQuad(elapsed, startX, endX, duration), .021f, 0);
                yield return null;
                elapsed += Time.deltaTime;
            }
            knob.transform.localPosition = new Vector3(endX, .021f, 0);
        }

        private float XPosition(int state)
        {
            return Mathf.Lerp(-0.028f, 0.028f, state * 1f / (NumTicks - 1));
        }

        public override string ToString() { return string.Format("{0} slider at {1}", Orientation.ToString(), coords(Cells[0])); }
        public override int NumStates { get { return NumTicks; } }
        public override object Flavor { get { return Orientation; } }
        public override string DescribeState(int state, bool isSolution = false) { return string.Format(isSolution ? "set to {0}" : "{0}", state + 1); }
    }
}