namespace GUI.Models
{
    public readonly struct STableTransferResult
    {
        public bool Succeeded { get; }

        public bool Cancelled { get; }

        public string Message { get; }

        public string? ProfileName { get; }

        public STableTransferResult(bool succeeded, bool cancelled, string message, string? profileName = null)
        {
            Succeeded = succeeded;
            Cancelled = cancelled;
            Message = message;
            ProfileName = profileName;
        }

        public static STableTransferResult Ok(string message, string? profileName = null)
        {
            return new STableTransferResult(true, false, message, profileName);
        }

        public static STableTransferResult Fail(string message)
        {
            return new STableTransferResult(false, false, message);
        }

        public static STableTransferResult Cancel()
        {
            return new STableTransferResult(false, true, string.Empty);
        }
    }
}
