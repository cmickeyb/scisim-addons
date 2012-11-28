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

using System.Collections;
using System.Collections.Generic;

using Dispatcher;
using Dispatcher.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
            
namespace Dispatcher.Handlers
{
    public class InfoHandlers
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
        public InfoHandlers(IConfig config, DispatcherModule dispatcher)
        {
            m_dispatcher = dispatcher;

            m_dispatcher.RegisterPreOperationHandler(typeof(InfoRequest),InfoRequestHandler);
            m_dispatcher.RegisterPreOperationHandler(typeof(MessageFormatRequest),MessageFormatRequestHandler);
            m_dispatcher.RegisterMessageType(typeof(InfoResponse));
            m_dispatcher.RegisterMessageType(typeof(MessageFormatResponse));
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
        
#endregion

#region ScriptInvocationInteface

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        protected ResponseBase InfoRequestHandler(RequestBase irequest)
        {
            if (irequest.GetType() != typeof(InfoRequest))
            {
                m_log.WarnFormat("[InfoHandler] wrong type");
                return new ResponseBase(ResponseCode.Failure,"wrong type");
            }
            
            List<string> scenes = new List<string>();
            foreach (KeyValuePair<string,Scene> kvp in m_sceneCache)
                scenes.Add(kvp.Key);
            
            List<string> msgs = new List<string>();
            foreach (KeyValuePair<string,OperationHandler> mvp in m_dispatcher.HandlerRegistry)
                msgs.Add(mvp.Key);

            return new InfoResponse(m_dispatcher.AsyncBinding,m_dispatcher.SyncBinding,scenes,msgs);
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        protected ResponseBase MessageFormatRequestHandler(RequestBase irequest)
        {
            if (irequest.GetType() != typeof(MessageFormatRequest))
            {
                m_log.WarnFormat("[InfoHandler] wrong type");
                return new ResponseBase(ResponseCode.Failure,"wrong type");
            }
            
            MessageFormatRequest req = (MessageFormatRequest)irequest;

            Type type = null;
            if (! MessageBase.FindRegisteredTypeByName(req.MessageName, out type))
            {
                m_log.WarnFormat("[InfoHandler] unknown type");
                return new ResponseBase(ResponseCode.Failure,"unknown type");
            }
            
            RequestBase obj = (RequestBase)Activator.CreateInstance(type);
            return new MessageFormatResponse(obj.SerializeToString());
        }
#endregion
    }
}
