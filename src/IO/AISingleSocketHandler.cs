/*
 * =================================================================================================
 * REFACTORING SUMMARY
 * =================================================================================================
 * Final version using System.Threading.Channels.Channel<T> instead of BlockingCollection<T>
 * to provide a truly async producer/consumer pipeline. The ReadQueue method now returns
 * an IAsyncEnumerable<T> to be consumed with 'await foreach'.
 * =================================================================================================
*/

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Sufficit.Asterisk.IO
{
    /// <summary>
    ///     Asterisk Interface Single Socket Handler. (Refactored for Async and Performance)
    ///     Handles an active socket connection for Asterisk interfaces, Manager and Gateway.
    /// </summary>
    public class AISingleSocketHandler : ISocketConnection, IDisposable, IAsyncDisposable
    {
        #region Static Counters & Regex

        public static int Running { get; private set; }
        public static int InMemory { get; private set; }

        /// <inheritdoc cref="ISocketStatus.IsConnected"/>
        public bool IsConnected => !IsDisposed && _socket.Connected;

        /// <inheritdoc cref="ISocketStatus.TotalBytesReceived"/>
        public ulong TotalBytesReceived { get; private set; }

        public static Regex AGI_STATUS_PATTERN_NAMED = new Regex(@"^(?<code>\d{3})[ -]", RegexOptions.Compiled);
        public const string AGI_REPLY_HANGUP = "HANGUP";

        #endregion

        private readonly ILogger _logger;
        private readonly Socket _socket;
        private readonly NetworkStream _stream;

        /// <summary>
        /// An unbounded channel used for the async producer-consumer pipeline.
        /// The background reading task acts as the producer, writing lines
        /// from the socket, while the public 'Read' methods act as consumers,
        /// reading lines from this channel.
        /// </summary>
        private readonly Channel<string?> _lineChannel;

        private readonly CancellationTokenSource _internalCts;
        private readonly Task _backgroundReadingTask;

        // Fields for efficient string decoding
        private readonly StringBuilder _stringBuilder;
        private readonly Decoder _decoder;
        private bool? _isRemoteRequest;

        public AGISocketOptions Options { get; }
        public bool IsDisposed { get; private set; }

        public AISingleSocketHandler (ILogger logger, AGISocketOptions options, Socket socket, CancellationToken externalToken = default)
        {
            InMemory++;
            Running++;

            if (!socket.Connected)
                throw new InvalidOperationException("Socket must be connected before creating a handler.");

            _logger = logger;
            _socket = socket;
            Options = options;

            _stream = new NetworkStream(_socket, true);
            _lineChannel = Channel.CreateUnbounded<string?>(new UnboundedChannelOptions { SingleReader = true });
            
            // Link the internal CTS with an optional external one.
            // If the external token is cancelled, our internal token will also be cancelled.
            _internalCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

            _stringBuilder = new StringBuilder();
            _decoder = Options.Encoding.GetDecoder();

            // Start the background reading task immediately using the modern async pattern
            _backgroundReadingTask = BackgroundReadingAsync(_internalCts.Token);

            _logger.LogDebug("async socket handler instantiated, hash: {hash}, socket id: {socket}", GetHashCode(), socket.Handle);
        }

        ~AISingleSocketHandler() => Dispose(false);

        #region Core Reading Loop (Async & Efficient)

        private async Task BackgroundReadingAsync (CancellationToken cancellationToken)
        {
            // create the buffer ONCE and reuse it. This dramatically reduces memory allocation.
            var buffer = new byte[Options.BufferSize];

            _logger.LogDebug("starting async receiver loop, hash: {hash}, socket id: {socket}", GetHashCode(), _socket.Handle);
            AGISocketReason cause;

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Use modern, non-blocking async I/O
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    // A read of 0 bytes indicates a graceful shutdown by the peer.
                    if (bytesRead == 0)
                    {
                        _logger.LogInformation("connection gracefully closed by peer, hash: {hash}", GetHashCode());
                        cause = AGISocketReason.NOTRECEIVING;
                        break;
                    }

                    // Update the total bytes received counter
                    TotalBytesReceived += (ulong)bytesRead;

                    // Process the received data efficiently
                    ProcessReceivedData(buffer, bytesRead);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogTrace("receiving loop was canceled, hash: {hash}", GetHashCode());
                cause = AGISocketReason.CANCELLED;
            }
            catch (ObjectDisposedException)
            {
                // This is expected when the stream is disposed from another thread.
                // We catch it here to prevent it from being an unhandled exception.
                _logger.LogTrace("receiving loop stopped because the socket was disposed, hash: {hash}", GetHashCode());
                cause = AGISocketReason.DISPOSED;
            }
            catch (IOException ex) when (ex.InnerException is SocketException sex)
            {
                _logger.LogError(ex, "io exception in receiver loop, hash: {hash}", GetHashCode());

                // Unpack the underlying socket exception for better error handling
                cause = await TriggerSocketException(sex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "unhandled exception in receiver loop, hash: {hash}", GetHashCode());
                cause = AGISocketReason.UNKNOWN;
            }

            _logger.LogDebug("async receiver loop finished, hash: {hash}, cause: {cause}", GetHashCode(), cause);
            Running--;

            // Mark the channel as complete, allowing consumers (e.g., ReadQueue) to finish processing any remaining items
            // and gracefully exit their 'await foreach' loops.
            _ = _lineChannel.Writer.TryComplete();

            // Always ensure we trigger the disconnect event
            DisconnectedTrigger(cause);            
        }

        /// <summary>
        /// Stops the background reading task and waits for its completion.
        /// </summary>
        /// <remarks>This method ensures that the background reading task is properly stopped by signaling
        /// cancellation and awaiting its completion. If the task is already completed, no action is taken.</remarks>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation of stopping and awaiting the background
        /// reading task.</returns>
        private async ValueTask StopAndAwaitReaderTask()
        {
            if (_backgroundReadingTask != null)
            {
                if (!_backgroundReadingTask.IsCompleted)
                {
                    if (!_internalCts.IsCancellationRequested)
                    {
                        _logger.LogInformation("stopping background reading task, hash: {hash}", GetHashCode());

                        // Cancel the internal CTS to signal the background task to stop
                        _internalCts.Cancel();
                    }

                    await _backgroundReadingTask;
                }
            }
        }

        /// <summary>
        /// Processes raw bytes, decodes them to string, and splits into lines efficiently.
        /// This method handles messages that might be split across multiple TCP packets.
        /// </summary>
        private void ProcessReceivedData(byte[] buffer, int bytesRead)
        {
            // This part is more complex, but avoids allocating MemoryStream and StreamReader on each call.
            int charCount = _decoder.GetCharCount(buffer, 0, bytesRead);
            if (charCount == 0) return;

            char[] chars = new char[charCount];
            _decoder.GetChars(buffer, 0, bytesRead, chars, 0);

            _stringBuilder.Append(chars);

            // Process all complete lines found in the StringBuilder
            while (true)
            {
                string? line = null;
                int newlineIndex = -1;

                for (int i = 0; i < _stringBuilder.Length; i++)
                {
                    if (_stringBuilder[i] == '\n')
                    {
                        newlineIndex = i;
                        break;
                    }
                }

                if (newlineIndex != -1)
                {
                    // Extract the line, removing the trailing '\n' and optional '\r'
                    int lineLength = newlineIndex;
                    if (lineLength > 0 && _stringBuilder[lineLength - 1] == '\r')
                    {
                        lineLength--;
                    }
                    line = _stringBuilder.ToString(0, lineLength);
                    _stringBuilder.Remove(0, newlineIndex + 1);
                }
                else
                {
                    // No more complete lines, break the loop
                    break;
                }

                if (line == AGI_REPLY_HANGUP)
                {
                    HangUpTrigger();
                    continue;
                }

                if (!_lineChannel.Writer.TryWrite(line))
                {
                    _logger.LogWarning("failed to write line to channel, hash: {hash}", GetHashCode());
                }
            }
        }

        #endregion

        #region Public Interface (Read, Write, Close)

        public NetworkStream? GetStream() => !IsDisposed ? _stream : null;

        public async IAsyncEnumerable<string> ReadQueue ([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // This returns an async stream that can be consumed with 'await foreach'
            await foreach (var line in _lineChannel.Reader.ReadAllAsync(cancellationToken))
            {
                if (line != null)
                    yield return line;
            }
        }

        /// <summary>
        /// Reads all lines until an empty line is received.
        /// </summary>
        public async IAsyncEnumerable<string> ReadRequest([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var line in ReadQueue(cancellationToken))
            {
                if (string.IsNullOrWhiteSpace(line))
                    break;
                yield return line;
            }
        }

        /// <summary>
        /// Reads all lines of a reply, stopping after a line that indicates the end of the reply.
        /// </summary>
        public async IAsyncEnumerable<string> ReadReply(uint? timeoutms = null)
        {
            var timeout = timeoutms.HasValue ? (int)timeoutms.Value : (int?)null;
            using var cts = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource();

            await foreach (var line in ReadQueue(cts.Token))
            {
                yield return line;

                var matcher = AGI_STATUS_PATTERN_NAMED.Match(line);
                if (matcher.Groups["code"].Success)
                {
                    if (int.TryParse(matcher.Groups["code"].Value, out int status))
                    {
                        // CONTINUE and INVALID_COMMAND_SYNTAX are statuses that might be followed by more data
                        if (status == 100 || status == 520)
                        {
                            continue;
                        }
                    }
                    // Any other status code indicates the end of the reply
                    break;
                }
            }
        }

        /// <summary>
        /// Asynchronously reads all lines of a reply from the queue.
        /// </summary>
        public async IAsyncEnumerable<string> ReadReplyAsync(uint? timeoutms = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var timeout = timeoutms ?? Options.ReceiveTimeout;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (timeout > 0)
                cts.CancelAfter((int)timeout);

            await foreach (var line in ReadQueue(cts.Token))
            {
                yield return line;

                var matcher = AGI_STATUS_PATTERN_NAMED.Match(line);
                if (matcher.Groups["code"].Success)
                {
                    if (int.TryParse(matcher.Groups["code"].Value, out int status))
                    {
                        // CONTINUE and INVALID_COMMAND_SYNTAX are statuses that might be followed by more data
                        if (status == 100 || status == 520)
                        {
                            continue;
                        }
                    }
                    // Any other status code indicates the end of the reply
                    break;
                }
            }
        }

        public async Task WriteAsync (string data, CancellationToken cancellationToken)
        {
            if (!IsConnected)
                throw new NotConnectedException("Socket is not connected or has been disposed.");
            
            var bytes = Options.Encoding.GetBytes(data);
            try
            {
                await _stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            }
            catch (IOException ex) when (ex.InnerException is SocketException sex)
            {
                _logger.LogError(ex, "error writing to socket, hash: {hash}, socket id: {socket}", GetHashCode(), _socket.Handle);
                //await TriggerSocketException(sex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "unexpected error writing to socket, hash: {hash}, socket id: {socket}", GetHashCode(), _socket.Handle);
                throw;
            }
        }

        #endregion

        #region Properties

        public IPAddress? LocalAddress => (_socket.LocalEndPoint as IPEndPoint)?.Address;
        public int LocalPort => (_socket.LocalEndPoint as IPEndPoint)?.Port ?? 0;
        public IPAddress? RemoteAddress => (_socket.RemoteEndPoint as IPEndPoint)?.Address;
        public int RemotePort => (_socket.RemoteEndPoint as IPEndPoint)?.Port ?? 0;
        public IntPtr Handle => _socket?.Handle ?? IntPtr.Zero;

        public bool IsRemoteRequest
        {
            get
            {
                if (_isRemoteRequest.HasValue)
                    return _isRemoteRequest.Value;

                var remote = _socket.RemoteEndPoint;
                var local = _socket.LocalEndPoint;

                _isRemoteRequest = SocketExtensions.IsRemoteRequest(remote, local);
                _logger.LogDebug("comparing for remote => (remote: {remote}, local: {local}) => {result}", remote, local, _isRemoteRequest);

                return _isRemoteRequest.Value;
            }
        }

        // Simplified event and state properties
        public event EventHandler? OnHangUp;
        public event EventHandler<AGISocketReason>? OnDisconnected;
        public event EventHandler? OnDisposing;

        // HangUp and Disconnect state properties
        public bool IsHangUp { get; private set; }
        private int _isDisconnectTriggered = 0;

        #endregion

        #region Event Triggers & Error Handling

        public ILogger GetLogger() => _logger;

        private void HangUpTrigger()
        {
            IsHangUp = true;
            _logger.LogTrace("hangup detected, hash: {hash}", GetHashCode());
            OnHangUp?.Invoke(this, EventArgs.Empty);
        }

        private void DisconnectedTrigger (AGISocketReason reason)
        {
            // Ensure this logic only runs once using Interlocked
            if (Interlocked.CompareExchange(ref _isDisconnectTriggered, 1, 0) == 0)
            {
                if (!reason.HasFlag(AGISocketReason.NORMALENDING))
                    _logger.LogWarning("disconnected triggered, hash: {hash}, reason: {reason}", GetHashCode(), reason);

                OnDisconnected?.Invoke(this, reason);
            }
        }

        private async ValueTask<AGISocketReason> TriggerSocketException (SocketException ex)
        {
            var cause = ex.SocketErrorCode switch
            {
                SocketError.ConnectionAborted => AGISocketReason.ABORTED,
                SocketError.ConnectionReset => AGISocketReason.RESETED,
                _ => AGISocketReason.UNKNOWN
            };

            if (cause != AGISocketReason.UNKNOWN)
                _logger.LogDebug("socket exception triggered, hash: {hash}, cause: {cause}, code: {code}", GetHashCode(), cause, ex.SocketErrorCode);
            else
                _logger.LogError(ex, "unknown socket exception, hash: {hash}, code: {code}", GetHashCode(), ex.SocketErrorCode);

            await StopAndAwaitReaderTask();
            return cause;
        }

        #endregion

        #region Dispose Pattern

        /// <summary>
        /// This is the synchronous Dispose method. It should not block.
        /// It's mainly for backward compatibility and to be called by the finalizer.
        /// Use DisposeAsync for graceful shutdown.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // Suppress finalization if we are disposing through the synchronous path.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (IsDisposed) return;
            IsDisposed = true;

            OnDisposing?.Invoke(this, EventArgs.Empty);
            InMemory--;

            if (disposing)
            {
                _logger.LogTrace("synchronous disposing of handler, hash: {hash}", GetHashCode());
                // Signal cancellation to the background task, but do not wait for it to complete.
                // If someone calls this synchronous Dispose, they won't block, but the background
                // task might still be running for a short time until it checks the token.
                if (!_internalCts.IsCancellationRequested)                
                    _internalCts.Cancel();                

                // Dispose the internal CTS.
                _internalCts.Dispose();

                // Disposing the stream will close the underlying socket.
                _stream.Dispose();

                // Mark the channel as complete.
                _ = _lineChannel.Writer.TryComplete();

                // We don't dispose of the Task here; it will complete on its own.
                // It's good practice not to dispose a Task object directly.
            }

            // Clear events to prevent memory leaks
            OnDisposing = null;
            OnDisconnected = null;
            OnHangUp = null;
        }

        /// <summary>
        /// Asynchronously releases the resources, ensuring the background reading task completes gracefully.
        /// This is the recommended method for disposing of the socket handler.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (IsDisposed)
            {
                return;
            }

            _logger.LogInformation("async disposing of handler, hash: {hash}", GetHashCode());

            // Signal cancellation to the background task.
            if (!_internalCts.IsCancellationRequested)            
                _internalCts.Cancel();            

            // Await the background task's completion. This is the key to non-blocking disposal.
            await _backgroundReadingTask;

            // Call the synchronous Dispose to clean up other resources.
            // The 'disposing' parameter is 'true' to signal that this is a managed resource cleanup.
            Dispose(true);

            // Suppress finalization, as the disposal has been handled.
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}