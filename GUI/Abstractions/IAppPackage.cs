namespace GUI.Abstractions
{
    public interface IAppPackage
    {
        IReadOnlyList<string> ListFiles(string relativeDirectory);

        string ReadAllText(string relativePath);
    }
}
