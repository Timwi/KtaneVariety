namespace Variety
{
    public struct ItemSelectable
    {
        public KMSelectable Selectable { get; private set; }
        public int Cell { get; private set; }

        public ItemSelectable(KMSelectable selectable, int cell) : this()
        {
            Selectable = selectable;
            Cell = cell;
        }
    }
}