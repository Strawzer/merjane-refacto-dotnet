namespace Refacto.DotNet.Controllers.Dtos.Product
{
    /// <summary>
    /// Response of <c>POST /orders/{orderId}/processOrder</c>.
    /// </summary>
    /// <remarks>
    /// The lower-case parameter name is part of the public contract and is kept as in the original code.
    /// </remarks>
    /// <param name="id">Identifier of the processed order, serialized as <c>id</c>.</param>
    public record ProcessOrderResponse(long id);
}
