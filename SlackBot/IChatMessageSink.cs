using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlackBot
{
    /// <summary>
    /// Represents a chat message sink.
    /// </summary>
    public interface IChatMessageSink
    {
        /// <summary>
        /// Asynchronously initializes the sink.
        /// </summary>
        /// <param name="name">The configuration name of the sink.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The default 
        /// value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns>
        /// A <see cref="Task"/> that represents the asynchronous initialize operation.
        /// </returns>
        Task InitializeAsync(string name, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Asynchronously processes a message.
        /// </summary>
        /// <param name="slack">The slack.</param>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The default 
        /// value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns>
        /// A <see cref="Task{ChatMessageSinkResult}"/> that represents the asynchronous process operation.
        /// </returns>
        Task<ChatMessageSinkResult> ProcessMessageAsync(ISlack slack, Message message, CancellationToken cancellationToken = default(CancellationToken));
    }
}
