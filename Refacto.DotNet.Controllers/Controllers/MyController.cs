using Merjane.Application;
using Microsoft.AspNetCore.Mvc;
using Refacto.DotNet.Controllers.Dtos.Product;

namespace Refacto.DotNet.Controllers.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderProcessingService _orderProcessingService;

        public OrdersController(OrderProcessingService orderProcessingService)
        {
            _orderProcessingService = orderProcessingService;
        }

        [HttpPost("{orderId}/processOrder")]
        [ProducesResponseType(200)]
        public ActionResult<ProcessOrderResponse> ProcessOrder(long orderId)
        {
            return new ProcessOrderResponse(_orderProcessingService.ProcessOrder(orderId));
        }
    }
}
