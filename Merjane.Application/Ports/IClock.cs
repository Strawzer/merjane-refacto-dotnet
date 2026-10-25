namespace Merjane.Application.Ports
{
    /// <summary>
    /// Source of the current date and time for the application layer.
    /// </summary>
    /// <remarks>
    /// Injected so that date-dependent rules can be tested with a fixed time.
    /// <see cref="OrderProcessingService"/> reads it once per order.
    /// </remarks>
    public interface IClock
    {
        /// <summary>
        /// Current date and time, in the same time base as the stored product dates (local time in production).
        /// </summary>
        DateTime Now { get; }
    }
}
