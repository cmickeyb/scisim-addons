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
using System.Text;
using System.Collections.Generic;

using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
            
using Dispatcher;
using Dispatcher.Messages;

using RemoteControl.Messages;

namespace RemoteControl.Handlers
{
    public class CommunicationHandlers : BaseHandler
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

#region BaseHandler Methods
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public CommunicationHandlers(Scene scene, IDispatcherModule dispatcher, string domain) : base(scene,dispatcher,domain)
        {}

        /// -----------------------------------------------------------------
        /// <summary>
        /// need to remove all references to the scene in the subscription
        /// list to enable full garbage collection of the scene object
        /// </summary>
        /// -----------------------------------------------------------------
        public override void UnregisterHandlers()
        {
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(ChatRequest));
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Called when all modules have been added for a region. This is 
        /// where we hook up events
        /// </summary>
        // -----------------------------------------------------------------
        public override void RegisterHandlers()
        {
            // m_log.WarnFormat("[CommunicationHandlers] register methods");
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(ChatRequest),ChatHandler);
        }
#endregion

#region Communication Handlers
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase ChatHandler(Dispatcher.Messages.RequestBase request)
        {
            if (request.GetType() != typeof(ChatRequest))
                return OperationFailed("wrong type");

            ChatRequest req = (ChatRequest)request;
            OSChatMessage msg = new OSChatMessage();
            
            msg.Scene = m_scene;
            msg.Type = ChatTypeEnum.Say;
            msg.Sender = null;
            msg.SenderUUID = req._UserAccount.PrincipalID;
            msg.From = req._UserAccount.Name;
            msg.Message = req.Message;
            msg.Position = req.Position;
            msg.Channel = req.Channel;

            m_scene.EventManager.TriggerOnChatBroadcast(new Object(), msg);

            return new Dispatcher.Messages.ResponseBase(ResponseCode.Success,"");
        }
#endregion
    }
}
