﻿/**
 * SpikeLite C# IRC Bot
 * Copyright (c) 2009-2011 FreeNode ##Csharp Community
 * 
 * This source is licensed under the terms of the MIT license. Please see the 
 * distributed license.txt for details.
 */

using SpikeLite.AccessControl;
using SpikeLite.Communications.Irc;

namespace SpikeLite.Communications.Messaging
{
    /// <summary>
    /// A concrete implementation of the <see cref="IPrivmsgParser"/> contract, this class also handles authentication of users.
    /// </summary>
    class PrivmsgParser : IPrivmsgParser
    {
        /// <summary>
        /// Gets or sets a <see cref="CommunicationManager"/> that we can use for distributing our parsed messages to other components
        /// within the system.
        /// </summary>
        public CommunicationManager CommunicationManager { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="IAuthenticationModule"/> implementaiton that handles shoving authentication tokens into our parsed messages.
        /// </summary>
        public IAuthenticationModule AuthHandler { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="UserTokenCache"/> repository of cached user tokens for our <see cref="AuthHandler"/>.
        /// </summary>
        private UserTokenCache UserTokenCache { get; set; }

        public void HandleMultiTargetMessage(User user, string channel, string message)
        {
            var userToken = UserTokenCache.RetrieveToken(user);
            var authToken = AuthHandler.Authenticate(userToken);

            var request = new Request
            {
                RequestFrom = authToken,
                Channel = channel,
                Nick = user.NickName,
                RequestSourceType = RequestSourceType.Public,
                RequestType = Communications.RequestType.Message,
                Addressee = user.NickName,
                Message = message
            };

            CommunicationManager.HandleRequestReceived(request);
        }

        public void HandleSingleTargetMessage(User user, string message)
        {
            var userToken = UserTokenCache.RetrieveToken(user);
            var authToken = AuthHandler.Authenticate(userToken);

            var request = new Request
            {
                RequestFrom = authToken,
                Channel = null,
                Nick = user.NickName,
                RequestSourceType = RequestSourceType.Private,
                RequestType = Communications.RequestType.Message,
                Message = message
            };

            CommunicationManager.HandleRequestReceived(request);
        }

        public void HandleMultiTargetAction(User user, string channel, string message)
        {
            var userToken = UserTokenCache.RetrieveToken(user);
            var authToken = AuthHandler.Authenticate(userToken);

            var request = new Request
            {
                RequestFrom = authToken,
                Channel = channel,
                Nick = user.NickName,
                RequestSourceType = RequestSourceType.Public,
                RequestType = Communications.RequestType.Action,
                Addressee = user.NickName,
                Message = message
            };

            CommunicationManager.HandleRequestReceived(request);
        }

        public void HandleSingleTargetAction(User user, string message)
        {
            var userToken = UserTokenCache.RetrieveToken(user);
            var authToken = AuthHandler.Authenticate(userToken);

            var request = new Request
            {
                RequestFrom = authToken,
                Channel = null,
                Nick = user.NickName,
                RequestSourceType = RequestSourceType.Private,
                RequestType = Communications.RequestType.Action,
                Message = message
            };

            CommunicationManager.HandleRequestReceived(request);
        }
    }
}
