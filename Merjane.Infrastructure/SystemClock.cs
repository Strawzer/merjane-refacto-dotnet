using Merjane.Application.Ports;

namespace Merjane.Infrastructure
{
    public class SystemClock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
