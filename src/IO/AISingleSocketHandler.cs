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
    public class AISingleSocketHandler : ISocketConnection, IDisposable
    {
        #region Static Counters & Regex

        public static int Running { get; private set; }
        public static int InMemory { get; private set; }

        public static Regex AGI_STATUS_PATTERN_NAMED = new Regex(@"^(?<code>\d{3})[ -]", RegexOptions.Compiled);
        public const string AGI_REPLY_HANGUP = "HANGUP";

        #endregion

        private readonly ILogger _logger;
        private readonly Socket _socket;
        private readonly NetworkStream _stream;

        private readonly Channel<string?> _lineChannel;

        private readonly CancellationTokenSource _internalCts;
        private readonly Task _backgroundReadingTask;

        // Fields for efficient string decoding
        private readonly StringBuilder _stringBuilder;
        private readonly Decoder _decoder;
        private bool? _isRemoteRequest;

        public AGISocketOptions Options { get; }
        public bool IsDisposed { get; private set; }

        public AISingleSocketHandler(ILogger logger, AGISocketOptions options, Socket socket, CancellationToken externalToken = default)
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

            _logger.LogDebug("({hash}) Async socket handler instantiated. Socket id: {socket}", GetHashCode(), socket.Handle);
        }

        ~AISingleSocketHandler() => Dispose(false);

        #region Core Reading Loop (Async & Efficient)

        private async Task BackgroundReadingAsync(CancellationToken token)
        {
            // Create the buffer ONCE and reuse it. This dramatically reduces memory allocation.
            var buffer = new byte[Options.BufferSize];

            _logger.LogInformation("({hash}) Starting async receiver loop. Socket id: {socket}", GetHashCode(), _socket.Handle);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    // Use modern, non-blocking async I/O
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, token);

                    // A read of 0 bytes indicates a graceful shutdown by the peer.
                    if (bytesRead == 0)
                    {
                        _logger.LogInformation("({hash}) Connection gracefully closed by peer.", GetHashCode());
                        DisconnectedTrigger(AGISocketReason.NOTRECEIVING);
                        break;
                    }

                    // Process the received data efficiently
                    ProcessReceivedData(buffer, bytesRead);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogTrace("({hash}) Receiving loop was canceled.", GetHashCode());
            }
            catch (IOException ex) when (ex.InnerException is SocketException sex)
            {
                // Unpack the underlying socket exception for better error handling
                TriggerSocketException(sex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "({hash}) Unhandled exception in receiver loop.", GetHashCode());
                DisconnectedTrigger(AGISocketReason.UNKNOWN);
            }
            finally
            {
                _logger.LogInformation("({hash}) Async receiver loop finished.", GetHashCode());
                Running--;
                // Ensure the buffer is marked as complete for any consumers
                _lineChannel.Writer.Complete();
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
                    _logger.LogWarning("({hash}) Failed to write line to channel.", GetHashCode());
                }
            }
        }

        #endregion

        #region Public Interface (Read, Write, Close)

        public NetworkStream? GetStream() => !IsDisposed ? _stream : null;

        public async IAsyncEnumerable<string> ReadQueue([EnumeratorCancellation] CancellationToken cancellationToken)
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

        public void Write(string s)
        {
            if (!IsConnected)
                throw new NotConnectedException("Socket is not connected or has been disposed.");

            var bytes = Options.Encoding.GetBytes(s);
            try
            {
                _stream.Write(bytes, 0, bytes.Length);
            }
            catch (IOException ex) when (ex.InnerException is SocketException sex)
            {
                TriggerSocketException(sex);
                throw;
            }
        }

        /// <summary>
        ///     Indicates that <see cref="Close(string?)"></see> was already called />
        /// </summary>
        public bool IsCloseRequested { get; private set; }

        public void Close (AGISocketReason reason)
        {
            if (IsDisposed) return;
            if (IsCloseRequested) return;
            IsCloseRequested = true;
            
            if (!reason.HasFlag(AGISocketReason.NORMALENDING))
                _logger.LogWarning("({hash}) Closing connection, reason: {cause}", GetHashCode(), reason);

            DisconnectedTrigger(reason);

            // This will signal the background reading task to stop.
            if (!_internalCts.IsCancellationRequested)
                _internalCts.Cancel();
        }

        public void Close(string? reason = null)
        {          
            _logger.LogWarning("({hash}) Closing connection, reason: {cause}", GetHashCode(), reason ?? "N/A");
            Close(AGISocketReason.UNKNOWN);
        }

        #endregion

        #region Properties

        public bool IsConnected => !IsDisposed && _socket.Connected;
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
            _logger.LogTrace("({hash}) HangUp detected.", GetHashCode());
            OnHangUp?.Invoke(this, EventArgs.Empty);
        }

        private void DisconnectedTrigger(AGISocketReason reason)
        {
            // Ensure this logic only runs once using Interlocked
            if (Interlocked.CompareExchange(ref _isDisconnectTriggered, 1, 0) == 0)
            {
                if (!reason.HasFlag(AGISocketReason.NORMALENDING))
                    _logger.LogWarning("({hash}) Disconnected triggered, reason: {reason}", GetHashCode(), reason);

                OnDisconnected?.Invoke(this, reason);

                // Trigger the cancellation token to stop the reader task
                if (!_internalCts.IsCancellationRequested)
                    _internalCts.Cancel();
            }
        }

        private void TriggerSocketException(SocketException ex)
        {
            AGISocketReason cause = ex.SocketErrorCode switch
            {
                SocketError.ConnectionAborted => AGISocketReason.ABORTED,
                SocketError.ConnectionReset => AGISocketReason.RESETED,
                _ => AGISocketReason.UNKNOWN
            };

            if (cause != AGISocketReason.UNKNOWN)
                _logger.LogDebug("({hash}) Socket exception triggered: {reason} ({code})", GetHashCode(), cause, ex.SocketErrorCode);
            else
                _logger.LogError(ex, "({hash}) Unknown socket exception: {code}", GetHashCode(), ex.SocketErrorCode);

            DisconnectedTrigger(cause);
        }

        #endregion

        #region Dispose Pattern

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed) return;
            IsDisposed = true;

            OnDisposing?.Invoke(this, EventArgs.Empty);
            InMemory--;

            if (disposing)
            {
                // Signal the background task to stop
                if (!_internalCts.IsCancellationRequested)
                {
                    _internalCts.Cancel();
                }

                // Disposing the stream will close the underlying socket.
                _stream.Dispose();
                _lineChannel.Writer.Complete();
                _internalCts.Dispose();

                try
                {
                    // It's good practice to wait for the task to complete, but with a timeout
                    // to avoid blocking indefinitely.
                    _backgroundReadingTask.Wait(1000);
                }
                catch (Exception ex)
                {
                    // This is expected if the task was canceled or faulted.
                    _logger.LogDebug(ex, "Exception while waiting for background task to complete during dispose.");
                }
                finally
                {
                    _backgroundReadingTask.Dispose();
                }
            }

            // Clear events to prevent memory leaks
            OnDisposing = null;
            OnDisconnected = null;
            OnHangUp = null;
        }

        #endregion
    }
}