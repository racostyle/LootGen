namespace TableLib
{
    public readonly struct TableItem
    {
        public string Name { get; }
        public string Weight { get; }
        public string Cost { get; }
        public int Rarity { get; }
        public string Description { get; }
        public string Notes { get; }
        public string Category { get; }

        public TableItem(string[] data, string category)
        {
            Name = data[0];
            Weight = data[1];
            Cost = data[2];

            if (int.TryParse(data[3].Trim(), out int parsed))
                Rarity = parsed;
            else
                Rarity = 0;

            Description = data[4];
            Notes = data.Length > 5 ? data[5] : string.Empty;
            Category = category;
        }
    }
}
