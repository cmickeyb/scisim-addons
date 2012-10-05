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
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    public class EndPointHandlers
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private DispatcherModule m_dispatcher = null;

        private Dictionary<string,Scene> m_sceneCache = new Dictionary<string,Scene>();

#region ControlInterface
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public EndPointHandlers(IConfig config, DispatcherModule dispatcher)
        {
            m_dispatcher = dispatcher;

            m_dispatcher.RegisterMessageType(typeof(CreateEndPointResponse));
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public void AddScene(Scene scene)
        {
            m_sceneCache.Add(scene.Name,scene);

            m_dispatcher.RegisterOperationHandler(scene,m_dispatcher.Domain,typeof(CreateEndPointRequest),CreateEndPointRequestHandler);
            m_dispatcher.RegisterOperationHandler(scene,m_dispatcher.Domain,typeof(RenewEndPointRequest),RenewEndPointRequestHandler);
            m_dispatcher.RegisterOperationHandler(scene,m_dispatcher.Domain,typeof(CloseEndPointRequest),CloseEndPointRequestHandler);
        }
        
        public void RemoveScene(Scene scene)
        {
            m_sceneCache.Remove(scene.Name);

            m_dispatcher.UnregisterOperationHandler(scene,m_dispatcher.Domain,typeof(CreateEndPointRequest));
            m_dispatcher.UnregisterOperationHandler(scene,m_dispatcher.Domain,typeof(RenewEndPointRequest));
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
        protected ResponseBase CreateEndPointRequestHandler(RequestBase irequest)
        {
            if (irequest.GetType() != typeof(CreateEndPointRequest))
                return OperationFailed("wrong type of request object");

            CreateEndPointRequest request = (CreateEndPointRequest)irequest;

            UUID id = UUID.Random();
            EndPoint ep = new EndPoint(id,request.CallbackHost,request.CallbackPort);

            ep.LastRenewTime = Util.EnvironmentTickCount();
            ep.LifeSpan = Math.Max(request.LifeSpan,m_dispatcher.MaxInterPingTime);

            m_dispatcher.RegisterEndPoint(id,ep);

            return new CreateEndPointResponse(id,ep.LifeSpan);
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        protected ResponseBase RenewEndPointRequestHandler(RequestBase irequest)
        {
            if (irequest.GetType() != typeof(RenewEndPointRequest))
                return OperationFailed("wrong type of request object");

            RenewEndPointRequest request = (RenewEndPointRequest)irequest;

            EndPoint ep = m_dispatcher.LookupEndPoint(request.EndPointID);
            if (ep == null)
                return OperationFailed(String.Format("Unknown endpoint identifier; {0}",request.EndPointID));
            
            ep.LastRenewTime = Util.EnvironmentTickCount();
            ep.LifeSpan = Math.Max(request.LifeSpan,m_dispatcher.MaxInterPingTime);

            return new CreateEndPointResponse(request.EndPointID,ep.LifeSpan);
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        protected ResponseBase CloseEndPointRequestHandler(RequestBase irequest)
        {
            if (irequest.GetType() != typeof(CloseEndPointRequest))
                return OperationFailed("wrong type of request object");

            CloseEndPointRequest request = (CloseEndPointRequest)irequest;

            EndPoint ep = m_dispatcher.LookupEndPoint(request.EndPointID);
            if (ep == null)
                return OperationFailed(String.Format("Unknown endpoint identifier; {0}",request.EndPointID));
            
            try 
            {
                ep.TriggerCloseHandlers();
                m_dispatcher.UnregisterEndPoint(ep.EndPointID);
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[EndPointHandlers] close event failed; {0}",e.Message);
            }

            return new ResponseBase(ResponseCode.Success,"");
        }
#endregion
    }
}
