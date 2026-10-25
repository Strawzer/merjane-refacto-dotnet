using Merjane.Application;
using Microsoft.AspNetCore.Mvc;
using Refacto.DotNet.Controllers.Dtos.Product;

namespace Refacto.DotNet.Controllers.Controllers
{
    /// <summary>
    /// HTTP entry point for order processing.
    /// </summary>
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderProcessingService _orderProcessingService;

        /// <summary>
        /// Creates the controller.
        /// </summary>
        /// <param name="orderProcessingService">Use case that processes the order.</param>
        public OrdersController(OrderProcessingService orderProcessingService)
        {
            _orderProcessingService = orderProcessingService;
        }

        /// <summary>
        /// Processes an order: <c>POST /orders/{orderId}/processOrder</c>.
        /// </summary>
        /// <remarks>
        /// Exceptions are not mapped to HTTP statuses, so an unknown order or a processing failure results in HTTP 500,
        /// as in the original code.
        /// </remarks>
        /// <param name="orderId">Identifier of the order.</param>
        /// <returns>HTTP 200 with the processed order identifier, serialized as <c>{"id":orderId}</c>.</returns>
        /// <exception cref="OrderNotFoundException">The order does not exist.</exception>
        /// <exception cref="InvalidOperationException">An <c>EXPIRABLE</c> product of the order has no expiry date.</exception>
        [HttpPost("{orderId}/processOrder")]
        [ProducesResponseType(200)]
        public ActionResult<ProcessOrderResponse> ProcessOrder(long orderId)
        {
            return new ProcessOrderResponse(_orderProcessingService.ProcessOrder(orderId));
        }
    }
}
