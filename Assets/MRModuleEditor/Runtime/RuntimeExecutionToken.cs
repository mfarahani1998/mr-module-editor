namespace MRModuleEditor.Runtime
{
    /// <summary>
    /// Immutable-per-run cancellation handle passed into every step context.
    ///
    /// A new token is created for each Play() call. Restart/Stop cancels the
    /// previous token instead of relying on a single shared bool that may be
    /// flipped back to false by the next run.
    /// </summary>
    public sealed class RuntimeExecutionToken
    {
        public RuntimeExecutionToken(int runId)
        {
            RunId = runId;
        }

        public int RunId { get; private set; }
        public bool IsCancellationRequested { get; private set; }
        public string CancellationReason { get; private set; } = "";

        public void Cancel(string reason)
        {
            if (IsCancellationRequested)
            {
                return;
            }

            IsCancellationRequested = true;
            CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Cancelled." : reason;
        }
    }
}
