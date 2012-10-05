/*
 * -----------------------------------------------------------------
 * Copyright (c) 2012 Intel Corporation
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are
 * met:
 * 
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 * 
 *     * Redistributions in binary form must reproduce the above
 *       copyright notice, this list of conditions and the following
 *       disclaimer in the documentation and/or other materials provided
 *       with the distribution.
 * 
 *     * Neither the name of the Intel Corporation nor the names of its
 *       contributors may be used to endorse or promote products derived
 *       from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE INTEL OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 * EXPORT LAWS: THIS LICENSE ADDS NO RESTRICTIONS TO THE EXPORT LAWS OF
 * YOUR JURISDICTION. It is licensee's responsibility to comply with any
 * export regulations applicable in licensee's jurisdiction. Under
 * CURRENT (May 2000) U.S. export regulations this software is eligible
 * for export from the U.S. and can be downloaded by or otherwise
 * exported or reexported worldwide EXCEPT to U.S. embargoed destinations
 * which include Cuba, Iraq, Libya, North Korea, Iran, Syria, Sudan,
 * Afghanistan and any other country to which the U.S. has embargoed
 * goods and services.
 * -----------------------------------------------------------------
 */

using Mono.Addins;

using System;
using System.Reflection;
using System.Threading;
using System.Text;
using System.Net;
using System.Net.Sockets;

using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

using System.Collections;
using System.Collections.Generic;

using Dispatcher;
using Dispatcher.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


using OpenSim.Region.Framework.Scenes.Serialization;
            
namespace Dispatcher.Handlers
{
    public class AuthHandlers
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private int m_maxLifeSpan = 300;
        private bool m_useAuthentication = true;
        private DispatcherModule m_dispatcher = null;
        private Dictionary<string,Scene> m_sceneCache = new Dictionary<string,Scene>();
        private ExpiringCache<UUID,UserAccount> m_authCache = new ExpiringCache<UUID,UserAccount>();

#region ControlInterface
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public AuthHandlers(IConfig config, DispatcherModule dispatcher)
        {
            m_dispatcher = dispatcher;
            m_maxLifeSpan = config.GetInt("MaxLifeSpan",m_maxLifeSpan);
            m_useAuthentication = config.GetBoolean("UseAuthentication",m_useAuthentication);

            m_dispatcher.RegisterPreOperationHandler(typeof(AuthRequest),AuthenticateRequestHandler);
            m_dispatcher.RegisterMessageType(typeof(AuthResponse));
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public void AddScene(Scene scene)
        {
            m_sceneCache.Add(scene.Name,scene);
        }
        
        public void RemoveScene(Scene scene)
        {
            m_sceneCache.Remove(scene.Name);
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public bool AuthorizeRequest(RequestBase irequest)
        {
            if (! m_useAuthentication)
                return true;
            
            UserAccount account;
            if (m_authCache.TryGetValue(irequest._Capability, out account))
            {
                irequest._UserAccount = account;
                return true;
            }
            
            return false;
        }

#endregion

#region ScriptInvocationInteface

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        protected ResponseBase OperationFailed(string msg)
        {
            m_log.WarnFormat("[AuthHandler] {0}",msg);
            return new ResponseBase(ResponseCode.Failure,msg);
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        protected ResponseBase AuthenticateRequestHandler(RequestBase irequest)
        {
            if (irequest.GetType() != typeof(AuthRequest))
                return OperationFailed("wrong type");
            
            AuthRequest request = (AuthRequest)irequest;

            // Get a handle to the scene for the request to be used later
            Scene scene;
            if (! m_sceneCache.TryGetValue(request._Scene, out scene))
                return OperationFailed("no scene specified");

            // Grab the account information and cache it for later use
            UserAccount account = null;
            if (request.UserID != UUID.Zero)
                account = scene.UserAccountService.GetUserAccount(scene.RegionInfo.ScopeID,request.UserID);
            else if (! String.IsNullOrEmpty(request.FirstName) && ! String.IsNullOrEmpty(request.LastName))
                account = scene.UserAccountService.GetUserAccount(scene.RegionInfo.ScopeID,request.FirstName,request.LastName);
            else if (! String.IsNullOrEmpty(request.EmailAddress))
                account = scene.UserAccountService.GetUserAccount(scene.RegionInfo.ScopeID,request.EmailAddress);
            
            if (account == null)
                OperationFailed(String.Format("failed to locate account for user {0}",request.UserID.ToString()));

            // Authenticate the user with the hashed passwd from the request
            if (scene.AuthenticationService.Authenticate(account.PrincipalID,request.HashedPasswd,0) == String.Empty)
                OperationFailed(String.Format("failed to authenticate user {0}",request.UserID.ToString()));
            
            UUID capability = request._Capability == UUID.Zero ? UUID.Random() : request._Capability;
            int lifespan = Math.Min(request.LifeSpan,m_maxLifeSpan);
            m_authCache.AddOrUpdate(capability,account,lifespan);

            return new AuthResponse(capability,lifespan);
        }
#endregion
    }
}
