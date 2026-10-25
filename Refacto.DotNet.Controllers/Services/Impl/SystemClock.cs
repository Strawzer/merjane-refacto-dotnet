using Merjane.Application.Ports;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    public class SystemClock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
