namespace GUI.Models
{
    public readonly struct SLabeledValue
    {
        public string Name { get; }
        public int Value { get; }

        public SLabeledValue(string name, int value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString()
        {
            return $"{Name} = {Value}";
        }
    }
}
