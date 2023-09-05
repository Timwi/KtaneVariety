using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Variety
{
    public abstract class Item
    {
        public Item(VarietyModule module, int[] cells) { Module = module; Cells = cells; }
        public VarietyModule Module { get; private set; }
        public int[] Cells { get; private set; }

        public abstract IEnumerable<ItemSelectable> SetUp(Random rnd);
        public abstract override string ToString();
        public abstract int NumStates { get; }
        public abstract object Flavor { get; }
        public virtual bool CanProvideStage { get { return true; } }
        public abstract string DescribeSolutionState(int state);
        public abstract string DescribeWhatUserDid();
        public abstract string DescribeWhatUserShouldHaveDone(int desiredState);
        public virtual void SetColorblind(bool on) { }
        public virtual void Checked() { }
        public virtual bool IsStuck { get { return false; } }
        public virtual bool DecideStates(int numPriorNonWireItems) { return true; }
        public virtual void ReceiveItemChange(int stageItemIndex) { }
        public abstract IEnumerator ProcessTwitchCommand(string command);
        public abstract IEnumerable<object> TwitchHandleForcedSolve(int desiredState);
        public abstract string TwitchHelpMessage { get; }

        private int _state;
        public int State { get { return _state; } }
        public void SetState(int state, bool automatic = false)
        {
            if (_state != state)
            {
                _state = state;
                if (StateSet != null)
                    StateSet(state, automatic);
            }
        }
        public Action<int, bool> StateSet;

        protected string coords(int ix) { return string.Format("{0}{1}", (char) (ix % VarietyModule.W + 'A'), ix / VarietyModule.W + 1); }

        protected static int W { get { return VarietyModule.W; } }
        protected static int H { get { return VarietyModule.H; } }

        protected static float GetX(int ix) { return VarietyModule.GetX(ix); }
        protected static float GetY(int ix) { return VarietyModule.GetY(ix); }

        protected static int[] CellRect(int cell, int width, int height) { return Enumerable.Range(0, width * height).Select(i => i % width + cell % W + W * (i / width + cell / W)).ToArray(); }
        protected static float GetXOfCellRect(int cell, int width) { return -VarietyModule.Width / 2 + (cell % W + (width - 1) * .5f) * VarietyModule.CellWidth; }
        protected static float GetYOfCellRect(int cell, int height) { return VarietyModule.Height / 2 - (cell / W + (height - 1) * .5f) * VarietyModule.CellHeight + VarietyModule.YOffset; }
    }
}