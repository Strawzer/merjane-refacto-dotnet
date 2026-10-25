using Merjane.Application.Ports;

namespace Merjane.Infrastructure
{
    /// <summary>
    /// <see cref="IClock"/> based on the machine clock.
    /// </summary>
    /// <remarks>
    /// Stateless, so it is registered as a singleton.
    /// </remarks>
    public class SystemClock : IClock
    {
        /// <inheritdoc/>
        /// <value>The local date and time of the host (<see cref="DateTime.Now"/>), as in the original code.</value>
        public DateTime Now => DateTime.Now;
    }
}
