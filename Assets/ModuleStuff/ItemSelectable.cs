namespace Variety
{
    public struct ItemSelectable
    {
        public KMSelectable Selectable { get; private set; }
        public int[] Cells { get; private set; }

        public ItemSelectable(KMSelectable selectable, int[] cells) : this()
        {
            Selectable = selectable;
            Cells = cells;
        }
    }
}