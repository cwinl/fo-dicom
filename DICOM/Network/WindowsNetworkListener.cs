﻿// Copyright (c) 2012-2017 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using Windows.Networking;

namespace Dicom.Network
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    using Windows.Networking.Sockets;

    /// <summary>
    /// Universal Windows Platform implementation of the <see cref="INetworkListener"/>.
    /// </summary>
    public class WindowsNetworkListener : INetworkListener
    {
        #region FIELDS

        private readonly HostName _ipAddress;

        private readonly string _port;

        private readonly ManualResetEventSlim _handle;

        private StreamSocketListener _listener;

        private StreamSocket _socket;

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsNetworkListener"/> class. 
        /// </summary>
        /// <param name="ipAddress">IP address(es) to listen to.</param>
        /// <param name="port">TCP/IP port to listen to.</param>
        internal WindowsNetworkListener(string ipAddress, int port)
        {
            HostName addr;
            try
            {
                addr = new HostName(ipAddress);
            }
            catch
            {
                addr = new HostName(NetworkManager.IPv4Any);
            }
            _ipAddress = addr;

            _port = port.ToString(CultureInfo.InvariantCulture);
            _handle = new ManualResetEventSlim(false);
        }

        #endregion

        #region METHODS

        /// <inheritdoc />
        public async Task StartAsync()
        {
            _listener = new StreamSocketListener();
            _listener.ConnectionReceived += OnConnectionReceived;

            _socket = null;
            _handle.Reset();
            await _listener.BindEndpointAsync(_ipAddress, _port).AsTask().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Stop()
        {
            _listener.ConnectionReceived -= OnConnectionReceived;
            _listener.Dispose();
            _handle.Set();
        }

        /// <inheritdoc />
        public Task<INetworkStream> AcceptNetworkStreamAsync(
            string certificateName,
            bool noDelay,
            CancellationToken token)
        {
            if (!string.IsNullOrWhiteSpace(certificateName))
            {
                throw new NotSupportedException(
                    "Authenticated server connections not supported on Windows Universal Platform.");
            }

            INetworkStream networkStream;
            try
            {
                _handle.Wait(token);
                networkStream = _socket == null ? null : new WindowsNetworkStream(_socket);
            }
            catch
            {
                networkStream = null;
            }

            _handle.Reset();

            return Task.FromResult(networkStream);
        }

        /// <summary>
        /// Event handler when connection received.
        /// </summary>
        /// <param name="sender">The sender, more specifically the listener object.</param>
        /// <param name="args">The connection received arguments; 
        /// <see cref="StreamSocketListenerConnectionReceivedEventArgs.Socket">Socket</see>/> property is saved for later use.</param>
        private void OnConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            _socket = args.Socket;
            _handle.Set();
        }

        #endregion
    }
}
