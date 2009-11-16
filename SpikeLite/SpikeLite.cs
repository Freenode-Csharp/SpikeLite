/**
 * SpikeLite C# IRC Bot
 * Copyright (c) 2009 FreeNode ##Csharp Community
 * 
 * This source is licensed under the terms of the MIT license. Please see the 
 * distributed license.txt for details.
 */

using System;
using System.Threading;
using log4net.Ext.Trace;
using Sharkbite.Irc;
using SpikeLite.AccessControl;
using SpikeLite.Communications;
using SpikeLite.Modules;
using SpikeLite.Communications.IRC;
using Mono.Rocks;

namespace SpikeLite
{
    /// <summary>
    /// The bot engine, just call "start" and you're off.
    /// </summary>
    public class SpikeLite
    {
        #region Data Members

        /// <summary>
        /// Stores the bot's connection status. 
        /// </summary>
        private BotStatus _botStatus = BotStatus.Stopped;

        /// <summary>
        /// Holds an instance of a <see cref="Connection"/> object, useful for binding to events.
        /// </summary>
        private Connection _connection;

        /// <summary>
        /// Holds our connection arguments.
        /// </summary>
        /// 
        /// <remarks>
        /// This is partially a hack to get around the behavior described in the remarks for <see cref="Connect"/>.
        /// We'll need to fix this to make the bot multi-network.
        /// </remarks>
        private ConnectionArgs _connectionArgs;

        /// <summary>
        /// This holds the instace of the logger we use for spamming whatever appender we so desire.
        /// </summary>
        private readonly TraceLogImpl _logger = (TraceLogImpl)TraceLogManager.GetLogger(typeof(SpikeLite));

        #endregion

        #region Properties

        /// <summary>
        /// Gets our connection status.
        /// </summary>
        /// 
        /// <remarks>
        /// This will need to be refactored for multi-network support.
        /// </remarks>
        private bool IsConnected
        {
            get { return _connection != null && _connection.Connected; }
        }

        /// <summary>
        /// Gets or sets the authentication module that we're using. This is usually injected via our IoC container.
        /// This must not be null.
        /// </summary>
        public IAuthenticationModule AuthenticationManager { get; set; }

        /// <summary>
        /// Gets or sets the communications manager we're using. This is usually injected via our IoC container. This
        /// must not be null.
        /// </summary>
        public CommunicationManager CommunicationManager { get; set; }

        /// <summary>
        /// Gets or sets our module manager we're using. This is usually injected via our IoC container. This
        /// must not be null.
        /// </summary>
        public ModuleManager ModuleManager { get; set; }

        /// <summary>
        /// Gets or sets whether the bot is set to identify itself with some sort of services agent or other infrastructure.
        /// </summary>
        /// 
        /// <remarks>
        /// Kog: 11/15/2009 - This needs to be refactored for multi-network support, we should fold it into our network-aware context when we build said functionality.
        /// </remarks>
        public bool SupportsIdentification { get; set; }

        #endregion

        #region Public Behavior

        /// <summary>
        /// Attempt to initialize our subsystems and spin up IO.
        /// </summary>
        public void Start()
        {
            // TODO: Kog 11/15/2009 - Refactor for multi-network support.

            // Make sure we're not trying to double start.
            if (_botStatus != BotStatus.Stopped)
            {
                throw new Exception(string.Format("Current BotStatus is : '{0}'. To start the bot the BotStatus must be 'Stopped'.", _botStatus));
            }

            Network network = CommunicationManager.NetworkList[0];
            Server server = network.ServerList[0];

            _connectionArgs = new ConnectionArgs
            {
                Hostname = server.Host,
                Nick = network.BotNickname,
                Port = server.Port ?? 6667,
                RealName = network.BotRealname,
                UserName = network.BotUsername
            };

            _botStatus = BotStatus.Starting;

            // Attempt to connect.
            ModuleManager.LoadModules();
            Connect();
            CommunicationManager.Connection = _connection;

            // Alright, we're cooking now.
            _botStatus = BotStatus.Started;
        }

        // TODO: Kog 12/25/2008 - Do we really need this?

        /// <summary>
        /// Kill all our modules, stop all our IPC.
        /// </summary>    
        public void Shutdown()
        {
            _botStatus = BotStatus.Stopped;
        }

        /// <summary>
        /// Disconnect from our networks. 
        /// </summary>
        /// 
        /// <param name="quitMessage">Quit message to send to IRCD. Cannot be <b>null</b> or <b>empty</b>.</param>
        /// 
        /// <remarks>
        /// This will perform a no-op if you are not already connected.
        /// </remarks>
        public void Quit(string quitMessage)
        {
            if (IsConnected)
            {
                _connection.Disconnect(quitMessage);
            }
        }

        /// <summary>
        /// Attempt to connect to our pre-configured network. Does nothing if you're already connected.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// This will need to be refactored to allow multi-network support. This will perform a no-op
        /// if you are already connected.
        /// </para>
        ///
        /// <para>
        /// Unfortunately there's a rather annoying bug with Thresher's IRC library: when you re-use the same
        /// connection object, it maintains a cache of capability options, as sent as part of the registration
        /// preamble from the IRCD. There's no way (so far) to flush this cache, and it's not a very intelligent
        /// cache - it doesn't look for hits, only misses - so we wind up trying to cache something twice and
        /// an exception resulting from said behavior.
        /// </para>
        /// 
        /// <para>
        /// So... In order to support a reconnect ability you need to create a NEW connection object. This requires
        /// unsubscribing your old event handlers (to prevent memory leaks), and re-registering them. Painful duplication,
        /// crappy mixing of responsibility... name your reason why this is bad. Unfortunately there's not much choice.
        /// </para>
        /// 
        /// </remarks>
        private void Connect()
        {
            if (!IsConnected)
            {
                _connection = new Connection(_connectionArgs, true, true);

                // Subscribe our events
                _connection.Listener.OnRegistered += OnRegister;
                _connection.Listener.OnPrivateNotice += OnPrivateNotice;
                _connection.OnRawMessageReceived += OnRawMessageReceived;
                _connection.OnRawMessageSent += OnRawMessageSent;

                _connection.Connect();

                // Make sure we don't get any random disconnected events before we're connected
                _connection.Listener.OnDisconnected += Listener_OnDisconnect;
            }
        }
 
        #endregion 

        #region Events

        /// <summary>
        /// We're sending a message. 
        /// </summary>
        /// 
        /// <param name="message">The text of the message sent to the IRCD.</param>
        /// 
        /// <remarks>
        /// The message text will be unformatted, as sent by the IRCD. May be RFC1459 compliant. 
        /// </remarks>
        private void OnRawMessageSent(string message)
        {
            if (_logger.IsTraceEnabled)
            {
                _logger.TraceFormat("Sent: {0}", message);    
            }
        }

        /// <summary>
        /// The IRCD has sent us a message that isn't a PRIVMSG or NOTICE.
        /// </summary>
        /// 
        /// <param name="message">The text of the message sent by the IRCD.</param>
        /// 
        /// <remarks>
        /// The message text will be unformatted, as sent by the IRCD. May be RFC1459 compliant. 
        /// </remarks>
        private void OnRawMessageReceived(string message)
        {
            if (_logger.IsTraceEnabled)
            {
                _logger.TraceFormat("Received: {0}", message);
            }
        }

        // TODO: Kog 11/15/2009 - Refactor the following behavior to be network specific.

        /// <summary>
        /// The IRCD has notified us that "registration" is complete, and the MOTD has finished being sent.
        /// </summary>
        /// 
        /// <remarks>
        /// This is a good place to send your services registration. Some networks provide cloaks for users that are best
        /// set up upon connect. Consider this an analog to mIRC's onConnect. The bot currently blocks on a response, see
        /// <see cref="OnPrivateNotice"/>.
        /// </remarks>
        private void OnRegister()
        {
            if (SupportsIdentification)
            {
                // TODO: Kog 11/15/2009 - Refactor this into an action<string, string, string> and wire it into another module. Perhaps we should have the frontend-console
                // TODO: Kog 11/15/2009 - do this.
                _connection.Sender.PrivateMessage("nickserv", String.Format("identify {0}", CommunicationManager.NetworkList[0].AccountPassword));
            }
            else
            {
                // Don't bother blocking, we don't expect to identify.
                JoinChannelsForNetwork();    
            }
        }

        /// <summary>
        /// We've recieved a NOTICE from the server.
        /// </summary>
        /// 
        /// <param name="user">A user representing the sender.</param>
        /// <param name="notice">The message being NOTICE'd.</param>
        /// 
        /// <remarks>
        /// We currently block upon connect between registration and receiving an authentication response. This is so that the
        /// real hostmask of the bot will not be revealed inadvertently. 
        /// </remarks>
        private void OnPrivateNotice(UserInfo user, string notice)
        {
            // Log the notice if we're set to the proper level.
            if (_logger.IsTraceEnabled)
            {
                _logger.TraceFormat("{0} {1} sent a NOTICE: {2}", user.Nick, user.Hostname, notice);
            }   

            // If we support identification of some sort, we need to block until we receive confirmation of successful identification from services agents.
            // This keeps us from joining a channel before we have a vanity host.
            if (SupportsIdentification && NoticeIsExpectedServicesAgentMessage(user, notice))
            {
                JoinChannelsForNetwork();
            }         
        }

        /// <summary>
        /// A convenience method for checking if we've received the notice we're blocking on from a services agent.
        /// </summary>
        /// 
        /// <param name="notice">The notice to digest.</param>
        /// <param name="user">The user sending the notice.</param>
        /// 
        /// <returns>True if we've been responded to by a services agent, else false.</returns>
        private bool NoticeIsExpectedServicesAgentMessage(UserInfo user, string notice)
        {
            // Make sure the message is coming from NickServ
            if (user.Nick.Equals("nickserv", StringComparison.OrdinalIgnoreCase))
            {
                return notice.StartsWith("You are now identified for", StringComparison.OrdinalIgnoreCase) ||
                       (notice.StartsWith("The nickname", StringComparison.OrdinalIgnoreCase) && notice.EndsWith("is not registered", StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }

        /// <summary>
        /// Attempts to join all configured channels.
        /// </summary>
        private void JoinChannelsForNetwork()
        {
            CommunicationManager.NetworkList[0].ServerList[0].ChannelList.ForEach(channel =>
            {
                CommunicationManager.JoinChannel(channel.Name);
                _logger.InfoFormat("joining {0}...", channel.Name);
            });
        }

        /// <summary>
        /// Handle disconnection. 
        /// </summary>
        /// 
        /// <remarks>
        /// We currently attempt to reconnect 5 times, given a 60 second stagger between connection attempts.
        /// </remarks>
        private void Listener_OnDisconnect()
        {
            // TODO: Kog 12/26/2008 - Rewrite this to work better with multiple networks, and with the implementation
            // TODO:                  details suggested on the Server class docs.

            int reconnectCount = 1;

            // Disconnect our event sources... see docs on the method, or on Connect.
            UnwireEvents();

            // Let's not reconnect when we're shutting down...
            if (_botStatus == BotStatus.Started)
            {
                // Try and reconnect.
                do
                {
                    Connect();

                    if (!IsConnected)
                    {
                        _logger.WarnFormat("Failed whilst attempting reconnection attempt #{0}", reconnectCount);

                        // Because of the do-while we end up with n+1 sleeps... not a big deal, but just remember.
                        Thread.Sleep(60000);
                        reconnectCount++;
                    }
                }
                while (!IsConnected && reconnectCount <= 5);

                // If we went through all the reconnect attempts, and we're still not getting anywhere kill the bot
                if (!IsConnected)
                {
                    Shutdown();
                }
            }
        }

        /// <summary>
        /// Remove the wiring for all our event sources. Please see the documentation on <see cref="Connect"/> for details.
        /// </summary>
        private void UnwireEvents()
        {
            _connection.Listener.OnRegistered -= OnRegister;
            _connection.Listener.OnPrivateNotice -= OnPrivateNotice;
            _connection.OnRawMessageReceived -= OnRawMessageReceived;
            _connection.OnRawMessageSent -= OnRawMessageSent;
            _connection.Listener.OnDisconnected -= Listener_OnDisconnect;
        }

        #endregion
    }
}