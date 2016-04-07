﻿using System;
using System.Collections.Generic;
using Abc.Zebus.Persistence.Matching;
using Abc.Zebus.Persistence.Storage;
using Abc.Zebus.Transport;

namespace Abc.Zebus.Persistence
{
    public class MessageReplayerRepository : IMessageReplayerRepository
    {
        private readonly Dictionary<PeerId, IMessageReplayer> _activeMessageReplayers = new Dictionary<PeerId, IMessageReplayer>();
        private readonly IPersistenceConfiguration _persistenceConfiguration;
        private readonly IStorage _storage;
        private readonly IBus _bus;
        private readonly ITransport _transport;
        private readonly IInMemoryMessageMatcher _inMemoryMessageMatcher;
        private readonly IReporter _speedReporter;
        private bool _messageReplayersEnabled = true;

        public MessageReplayerRepository(IPersistenceConfiguration persistenceConfiguration, IStorage storage, IBus bus, ITransport transport,
                                         IInMemoryMessageMatcher inMemoryMessageMatcher, IReporter speedReporter)
        {
            _persistenceConfiguration = persistenceConfiguration;
            _storage = storage;
            _bus = bus;
            _transport = transport;
            _inMemoryMessageMatcher = inMemoryMessageMatcher;
            _speedReporter = speedReporter;
        }

        public bool HasActiveMessageReplayers()
        {
            lock (_activeMessageReplayers)
            {
                return _activeMessageReplayers.Count != 0;
            }
        }

        public IMessageReplayer CreateMessageReplayer(Peer peer, Guid replayId)
        {
            lock (_activeMessageReplayers)
            {
                ThrowIfDeactivated();
            }

            return new MessageReplayer(_persistenceConfiguration, _storage, _bus, _transport, _inMemoryMessageMatcher, peer, replayId, _speedReporter);
        }

        public void DeactivateMessageReplayers()
        {
            _messageReplayersEnabled = false;
        }

        public IMessageReplayer GetActiveMessageReplayer(PeerId peerId)
        {
            lock (_activeMessageReplayers)
            {
                IMessageReplayer value;
                return _activeMessageReplayers.TryGetValue(peerId, out value) ? value : default(IMessageReplayer);
            }
        }

        public void SetActiveMessageReplayer(PeerId peerId, IMessageReplayer messageReplayer)
        {
            lock (_activeMessageReplayers)
            {
                ThrowIfDeactivated();

                _activeMessageReplayers[peerId] = messageReplayer;
                messageReplayer.Stopped += () => RemoveActiveMessageReplayer(peerId);
            }
        }

        private void ThrowIfDeactivated()
        {
            if (!_messageReplayersEnabled)
                throw new InvalidOperationException("Message replayers are deactivated");
        }

        private void RemoveActiveMessageReplayer(PeerId peerId)
        {
            lock (_activeMessageReplayers)
            {
                _activeMessageReplayers.Remove(peerId);
            }
        }
    }
}