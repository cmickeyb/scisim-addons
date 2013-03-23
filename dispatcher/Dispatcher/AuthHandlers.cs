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
using OpenSim.Framework.Console;
using OpenSim.Services.Interfaces;

using System.Collections;
using System.Collections.Generic;

using Dispatcher;
using Dispatcher.Messages;
using Dispatcher.Utils;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


using OpenSim.Region.Framework.Scenes.Serialization;
            
namespace Dispatcher.Handlers
{
    public class AuthHandlers
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private ICommandConsole m_console;
        
        public const String MasterDomain = "MASTER";
        
        private int m_maxLifeSpan = 300;
        private bool m_useAuthentication = true;
        private DispatcherModule m_dispatcher = null;
        private List<Scene> m_sceneList = new List<Scene>();
        private Dictionary<string,Scene> m_sceneCache = new Dictionary<string,Scene>();
        private CapabilityCache m_capCache = new CapabilityCache();
        

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

            m_dispatcher.RegisterPreOperationHandler(typeof(CreateCapabilityRequest),CreateCapabilityRequestHandler);
            m_dispatcher.RegisterOperationHandler(m_dispatcher.Domain,typeof(RenewCapabilityRequest),RenewCapabilityRequestHandler);
            m_dispatcher.RegisterOperationHandler(m_dispatcher.Domain,typeof(DestroyCapabilityRequest),DestroyCapabilityRequestHandler);
            m_dispatcher.RegisterMessageType(typeof(CapabilityResponse));

            m_console = MainConsole.Instance;
            m_console.Commands.AddCommand("Dispatcher", false, "dispatcher show caps", "dispatch show capabilities",
                                          "Dump the capabilities table", "", HandleShowCapabilities);
            
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        private void HandleShowCapabilities(string module, string[] cmd)
        {
            ConsoleDisplayTable cdt = new ConsoleDisplayTable();
            cdt.Indent = 2;
            cdt.AddColumn("Capability",36);
            cdt.AddColumn("Expiration",15);
            cdt.AddColumn("User Name",30);
            cdt.AddColumn("Domains",50);

            int now = Util.EnvironmentTickCount();

            List<CapabilityInfo> caps = m_capCache.DumpCapabilities();
            foreach (CapabilityInfo cap in caps)
            {
                int span = (cap.LifeSpan - Util.EnvironmentTickCountSubtract(now,cap.LastRefresh)) / 1000;
                string name = cap.Account.FirstName + ' ' + cap.Account.LastName;
                string time = DateTime.Now.AddSeconds(span).ToLongTimeString();
                
                string doms = "";
                foreach (string s in cap.DomainList)
                    doms += s + ",";

                cdt.AddRow(cap.Capability.ToString(),time,name,doms);
            }
            
            m_console.OutputFormat(cdt.ToString());
        }
        
                
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public void AddScene(Scene scene)
        {
            m_sceneCache.Add(scene.Name,scene);
            m_sceneList = new List<Scene>(m_sceneCache.Values);
        }
        
        public void RemoveScene(Scene scene)
        {
            m_sceneCache.Remove(scene.Name);
            m_sceneList = new List<Scene>(m_sceneCache.Values);
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public bool AuthorizeRequest(RequestBase irequest)
        {
            if (! m_useAuthentication)
                return true;
            
            UserAccount acct;
            HashSet<String> dlist;
            if (m_capCache.GetCapability(irequest._Capability, out acct, out dlist))
            {
                if (irequest._Domain == m_dispatcher.Domain || dlist.Contains(irequest._Domain) || dlist.Contains(MasterDomain))
                {
                    irequest._UserAccount = acct;
                    return true;
                }
            }
            
            return false;
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public bool DestroyDomainCapability(UUID capability)
        {
            return m_capCache.RemoveCapability(capability);
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public UUID CreateDomainCapability(String domain, UUID userid, int span)
        {
            HashSet<string> domainList = new HashSet<String>();
            domainList.Add(domain);
            
            return CreateDomainCapability(domainList,userid,span);
        }
        
        public UUID CreateDomainCapability(HashSet<String> domainList, UUID userid, int span)
        {
            // Sanity check
            if (m_sceneList.Count == 0)
            {
                m_log.ErrorFormat("[AuthHandler] No scenes");
                return UUID.Zero;
            }
            
            // This method is called from region modules that are trusted so we allow any duration
            // of capability lifespan, 

            if (span < 0)
            {
                m_log.WarnFormat("[AuthHandler] idle must be zero or greater; {0}",span);
                return UUID.Zero;
            }
            
            // grab the user account information
            UserAccount acct = m_sceneList[0].UserAccountService.GetUserAccount(m_sceneList[0].RegionInfo.ScopeID,userid);
            if (acct == null)
            {
                m_log.WarnFormat("[AuthHandler] unable to create capability for user {0}",userid);
                return UUID.Zero;
            }

            UUID capability = UUID.Random();
            m_capCache.AddCapability(capability,acct,domainList,span);
            return capability;
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
        protected ResponseBase CreateCapabilityRequestHandler(RequestBase irequest)
        {
            if (irequest.GetType() != typeof(CreateCapabilityRequest))
                return OperationFailed("wrong type");
            
            CreateCapabilityRequest request = (CreateCapabilityRequest)irequest;

            // Get a handle to the scene for the request to be used later
            
            Scene scene;
            
            // if no scene is specified, then we'll just use a random one
            // in theory no scene needs to be set unless the services in each one are different
            if (String.IsNullOrEmpty(request._Scene))
                scene = m_sceneList[0];
            else if (! m_sceneCache.TryGetValue(request._Scene, out scene))
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
                return OperationFailed(String.Format("failed to locate account for user {0}",request.UserID.ToString()));

            // Authenticate the user with the hashed passwd from the request
            if (scene.AuthenticationService.Authenticate(account.PrincipalID,request.HashedPasswd,0) == String.Empty)
                return OperationFailed(String.Format("failed to authenticate user {0}",request.UserID.ToString()));
            
            HashSet<String> dlist = new HashSet<String>(request.DomainList);

            // Clamp the lifespan for externally requested capabilities, region modules can register
            // caps with longer or infinite lifespans
            if (request.LifeSpan <= 0)
                return OperationFailed(String.Format("lifespan must be greater than 0 {0}",request.LifeSpan));
                
            int span = Math.Min(request.LifeSpan,m_maxLifeSpan);

            // add it to the authentication cache
            UUID capability = UUID.Random();
            m_capCache.AddCapability(capability,account,dlist,span);

            return new CapabilityResponse(capability,span);
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        protected ResponseBase RenewCapabilityRequestHandler(RequestBase irequest)
        {
            if (irequest.GetType() != typeof(RenewCapabilityRequest))
                return OperationFailed("wrong type");
            
            RenewCapabilityRequest request = (RenewCapabilityRequest)irequest;

            // Get the domains...
            HashSet<String> dlist = new HashSet<String>(request.DomainList);

            // Clamp the lifespan for externally requested capabilities, region modules can register
            // caps with longer or infinite lifespans
            if (request.LifeSpan <= 0)
                return OperationFailed(String.Format("lifespan must be greater than 0 {0}",request.LifeSpan));
                
            int span = Math.Min(request.LifeSpan,m_maxLifeSpan);
            
            // update the capability cache
            if (! m_capCache.UpdateCapability(request._Capability,dlist,span))
                return OperationFailed(String.Format("unable to update the capability"));
            
            return new CapabilityResponse(request._Capability,span);
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        protected ResponseBase DestroyCapabilityRequestHandler(RequestBase irequest)
        {
            if (irequest.GetType() != typeof(DestroyCapabilityRequest))
                return OperationFailed("wrong type");
            
            DestroyCapabilityRequest request = (DestroyCapabilityRequest)irequest;

            // update the capability cache
            if (! m_capCache.RemoveCapability(request._Capability))
                return OperationFailed(String.Format("unable to remove the capability"));
            
            return new ResponseBase(ResponseCode.Success,"");
        }
#endregion
    }
}
