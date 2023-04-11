namespace MassTransit
{
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Mediator;


    public static class MediatorRequestExtensions
    {
        /// <summary>
        /// Sends a request, with the specified response type, and awaits the response.
        /// </summary>
        /// <param name="mediator"></param>
        /// <param name="request">The request message</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T">The response type</typeparam>
        /// <returns>The response object</returns>
        public static async Task<T> SendRequest<T>(this IMediator mediator, Request<T> request, CancellationToken cancellationToken = default)
            where T : class
        {
            try
            {
                Response<T> response = await mediator.CreateRequest(request, cancellationToken).GetResponse<T>().ConfigureAwait(false);

                return response.Message;
            }
            catch (RequestException exception)
            {
                if (exception.InnerException != null)
                {
                    var dispatchInfo = ExceptionDispatchInfo.Capture(exception.InnerException);

                    dispatchInfo.Throw();
                }

                throw;
            }
        }
    }
}
