namespace TableLib
{
    public struct TableItem
    {
        public string Name { get; }
        public string Weight { get; }
        public string Cost { get; }
        public int Rarity { get; }
        public string Description { get; }

        public TableItem(string[] data)
        {
            Name = data[0];
            Weight = data[1];
            Cost = data[2];
            Rarity = int.Parse(data[3]);
            Description = data[4];
        }
    }
}
