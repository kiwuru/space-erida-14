using System.Threading;

namespace Content.Server.Carrying
{
    /// <summary>
    /// Stores the carrier of an entity being carried.
    /// </summary>
    [RegisterComponent]
    public sealed partial class BeingCarriedComponent : Component
    {
        public EntityUid Carrier = default!;

        /// <summary>
        /// Cancel token for escape attempts
        /// </summary>
        public CancellationTokenSource? EscapeCancelToken;

        /// <summary>
        /// Dispose the escape cancel token if it exists
        /// </summary>
        public void DisposeEscapeToken()
        {
            EscapeCancelToken?.Dispose();
            EscapeCancelToken = null;
        }
    }
}
