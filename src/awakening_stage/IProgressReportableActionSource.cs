public interface IProgressReportableActionSource : ITimedActionSource
{
    /// <summary>
    ///   Used to report the progress before the construction is finished to allow playing construction animations
    /// </summary>
    /// <param name="progress">The current progress in range 0-1</param>
    public void ReportActionProgress(float progress);
}
