using System.Threading;
using System.Threading.Tasks;
using Chakal.Core.Models.Events;

namespace Chakal.Core.Interfaces
{
    /// <summary>
    /// Interface for archiving raw events
    /// </summary>
    public interface IEventArchiver
    {
        /// <summary>
        /// Archives a raw event
        /// </summary>
        /// <param name="evt">The raw event to archive</param>
        /// <param name="ct">Cancellation token</param>
        ValueTask ArchiveAsync(WebcastEnvelope evt, CancellationToken ct);
    }
} 