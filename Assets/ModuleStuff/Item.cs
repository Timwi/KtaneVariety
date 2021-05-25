using System.Collections.Generic;

namespace Variety
{
    public abstract class Item
    {
        public Item(int[] cells) { Cells = cells; }
        public int[] Cells { get; private set; }
        public abstract IEnumerable<ItemSelectable> SetUp(VarietyModule module);
        public abstract override string ToString();

        protected string coords(int ix) { return string.Format("{0}{1}", (char) (ix % VarietyModule.W + 'A'), ix / VarietyModule.W + 1); }

        protected static int W { get { return VarietyModule.W; } }
        protected static int H { get { return VarietyModule.H; } }
        protected static float GetX(int ix) { return (float) (-VarietyModule.Width / 2 + (ix % W) * VarietyModule.CellWidth); }
        protected static float GetY(int ix) { return (float) (VarietyModule.Height / 2 - (ix / W) * VarietyModule.CellHeight + VarietyModule.YOffset); }
    }
}