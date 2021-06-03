using System.Collections.Generic;

namespace Variety
{
    public abstract class ItemFactory
    {
        public abstract Item Generate(VarietyModule module, HashSet<object> taken);

        protected static int W { get { return VarietyModule.W; } }
        protected static int H { get { return VarietyModule.H; } }
    }
}