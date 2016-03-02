// Copyright (c) CodesInChaos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace NetworkSniffer
{
    public abstract class IpSniffer
    {
        public event Action<ArraySegment<byte>> PacketReceived;
        public int? BufferSize { get; set; }

        protected void OnPacketReceived(ArraySegment<byte> data)
        {
            var packetReceived = PacketReceived;
            if (packetReceived != null)
                packetReceived(data);
        }

        private bool _enabled;
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled != value)
                {
                    SetEnabled(value);
                    _enabled = value;
                }
            }
        }
        public abstract IEnumerable<string> Status();

        protected abstract void SetEnabled(bool value);

    }
}
