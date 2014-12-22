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
            
namespace Dispatcher
{
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    public class EndPoint
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public UUID EndPointID;
        
        public String Host { get; set; }
        public int Port { get; set; }

        public int LastRenewTime { get; set; } // tick count in milliseconds
        public int LifeSpan { get; set; } // in seconds

        // timeout variables
        public delegate void OnCloseHandler(UUID endpoint);

        private List<OnCloseHandler> m_closeRegistry = new List<OnCloseHandler>();

        // local network information
        private IPEndPoint m_endpoint;

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public EndPoint(UUID id, string host, int port)
        {
            EndPointID = id;
            Host = host;
            Port = port;
            LastRenewTime = 0;
            LifeSpan = 0;
            
            IPHostEntry hostinfo = Dns.Resolve(Host);
            if (hostinfo.AddressList.Length == 0)
            {
                m_log.WarnFormat("[Dispatcher] unable to resolve host {0}",Host);
                return;
            }

            IPAddress addr = hostinfo.AddressList[0];
            m_endpoint = new IPEndPoint(addr,Port);
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public bool AddCloseHandler(OnCloseHandler handler)
        {
            lock (m_closeRegistry)
                m_closeRegistry.Add(handler);

            return true;
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public bool RemoveCloseHandler(OnCloseHandler handler)
        {
            lock (m_closeRegistry)
                return m_closeRegistry.Remove(handler);
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public void TriggerCloseHandlers()
        {
            m_log.WarnFormat("[EndPointHandlers]: Trigger on close for endpoint {0}",EndPointID);

            lock (m_closeRegistry)
            {
                foreach (OnCloseHandler handler in m_closeRegistry)
                {
                    try
                    {
                        handler(EndPointID);
                    }
                    catch (Exception e)
                    {
                        m_log.WarnFormat("[EndPointHandlers]: Delegate failed; {0}", e.Message);
                        // Keep going
                    }
                }
            }
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public void Send(Dispatcher.Messages.CallbackBase msg)
        {
            try
            {
                String data = msg.SerializeToString();
                byte[] buffer = Encoding.ASCII.GetBytes(data);

                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sock.SendTo(buffer, m_endpoint);

                //m_log.WarnFormat("[Dispatcher] sent {0} to {1}:{2}",data,hostname,port);
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[Dispatcher] send failed; {0}",e.Message);
            }
        }
    }
}
