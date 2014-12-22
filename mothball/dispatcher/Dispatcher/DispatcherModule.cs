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
using System.Timers;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Monitoring;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Dispatcher.Messages;
using Dispatcher.Handlers;

[assembly: Addin("Dispatcher", "0.1")]
[assembly: AddinDependency("OpenSim", "0.5")]

namespace Dispatcher
{
    public class DispatcherStat
    {
        public UInt64 TotalRequests;
        public UInt64 TotalTime;

        public DispatcherStat()
        {
            TotalRequests = 0;
            TotalTime = 0;
        }
    }

    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]

    public class DispatcherModule  : ISharedRegionModule, IDispatcherModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private static string m_separator = "::";

        private static int m_socketTimeout = 10000; // 10 second timeout
        
        private IConfig m_config  = null;
        private bool   m_enabled    = false;

        // these cover configurable properties
        private string m_httppath = "/Dispatcher/";
        private int m_udpport = 45325;
        private string m_host = "";

        private string m_abinding = "";
        public string AsyncBinding { get { return m_abinding; } }
            
        private string m_sbinding = "";
        public string SyncBinding { get { return m_sbinding; } }

        private string m_domain = "Dispatcher";
        public string Domain { get { return m_domain; } }

        private int m_maxInterPingTime = 3000; // milliseconds
        public int MaxInterPingTime { get { return m_maxInterPingTime; } }

        private Queue<RequestBase> m_requestQueue = new Queue<RequestBase>();
        public Queue<RequestBase> RequestQueue { get { return m_requestQueue; } }

        private int m_requestThreads = 0;
        private int m_maxRequestThreads = 2;

        public Dictionary<string,OperationHandler> HandlerRegistry { get { return m_HandlerRegistry; } }
        private Dictionary<string,OperationHandler> m_HandlerRegistry =
            new Dictionary<string,OperationHandler>();

        public Dictionary<string,OperationHandler> PreHandlerRegistry { get { return m_PreHandlerRegistry; } }
        private Dictionary<string,OperationHandler> m_PreHandlerRegistry =
            new Dictionary<string,OperationHandler>();

        private AuthHandlers m_authorizer = null;
        private InfoHandlers m_infohandler = null;
        private EndPointHandlers m_endpointhandler = null;
        private DispatcherCommandModule m_commands = null;
        
        private Dictionary<UUID,EndPoint> m_endpointRegistry = new Dictionary<UUID,EndPoint>();

        private Dictionary<String,DispatcherStat> m_dispatcherStats = new Dictionary<String,DispatcherStat>();
        public Dictionary<String,DispatcherStat> DispatcherStats { get { return m_dispatcherStats; } }

        private System.Timers.Timer m_endpointTimer;
        
#region IRegionModule Members

        // -----------------------------------------------------------------
        /// <summary>
        /// Name of this shared module is it's class name
        /// </summary>
        // -----------------------------------------------------------------
        public string Name
        {
            get { return this.GetType().Name; }
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Initialise this shared module
        /// </summary>
        /// <param name="scene">this region is getting initialised</param>
        /// <param name="source">nini config, we are not using this</param>
        // -----------------------------------------------------------------
        public void Initialise(IConfigSource config)
        {
            try 
            {
                if ((m_config = config.Configs["Dispatcher"]) == null)
                {
                    // There is no configuration, the module is disabled
                    m_log.InfoFormat("[Dispatcher] no configuration info");
                    return;
                }

                m_enabled = m_config.GetBoolean("Enabled", m_enabled);
                if (m_enabled)
                {
                    IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
                    m_host = ips[0].ToString();

                    m_host = m_config.GetString("HostAddress",m_host);
                    m_udpport = m_config.GetInt("UDPPort",m_udpport);
                    m_abinding = String.Format("{0}:{1}",m_host,m_udpport);

                    m_httppath = m_config.GetString("HTTPPath",m_httppath);
                    if (! m_httppath.StartsWith("/"))
                        m_httppath = "/" + m_httppath;
                    if (! m_httppath.EndsWith("/"))
                        m_httppath = m_httppath + "/";
                    m_sbinding = String.Format("http://{0}:{1}{2}",m_host,MainServer.Instance.Port,m_httppath);

                    m_maxRequestThreads = m_config.GetInt("MaxAsyncThreads",m_maxRequestThreads);
                    m_maxInterPingTime = m_config.GetInt("MaxInterPingTime",m_maxInterPingTime);

                    m_authorizer = new AuthHandlers(m_config,this);
                    m_infohandler = new InfoHandlers(m_config,this);
                    m_endpointhandler = new EndPointHandlers(m_config,this);

                    m_commands = new DispatcherCommandModule(m_config,this);
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[Dispatcher] initialization error: {0}",e.Message);
                return;
            }

            m_log.WarnFormat("[Dispatcher] module {0} enabled, udp binding {1}, http binding {2}",
                             (m_enabled ? "is" : "is not"),m_abinding,m_sbinding);
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// all of the shared modules have been loaded and initialized
        /// </summary>
        // -----------------------------------------------------------------
        public void PostInitialise()
        {
            if (m_enabled)
            {
                m_commands.RegionsLoaded();

                // Start the listener thread here
                //Watchdog.StartThread(AsynchronousListenLoop,"Dispatcher",ThreadPriority.Normal,false,true,null,4*m_socketTimeout);
                WorkManager.StartThread(AsynchronousListenLoop,"Dispatcher",ThreadPriority.Normal,false,true,null,4*m_socketTimeout);

                // Add a handler to the HTTP server
                // MainServer.Instance.AddHTTPHandler(m_httppath,HandleSynchronousRequest);

                DispatcherStreamHandler handler = new DispatcherStreamHandler("POST", m_httppath, HandleStreamRequest);
                MainServer.Instance.AddStreamHandler(handler);

                // Start the timeout thread
                m_endpointTimer = new System.Timers.Timer(m_maxInterPingTime/4);

                m_endpointTimer.Enabled = true;
                m_endpointTimer.AutoReset = true;
                m_endpointTimer.Interval = m_maxInterPingTime/4;
                m_endpointTimer.Elapsed += new ElapsedEventHandler(CheckEndPointTimeout);
            }
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Nothing to do on close
        /// </summary>
        // -----------------------------------------------------------------
        public void Close()
        {
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public void AddRegion(Scene scene)
        {
            if (m_enabled)
            {
                m_authorizer.AddScene(scene);
                m_infohandler.AddScene(scene);
                m_endpointhandler.AddScene(scene);
                
                m_commands.AddScene(scene);
                
                scene.RegisterModuleInterface<IDispatcherModule>(this);
            }
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// need to remove all references to the scene in the subscription
        /// list to enable full garbage collection of the scene object
        /// </summary>
        // -----------------------------------------------------------------
        public void RemoveRegion(Scene scene)
        {
            if (m_enabled)
            {
                m_authorizer.RemoveScene(scene);
                m_infohandler.RemoveScene(scene);
                m_endpointhandler.RemoveScene(scene);

                m_commands.RemoveScene(scene);

                scene.UnregisterModuleInterface<IDispatcherModule>(this);
            }
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Called when all modules have been added for a region. This is 
        /// where we hook up events
        /// </summary>
        // -----------------------------------------------------------------
        public void RegionLoaded(Scene scene)
        {
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public Type ReplaceableInterface
        {
            get { return null; }
        }

#endregion

#region IDispatchModule

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public void ResetStats()
        {
            m_dispatcherStats = new Dictionary<String,DispatcherStat>();
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public bool RegisterMessageType(Type messagetype)
        {
            Dispatcher.Messages.MessageBase.RegisterMessageType(messagetype);
            return true;
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public bool UnregisterMessageType(Type messagetype)
        {
            Dispatcher.Messages.MessageBase.UnregisterMessageType(messagetype);
            return true;
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public bool RegisterEndPoint(UUID id, EndPoint ep)
        {
            lock (m_endpointRegistry)
            {
                if (m_endpointRegistry.ContainsKey(id))
                {
                    m_log.WarnFormat("[Dispatcher] duplicate end point registration; {0}",id.ToString());
                    return false;
                }
            
                m_endpointRegistry.Add(id,ep);
            }
                                 
            return true;
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public bool UnregisterEndPoint(UUID id)
        {
            lock (m_endpointRegistry)
            {
                EndPoint ep = null;
                if (m_endpointRegistry.TryGetValue(id,out ep))
                {
                    ep.TriggerCloseHandlers();
                    m_endpointRegistry.Remove(id);

                    return true;
                }
            }
            
            m_log.WarnFormat("[Dispatcher] attempt to remove non-existant endpoint; {0}",id);
            return false;
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public EndPoint LookupEndPoint(UUID id)
        {
            lock (m_endpointRegistry)
            {
                EndPoint ep = null;
                if (m_endpointRegistry.TryGetValue(id, out ep))
                    return ep;
            }
            
            return null;
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public bool RegisterPreOperationHandler(Type messagetype, OperationHandler handler)
        {
            UnregisterPreOperationHandler(messagetype);

            m_PreHandlerRegistry.Add(messagetype.FullName,handler);
            RegisterMessageType(messagetype);

            return true;
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public bool UnregisterPreOperationHandler(Type messagetype)
        {
            if (m_PreHandlerRegistry.ContainsKey(messagetype.FullName))
            {
                m_PreHandlerRegistry.Remove(messagetype.FullName);
                UnregisterMessageType(messagetype);
            }

            return true;
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public bool RegisterOperationHandler(Scene scene, string domain, Type messagetype, OperationHandler handler)
        {
            string opkey = scene.Name + m_separator + domain + m_separator + messagetype.FullName;
            return DoRegisterOperationHandler(opkey,messagetype,handler);
        }

        public bool RegisterOperationHandler(string domain, Type messagetype, OperationHandler handler)
        {
            string opkey = domain + m_separator + messagetype.FullName;
            return DoRegisterOperationHandler(opkey,messagetype,handler);
        }

        private bool DoRegisterOperationHandler(string opkey, Type messagetype, OperationHandler handler)
        {
            m_log.DebugFormat("[Dispatcher] register handler for {0}",opkey);

            // Make sure we get rid of any old versions in either registry
            DoUnregisterOperationHandler(opkey, messagetype);

            m_HandlerRegistry.Add(opkey,handler); // Save it in the handler registry
            RegisterMessageType(messagetype); // And save it in the type registry

            return true;
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public bool UnregisterOperationHandler(Scene scene, string domain, Type messagetype)
        {
            string opkey = scene.Name + m_separator + domain + m_separator + messagetype.FullName;
            return DoUnregisterOperationHandler(opkey,messagetype);
        }

        public bool UnregisterOperationHandler(string domain, Type messagetype)
        {
            string opkey = domain + m_separator + messagetype.FullName;
            return DoUnregisterOperationHandler(opkey,messagetype);
        }


        private bool DoUnregisterOperationHandler(string opkey, Type messagetype)
        {
            if (m_HandlerRegistry.ContainsKey(opkey))
            {
                m_log.DebugFormat("[Dispatcher] unregister handler for {0}",opkey);

                m_HandlerRegistry.Remove(opkey);
                UnregisterMessageType(messagetype);

                return true;
            }
            
            return false;
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public UUID CreateDomainCapability(String scene, HashSet<String> domainList, UUID userid, int lifespan)
        {
            return m_authorizer.CreateDomainCapability(scene,domainList,userid,lifespan);
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public UUID CreateDomainCapability(String scene, String domain, UUID userid, int lifespan)
        {
            return m_authorizer.CreateDomainCapability(scene,domain,userid,lifespan);
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public bool DestroyDomainCapability(UUID capability)
        {
            return m_authorizer.DestroyDomainCapability(capability);
        }
        
#endregion

#region Control Members
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        protected ResponseBase OperationFailed(string msg)
        {
            m_log.WarnFormat("[Dispatcher] {0}",msg);
            return new ResponseBase(ResponseCode.Failure,msg);
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        private void CheckEndPointTimeout(object sender, EventArgs ea)
        {
            Int32 curtick = Util.EnvironmentTickCount();
            List<UUID> timedout = new List<UUID>();
            
            lock (m_endpointRegistry)
            {
                foreach (KeyValuePair<UUID,EndPoint> kvp in m_endpointRegistry)
                {
                    if (Util.EnvironmentTickCountSubtract(curtick,kvp.Value.LastRenewTime) > kvp.Value.LifeSpan)
                        timedout.Add(kvp.Key);
                }
            }
            
            foreach (UUID id in timedout)
                UnregisterEndPoint(id);
            
            m_endpointTimer.Start();
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        private void EnqueueRequest(RequestBase req)
        {
            lock (m_requestQueue)
            {
                // put the request in the queue
                m_requestQueue.Enqueue(req);

                // if there are already enough threads operating here,
                // then there is nothing else to do
                if (m_requestThreads >= m_maxRequestThreads)
                    return;

                // Guess there is work for a new thread
                m_requestThreads++;
            }

            Util.FireAndForget(delegate { ProcessRequestQueue(); });
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        private bool FindHandler(RequestBase req, out OperationHandler handler)
        {
            handler = null;

            string opkey = req._Domain + m_separator + req.GetType().FullName;
            
            // First look for a scene-specific handler
            if (! String.IsNullOrEmpty(req._Scene))
            {
                if (m_HandlerRegistry.TryGetValue(req._Scene + m_separator + opkey, out handler))
                    return true;
            }
            
            // Look for a scene-independent handler
            if (m_HandlerRegistry.TryGetValue(opkey, out handler))
                return true;
                
            return false;
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        private void RecordCompletedOperation(RequestBase req)
        {
            lock (m_dispatcherStats)
            {
                String tname = req.GetType().Name;
                DispatcherStat stat = null;
                
                if (! m_dispatcherStats.TryGetValue(tname, out stat))
                {
                    stat = new DispatcherStat();
                    m_dispatcherStats[tname] = stat;
                }
                
                UInt64 tdiff = (UInt64)Util.EnvironmentTickCountSubtract(req.RequestEntryTime);
                stat.TotalTime += tdiff;
                stat.TotalRequests++;
            }
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        private void ProcessRequestQueue()
        {
            while (true)
            {
                RequestBase req = null;
                lock (m_requestQueue)
                {
                    if (m_requestQueue.Count == 0)
                    {
                        m_requestThreads--;
                        return;
                    }
                    
                    req = m_requestQueue.Dequeue();
                }

                OperationHandler handler;
                if (FindHandler(req, out handler))
                    handler(req);

                RecordCompletedOperation(req);
            }
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        private ResponseBase InvokeHandler(RequestBase req)
        {
            OperationHandler handler;

            // check to see if this is a pre-authenticate request and handle it there, all of these
            // operations must be synchronous
            if (m_PreHandlerRegistry.TryGetValue(req.GetType().FullName, out handler))
                return handler(req);

            // Check to see if we have a valid capability and expand it
            String message;
            if (! m_authorizer.AuthorizeRequest(req, out message))
                return OperationFailed(String.Format("Unauthorized invocation; {0}", message));

            // Find the regular handler
            if (! FindHandler(req,out handler))
                return OperationFailed(String.Format("Unknown message type; {0}",req.GetType().FullName));

            if (req._AsyncRequest)
            {
                // Util.FireAndForget( delegate { handler(req); } );
                EnqueueRequest(req);
                return new ResponseBase(ResponseCode.Queued,"");
            }

            ResponseBase resp = handler(req);
            RecordCompletedOperation(req);
            
            return resp;
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        private void InvokeAsynchronousHandler(string request)
        {
            try
            {
                RequestBase req = RequestBase.DeserializeFromTextData(request);
                if (req == null)
                {
                    OperationFailed("Failed to deserialize request");
                    return;
                }
                
                // This request has to be asynchronous
                req._AsyncRequest = true;
                
                // Don't care about the results
                InvokeHandler(req);
            }
            catch (Exception e)
            {
                OperationFailed(String.Format("Fatal error; {0}",e.Message));
            }
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        private byte[] HandleTextRequest(string domain, Stream tstream, IOSHttpRequest request, IOSHttpResponse response)
        {
            response.ContentType = "application/json";
            response.StatusCode = 200;
            response.KeepAlive = true;
            
            ResponseBase resp = null;
            try
            {
                RequestBase req = RequestBase.DeserializeFromTextStream(tstream);
                if (req == null)
                {
                    resp = OperationFailed("Failed to deserialize request");
                    return Encoding.UTF8.GetBytes(resp.SerializeToString());
                }
                
                // Check to see if this is an authentication request
                // Get a complete domain and find the handler
                if (String.IsNullOrEmpty(req._Domain))
                    req._Domain = domain;
                
                resp = InvokeHandler(req);
            }
            catch (Exception e)
            {
                resp = OperationFailed(String.Format("Fatal error; {0}",e.Message));
            }

            return Encoding.UTF8.GetBytes(resp.SerializeToString());
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        private byte[] HandleBinaryRequest(string domain, Stream bstream, IOSHttpRequest request, IOSHttpResponse response)
        {
            response.ContentType = "application/bson";
            response.StatusCode = 200;
            response.KeepAlive = true;

            ResponseBase resp = null;
            try
            {
                RequestBase req = RequestBase.DeserializeFromBinaryStream(bstream);
                if (req == null)
                {
                    resp = OperationFailed("Failed to deserialize request");
                    return resp.SerializeToBinaryData();
                }
                
                // Check to see if this is an authentication request
                // Get a complete domain and find the handler
                if (String.IsNullOrEmpty(req._Domain))
                    req._Domain = domain;
                
                resp = InvokeHandler(req);
            }
            catch (Exception e)
            {
                resp = OperationFailed(String.Format("Fatal error; {0}",e.Message));
            }

            return resp.SerializeToBinaryData();
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public byte[] HandleStreamRequest(string path, Stream stream, IOSHttpRequest request, IOSHttpResponse response)
        {
            string domain = path.Remove(0,m_httppath.Length);
            m_log.DebugFormat("[Dispatcher] path={0}, domain={1}, content-type={2}",path,domain,request.ContentType);

            switch (request.ContentType)
            {
            case "application/bson" :
                return HandleBinaryRequest(domain,stream,request,response);
                
            case "application/json" :
            case "test/json" :
                return HandleTextRequest(domain,stream,request,response);

            default:
                m_log.WarnFormat("[Dispatcher] request with unhandled content type; {0}", request.ContentType);
                return null;
            }
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        private void AsynchronousListenLoop()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any,m_udpport);
            Socket mListener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mListener.Bind(remoteEndPoint);
            mListener.ReceiveTimeout = m_socketTimeout;
            
            byte[] buffer = new byte[1024];

            while (true)
            {
                try
                {
                    int count = mListener.Receive(buffer,0,buffer.Length,SocketFlags.None);

                    if (count > 0)
                    {
                        string sdata = Encoding.ASCII.GetString(buffer,0,count);
                        Util.FireAndForget( delegate { InvokeAsynchronousHandler(sdata); } );
                    }

                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode != SocketError.TimedOut && ex.SocketErrorCode != SocketError.WouldBlock)
                        m_log.ErrorFormat("[Dispatcher] Socket error {0}",ex.SocketErrorCode);
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat("[Dispatcher] {0}",e.ToString());
                }

                Watchdog.UpdateThread();
            }
        }
    }
#endregion
}
