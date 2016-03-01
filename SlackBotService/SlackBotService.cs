using SlackBot.Slack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SlackBot
{
    /// <summary>
    /// Represents a service that communicates with Slack.
    /// </summary>
    [System.ComponentModel.DesignerCategory("")]
    partial class SlackBotService : ServiceBase
    {
        private Client _client;
        private bool _running;
        private List<IChatMessageSink> _sinks;
        private bool _isConsole;

        /// <summary>
        /// Initializes a new instance of the <see cref="SlackBotService"/> class.
        /// </summary>
        public SlackBotService()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Used to start the service in console mode.
        /// </summary>
        /// <param name="args">The arguments.</param>
        internal void ConsoleStart(string[] args)
        {
            _isConsole = true;
            OnStart(args);
        }

        /// <summary>
        /// Executes when a Start command is sent to the service by the Service Control Manager (SCM) or when the operating system starts (for a service that starts automatically). Specifies actions to take when the service starts.
        /// </summary>
        /// <param name="args">Data passed by the start command.</param>
        protected override async void OnStart(string[] args)
        {
            if (_client != null)
                _client.Dispose();

            _running = true;
            _client = new Client();
            _client.ConnectionStateChanged += OnConnectionStateChanged;
            _client.Error += OnError;
            _client.MessageReceived += OnMessageReceived;
            _sinks = new List<IChatMessageSink>();
            
            foreach (var pendingSink in Configuration.SlackConfigurationSection.Current.Sinks.CreateSinks())
            {
                try
                {
                    _sinks.Add(await pendingSink);
                }
                catch (Exception ex)
                {
                    OnError(_client, ex);
                }
            }

            await ConnectAsync();
        }

        /// <summary>
        /// Asynchronously connects to Slack.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that represents the asynchronous connect operation.
        /// </returns>
        private async Task ConnectAsync()
        {
            try
            {
                await _client.OpenAsync(Configuration.SlackConfigurationSection.Current.Client.Token);
            }
            catch (Exception ex)
            {
                OnError(_client, ex);
            }
        }

        /// <summary>
        /// Called when the connection state has changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        async void OnConnectionStateChanged(object sender, EventArgs e)
        {
            if (sender == _client)
            {
                if (
                    _running &&
                    _client.ConnectionState == ClientConnectionState.Disconnected)
                {
                    EventLog.WriteEntry("Connection to Slack lost. Reconnecting in 5 seconds.", EventLogEntryType.Information, 1);
                    await Task.Delay(5000);
                    await ConnectAsync();
                }
                else if (_client.ConnectionState == ClientConnectionState.Established)
                {
                    EventLog.WriteEntry("Connection to Slack established.", EventLogEntryType.Information, 1);
                    if (_isConsole) Console.WriteLine("Connected");
                }
            }
        }

        /// <summary>
        /// Called when a message is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        async void OnMessageReceived(object sender, Message e)
        {
            if (e.Subtype == MessageSubtypes.Message && !e.Hidden)
            {
                foreach (var sink in _sinks)
                {
                    try
                    {
                        if (await sink.ProcessMessageAsync(_client, e) == ChatMessageSinkResult.Complete)
                            return;
                    }
                    catch (Exception ex)
                    {
                        OnError(_client, ex);
                    }
                }
            }
        }

        /// <summary>
        /// Called when an error occurs.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ExceptionEventArgs"/> instance containing the event data.</param>
        void OnError(object sender, ExceptionEventArgs e)
        {
            if (_isConsole)
            {
                Console.Error.WriteLine(e.Exception);
                // Also log to event log so that it's easier to test when debugging...
            }
            EventLog.WriteEntry(e.Exception.ToString(), EventLogEntryType.Error, 1);
        }

        /// <summary>
        /// Executes when a Stop command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service stops running.
        /// </summary>
        protected override void OnStop()
        {
            _running = false;
            if ((int)_client.ConnectionState >= (int)ClientConnectionState.Connecting)
            {
                try
                {
                    _client.CloseAsync().Wait();
                }
                catch (Exception e)
                {
                    OnError(_client, e);
                }
            }
        }
    }
}
