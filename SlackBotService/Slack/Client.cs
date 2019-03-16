using ServiceStack.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace SlackBot.Slack
{
    /// <summary>
    /// Represents a realtime messaging client.
    /// </summary>
    public class Client : ISlack, IDisposable
    {
        private string _token;
        private WebSocket _websocket;
        private bool _isDisposed;

        private ClientConnectionState _connectionState;
        /// <summary>
        /// Gets the state of the connection.
        /// </summary>
        /// <value>
        /// The state of the connection.
        /// </value>
        public ClientConnectionState ConnectionState
        {
            get { return _connectionState; }
            private set
            {
                if (value != _connectionState)
                {
                    _connectionState = value;
                    var csc = Interlocked.CompareExchange(ref ConnectionStateChanged, null, null);
                    if (csc != null) csc(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Gets the identifier of the client.
        /// </summary>
        /// <value>
        /// The identifier of the client.
        /// </value>
        public string Id
        {
            get;
            private set;
        }

        /// <summary>
        /// Occurs when when the <see cref="ConnectionState"/> property changes.
        /// </summary>
        public event EventHandler ConnectionStateChanged;

        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        public event EventHandler<Message> MessageReceived;

        /// <summary>
        /// Occurs when an error occurs.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> Error;

        /// <summary>
        /// Initializes a new instance of the <see cref="Client"/> class.
        /// </summary>
        public Client()
        {

        }

        /// <summary>
        /// Asynchronously opens a connection to the Slack RTM API.
        /// </summary>
        /// <param name="token">Your API token.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns>
        /// A <see cref="System.Threading.Tasks.Task"/> that represents the asynchronous open operation.
        /// </returns>
        public async Task OpenAsync(string token, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_isDisposed) throw new ObjectDisposedException("Client");
            if (ConnectionState != ClientConnectionState.Disconnected)
                throw new InvalidOperationException("Client must not be connected.");
            ConnectionState = ClientConnectionState.Connecting;

            _token = token;
            try
            {
                var response = await Http.GetJsonAsync("https://slack.com/api/rtm.start", cancellationToken, "token", _token);
                if (response["ok"] == "true")
                {
                    var ws = _websocket = new WebSocket(response["url"]);
                    ws.Open();
                    ConnectionState = ClientConnectionState.Connected;

                    if (ConnectionState == ClientConnectionState.Connected)
                        BeginReceive(ws);

                    var self = response.Object("self");
                    Id = self["id"];

                    var chans = response.ArrayObjects("channels");
                }
                else
                {
                    ConnectionState = ClientConnectionState.Disconnected;
                    throw SlackException.FromStatusCode(response["error"]);
                }
            }
            catch(Exception ex)
            {
                //We need to reset the connection state to disconnected if it fails to connect
                //so that it will keep retying.
                ConnectionState = ClientConnectionState.Disconnected;
                throw;
            }
            
        }

        /// <summary>
        /// Begins receiving data from a websocket.
        /// </summary>
        /// <param name="ws">The ws.</param>
        private void BeginReceive(WebSocket ws)
        {
            ws.MessageReceived += Ws_MessageReceived;
        }

        private void Ws_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var obj = JsonObject.Parse(e.Message);
            switch (obj["type"])
            {
                case "hello": ConnectionState = ClientConnectionState.Established; break;
                case "message": HandleMessage(obj); break;
            }
        }

        /// <summary>
        /// Asynchronously sends the specified message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns>The message.</returns>
        public async Task SendAsync(Message message, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (message == null) throw new ArgumentNullException("message");
            if (_isDisposed) throw new ObjectDisposedException("Client");
            if (ConnectionState != ClientConnectionState.Established) throw new InvalidOperationException("Client must be connected.");

            var json = message.ToJson();
            var response = await Http.PostJsonAsync("https://slack.com/api/chat.postMessage", json, cancellationToken, "token", _token);
            if (response["ok"] != "true")
                throw SlackException.FromStatusCode(response["error"]);
        }

        /// <summary>
        /// Handles a message.
        /// </summary>
        /// <param name="obj">The object.</param>
        private void HandleMessage(JsonObject obj)
        {
            Interlocked.CompareExchange(ref MessageReceived, null, null)?.Invoke(this, Json.Message(obj));
        }

        /// <summary>
        /// Asynchronously closes the connection to the server.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
        /// <returns>
        /// A <see cref="Task"/> that represents the asynchronous close operation.
        /// </returns>
        public async Task CloseAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var ws = Interlocked.Exchange(ref _websocket, null);

            if (ws != null &&
                (int)ConnectionState >= (int)ClientConnectionState.Connecting)
            {
                ConnectionState = ClientConnectionState.Disconnecting;

                try
                {
                    ws.Close();
                    ws.Dispose();
                }
                catch { }

                ConnectionState = ClientConnectionState.Disconnected;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            _isDisposed = true;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing) CloseAsync().Wait();
        }
    }
}
