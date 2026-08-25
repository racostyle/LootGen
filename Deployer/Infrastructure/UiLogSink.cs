namespace Deployer.Infrastructure
{
    public sealed class UiLogSink
    {
        public Action<string>? Writer { get; set; }

        public void Write(string message)
        {
            Writer?.Invoke(message);
        }
    }
}
