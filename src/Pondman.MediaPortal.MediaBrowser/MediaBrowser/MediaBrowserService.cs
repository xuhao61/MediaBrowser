﻿using System;
using System.Net;
using System.Reflection;
using System.Threading;
using MediaBrowser.ApiInteraction;
using MediaBrowser.ApiInteraction.WebSocket;
using MediaBrowser.Model.ApiClient;
using MediaBrowser.Model.System;
using MediaPortal.ExtensionMethods;
using Pondman.MediaPortal.MediaBrowser.Events;
using Pondman.MediaPortal.MediaBrowser.GUI;
using Pondman.MediaPortal.MediaBrowser.Models;

namespace Pondman.MediaPortal.MediaBrowser
{
    public class MediaBrowserService : IMediaBrowserService
    {
        
        #region Private variables

        private readonly ServerLocator _locator;
        private readonly ILogger _logger;
        private readonly MediaBrowserPlugin _plugin;
        private bool _disposed;
        private Timer _retryTimer;

        #endregion

        public MediaBrowserService(MediaBrowserPlugin plugin, ILogger logger = null)
        {
            _locator = new ServerLocator();
            _logger = logger ?? NullLogger.Instance;
            _plugin = plugin;
            _logger.Info("MediaBrowserService initialized.");
        }

        #region Events

        public event EventHandler<ServerChangedEventArgs> ServerChanged;

        public event EventHandler<SystemInfoChangedEventArgs> SystemInfoChanged;

        #endregion

        public MediaBrowserPlugin Plugin
        {
            get { return _plugin; }
        }

        /// <summary>
        ///     Gets or sets the endpoint of the server.
        /// </summary>
        /// <value>
        ///     The server endpoint.
        /// </value>
        public IPEndPoint Server
        {
            get { return _endpoint; }
            set
            {
                _endpoint = value;
                OnServerChanged(_endpoint);
            }
        } IPEndPoint _endpoint;

        public bool IsServerLocated
        {
            get { return (Server != null); }
        }

        public SystemInfo System
        {
            get { return _systemInfo; }
            internal set
            {
                _systemInfo = value;
                SystemInfoChanged.FireEvent(this, new SystemInfoChangedEventArgs(_systemInfo));
            } 
        } SystemInfo _systemInfo;

        public MediaBrowserClient Client
        {
            get { return _client; }
            internal set
            {
                _client = value;
                ApiWebSocket.Create(_client, OnConnecting, _logger.Error);
                Update();
            }
        } MediaBrowserClient _client;

        public void Discover(int retryIntervalMs = 60000)
        {
            if (_retryTimer != null)
            {
                _retryTimer.Dispose();
            }

            _retryTimer = new Timer(x =>
            {
                _logger.Info("Discovering Media Browser Server.");
                _locator.FindServer(OnServerDiscovered);
            }, null, 0, retryIntervalMs);
        }

        public void Update()
        {
            if (!IsServerLocated) return;
            Client.GetSystemInfo(info => System = info, _logger.Error);
        }

        #region internal event handlers

        protected void OnConnecting(ApiWebSocket socket)
        {
            _logger.Info("Connecting to Media Browser Server.");

            socket.MessageCommand += OnSocketMessageCommand;
            socket.PlayCommand += OnPlayCommand;
            socket.BrowseCommand += OnBrowseCommand;
            socket.Connected += OnSocketConnected;
            socket.Disconnected += OnSocketDisconnected;
        }

        void OnSocketConnected(object sender, EventArgs e)
        {
            _logger.Info("Connected to Media Browser Server.");
        }

        void OnSocketDisconnected(object sender, EventArgs e)
        {
            _logger.Info("Lost connection with Media Browser Server.");
        }

        void OnSocketMessageCommand(object sender, MessageCommandEventArgs e)
        {
            _logger.Debug("Message: {0}", e.Request.Text);
        }

        protected void OnServerDiscovered(IPEndPoint endpoint)
        {
            if (_retryTimer == null) return;

            _retryTimer.Dispose();
            _retryTimer = null;

            _logger.Info("Found MediaBrowser Server: {0}", endpoint);
            Server = endpoint;
        }

        protected void OnServerChanged(IPEndPoint endpoint)
        {
            _logger.Debug("Creating Media Browser client.");
            var client = new MediaBrowserClient(
                            endpoint.Address.ToString(),
                            endpoint.Port,
                            Environment.OSVersion.VersionString,
                            Environment.MachineName,
                            Plugin.Version.ToString()
                            );
            Client = client;
            ServerChanged.FireEvent(this, new ServerChangedEventArgs(endpoint));
        }

        // todo: move command handlers to GUI code

        protected void OnPlayCommand(object sender, PlayRequestEventArgs args)
        {
            // todo: support multiple ids
            _logger.Info("Remote Play Request: Id={1}, StartPositionTicks={2}", args.Request.ItemIds[0],
                args.Request.StartPositionTicks);
            var resumeTime = (int)TimeSpan.FromTicks(args.Request.StartPositionTicks ?? 0).TotalSeconds;

            GUICommon.Window(MediaBrowserWindow.Details, MediaBrowserMedia.Play(args.Request.ItemIds[0], resumeTime));
        }

        protected void OnBrowseCommand(object sender, BrowseRequestEventArgs args)
        {
            _logger.Info("Remote Browse Request: Type={0}, Id={1}, Name={2}", args.Request.ItemType, args.Request.ItemId,
                args.Request.ItemName);

            switch (args.Request.ItemType)
            {
                case "Movie":
                    GUICommon.Window(MediaBrowserWindow.Details, MediaBrowserMedia.Browse(args.Request.ItemId));
                    return;
                default:
                    GUICommon.Window(MediaBrowserWindow.Main,
                        new MediaBrowserItem
                        {
                            Id = args.Request.ItemId,
                            Type = args.Request.ItemType,
                            Name = args.Request.ItemName
                        });
                    return;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (_retryTimer != null)
                    _retryTimer.Dispose();
                
                if (Client != null && Client.WebSocketConnection != null)
                    Client.WebSocketConnection.Dispose();

                _logger.Info("MediaBrowserService shutdown.");
            }

            _disposed = true;
        }

        #endregion

    }
}