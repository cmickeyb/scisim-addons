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
    public class EventHandlers : BaseHandler
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected class EventCallback
        {
            public UUID ObjectID { get; set; }
            public UUID EndPointID { get; set; }
            public UUID RequestID { get; set; }

            public EventCallback(UUID id, UUID endpoint, UUID req)
            {
                ObjectID = id;
                EndPointID = endpoint;
                RequestID = req;
            }
        }
        
        // the object registry is keyed off object id
        Dictionary<UUID,List<EventCallback>> m_objectRegistry = new Dictionary<UUID,List<EventCallback>>();

        // endpoint registry is keyed off endpoint id
        Dictionary<UUID,List<EventCallback>> m_endpointRegistry = new Dictionary<UUID,List<EventCallback>>();

#region ControlInterface
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public EventHandlers(Scene scene, IDispatcherModule dispatcher, string domain) : base(scene,dispatcher,domain)
        {
            m_scene.EventManager.OnObjectGrab += touch_start;
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// need to remove all references to the scene in the subscription
        /// list to enable full garbage collection of the scene object
        /// </summary>
        /// -----------------------------------------------------------------
        public override void UnregisterHandlers()
        {
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(RegisterTouchCallbackRequest));
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(UnregisterTouchCallbackRequest));
            m_dispatcher.UnregisterMessageType(typeof(RegisterTouchCallbackResponse));
            m_dispatcher.UnregisterMessageType(typeof(TouchCallback));
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Called when all modules have been added for a region. This is 
        /// where we hook up events
        /// </summary>
        // -----------------------------------------------------------------
        public override void RegisterHandlers()
        {
            m_log.DebugFormat("[EventHandlers] register methods");
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(RegisterTouchCallbackRequest),RegisterTouchCallbackHandler);
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(UnregisterTouchCallbackRequest),UnregisterTouchCallbackHandler);
            m_dispatcher.RegisterMessageType(typeof(RegisterTouchCallbackResponse));
            m_dispatcher.RegisterMessageType(typeof(TouchCallback));
        }
#endregion

#region ScriptInvocationInteface
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase RegisterTouchCallbackHandler(Dispatcher.Messages.RequestBase irequest)
        {
            if (irequest.GetType() != typeof(RegisterTouchCallbackRequest))
                return OperationFailed("wrong type");

            RegisterTouchCallbackRequest request = (RegisterTouchCallbackRequest)irequest;

            // Get the object and register a handler for it
            SceneObjectGroup sog = m_scene.GetSceneObjectGroup(request.ObjectID);
            if (sog == null)
                return OperationFailed(String.Format("no such object; {0}",request.ObjectID));

            if (sog.UUID != request.ObjectID)
                return OperationFailed("touch callback must be registered for root prim");

            UUID requestID = UUID.Random();
            sog.RootPart.SetScriptEvents(requestID,(int)scriptEvents.touch);

            // Create the event callback structure
            EventCallback cb = new EventCallback(request.ObjectID,request.EndPointID,requestID);

            // Add it to the object registry for handling the touch events
            lock (m_objectRegistry)
            {
                if (! m_objectRegistry.ContainsKey(request.ObjectID))
                    m_objectRegistry.Add(request.ObjectID,new List<EventCallback>());
                m_objectRegistry[request.ObjectID].Add(cb);
            }

            // Add it to the endpoint registry for handling changes in the endpoint state
            lock (m_endpointRegistry)
            {
                if (! m_endpointRegistry.ContainsKey(request.EndPointID))
                {
                    m_endpointRegistry.Add(request.EndPointID,new List<EventCallback>());
                
                    // Only need to register the handler for the first request for this endpoint
                    EndPoint ep = m_dispatcher.LookupEndPoint(request.EndPointID);
                    ep.AddCloseHandler(this.EndPointCloseHandler);
                }
                m_endpointRegistry[request.EndPointID].Add(cb);
            }
            
            m_log.DebugFormat("[EventHandlers] registered touch callback for {0}",request.ObjectID);
            return new RegisterTouchCallbackResponse(requestID);
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase UnregisterTouchCallbackHandler(Dispatcher.Messages.RequestBase irequest)
        {
            if (irequest.GetType() != typeof(UnregisterTouchCallbackRequest))
                return OperationFailed("wrong type");

            UnregisterTouchCallbackRequest request = (UnregisterTouchCallbackRequest)irequest;

            // Find the EventCallback structure
            EventCallback cb = null;
            lock (m_objectRegistry)
            {
                List<EventCallback> cblist = null;
                if (! m_objectRegistry.TryGetValue(request.ObjectID, out cblist))
                    return OperationFailed(String.Format("no handler for requested object; {0}",request.ObjectID));

                cb = cblist.Find(delegate(EventCallback test) { return test.RequestID == request.RequestID; });
                if (cb == null)
                    return OperationFailed(String.Format("invalid request id; {0}",request.RequestID));

                cblist.Remove(cb);
                if (cblist.Count == 0)
                    m_objectRegistry.Remove(request.ObjectID);
            }
            
            lock (m_endpointRegistry)
            {
                List<EventCallback> cblist = null;
                if (m_endpointRegistry.TryGetValue(cb.EndPointID,out cblist))
                {
                    cblist.Remove(cb);
                    if (cblist.Count == 0)
                    {
                        EndPoint ep = m_dispatcher.LookupEndPoint(cb.EndPointID);
                        ep.RemoveCloseHandler(this.EndPointCloseHandler);

                        m_endpointRegistry.Remove(cb.EndPointID);
                    }
                }
            }

            // Remove the touch event callback from the SOG
            SceneObjectGroup sog = m_scene.GetSceneObjectGroup(request.ObjectID);
            if (sog == null)
                return OperationFailed(String.Format("no such object; {0}",request.ObjectID));

            sog.RootPart.RemoveScriptEvents(cb.RequestID);

            m_log.DebugFormat("[EventHandlers] unregistered touch callback for {0}",request.ObjectID);
            return new Dispatcher.Messages.ResponseBase(ResponseCode.Success,"");
        }
        

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public void touch_start(uint localID, uint originalID, Vector3 offsetPos,
                IClientAPI remoteClient, SurfaceTouchEventArgs surfaceArgs)
        {
            m_log.DebugFormat("[EventHandlers] Touch event received");

            // Find the object UUID associated with the localID/originalID
            UUID objectID;
            if (originalID == 0)
            {
                SceneObjectPart part = m_scene.GetSceneObjectPart(localID);
                if (part == null)
                    return;
                objectID = part.ParentGroup.UUID;
            }
            else
            {
                SceneObjectPart part = m_scene.GetSceneObjectPart(originalID);
                if (part == null)
                    return;
                objectID = part.ParentGroup.UUID;
            }

            // And grab the callbacks we need to send
            List<EventCallback> cblist = null;
            lock (m_objectRegistry)
            {
                if (! m_objectRegistry.TryGetValue(objectID, out cblist))
                    return;
            }

            // Send the callback
            TouchCallback touch = new RemoteControl.Messages.TouchCallback(objectID,UUID.Zero,remoteClient.AgentId,offsetPos,surfaceArgs);
            foreach (EventCallback cb in cblist)
            {
                EndPoint ep = m_dispatcher.LookupEndPoint(cb.EndPointID);
                if (ep == null)
                {
                    m_log.WarnFormat("[EventHandlers] unable to locate endpoint {0}",cb.RequestID);
                    return;
                }

                touch.RequestID = cb.RequestID;
                ep.Send(touch);
            }
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public void EndPointCloseHandler(UUID endpointID)
        {
            m_log.WarnFormat("[EventHandlers] endpoint close event for {0}",endpointID);

            // The endpoint is being closed, remove all subscriptions that point to it
            // and remove the endpoint itself from our subscription list

            // Get the list of subscriptions that point to this endpoint and
            // clean up the endpoint list
            List<EventCallback> cblist = null;
            lock (m_endpointRegistry)
            {
                if (! m_endpointRegistry.TryGetValue(endpointID, out cblist))
                    return;

                m_endpointRegistry.Remove(endpointID);
            }
            
            // Go through the object index and remove the callbacks
            // from each of them
            lock (m_objectRegistry)
            {
                foreach (EventCallback cb in cblist)
                {
                    // And remove it from the object registry
                    List<EventCallback> olist = null;
                    if (m_objectRegistry.TryGetValue(cb.ObjectID, out olist))
                    {
                        olist.Remove(cb);
                        if (olist.Count == 0)
                            m_objectRegistry.Remove(cb.ObjectID);
                    }
                }
            }

            // For each of the subscription, remove the touch event handler
            foreach (EventCallback cb in cblist)
            {
                // Remove the touch event callback from the SOG
                SceneObjectGroup sog = m_scene.GetSceneObjectGroup(cb.ObjectID);
                if (sog != null)
                    sog.RootPart.RemoveScriptEvents(cb.RequestID);
            }
        }
#endregion
    }
}
