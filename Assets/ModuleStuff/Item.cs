using System;
using System.Collections.Generic;

namespace Variety
{
    public abstract class Item
    {
        public Item(VarietyModule module, int[] cells) { Module = module; Cells = cells; }
        public VarietyModule Module { get; private set; }
        public int[] Cells { get; private set; }
        public ItemStatus Status { get; set; }

        public abstract IEnumerable<ItemSelectable> SetUp();
        public abstract override string ToString();
        public abstract int NumStates { get; }

        private int _state;
        public int State
        {
            get { return _state; }
            protected set
            {
                _state = value;
                if (StateSet != null)
                    StateSet(value);
            }
        }
        public Action<int> StateSet;

        protected abstract bool CheckStateImmediately { get; }
        protected string coords(int ix) { return string.Format("{0}{1}", (char) (ix % VarietyModule.W + 'A'), ix / VarietyModule.W + 1); }

        protected static int W { get { return VarietyModule.W; } }
        protected static int H { get { return VarietyModule.H; } }
        protected static float GetX(int ix) { return -VarietyModule.Width / 2 + (ix % W) * VarietyModule.CellWidth; }
        protected static float GetY(int ix) { return VarietyModule.Height / 2 - (ix / W) * VarietyModule.CellHeight + VarietyModule.YOffset; }
    }
}