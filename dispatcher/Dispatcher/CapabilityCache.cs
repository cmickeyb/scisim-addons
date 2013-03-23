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
            
namespace Dispatcher.Utils
{
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    public class CapabilityInfo
    {
        public UUID Capability { get; set; }
        public UserAccount Account { get; set; }
        public HashSet<String> DomainList { get; set; }

        public int LastRefresh { get; set; }
        public int LifeSpan { get; set; }

        public CapabilityInfo(UUID cap, UserAccount acct, HashSet<String> dlist, int span)
        {
            Capability = cap;
            Account = acct;
            DomainList = dlist;

            LifeSpan = span * 1000;
            LastRefresh = Util.EnvironmentTickCount();
        }

        public CapabilityInfo()
        {
            Capability = UUID.Random();
            Account = null;
            DomainList = new HashSet<String>();

            LifeSpan = 0;
            LastRefresh = 0;
        }
    }

    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    public class CapabilityCache
    {
        public Dictionary<UUID,CapabilityInfo> CapabilityCollection { get; set; }

        protected static int PurgeInterval = 60 * 1000;
        protected int LastPurge { get; set; }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public CapabilityCache()
        {
            CapabilityCollection = new Dictionary<UUID,CapabilityInfo>();
            LastPurge = Util.EnvironmentTickCount();
        }
            
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public bool AddCapability(UUID cap, UserAccount acct, HashSet<String> dlist, int span)
        {
            lock (CapabilityCollection)
            {
                CapabilityInfo capinfo = new CapabilityInfo(cap,acct,dlist,span);
                CapabilityCollection.Add(cap,capinfo);
                return true;
            }
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public bool RemoveCapability(UUID cap)
        {
            lock (CapabilityCollection)
            {
                if (CapabilityCollection.ContainsKey(cap))
                {
                    CapabilityCollection.Remove(cap);
                    return true;
                }
            }
            return false;
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public List<CapabilityInfo> DumpCapabilities()
        {
            lock (CapabilityCollection)
            {
                List<CapabilityInfo> caps = new List<CapabilityInfo>(CapabilityCollection.Values);
                return caps;
            }
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public bool GetCapability(UUID cap, out UserAccount acct, out HashSet<String> dlist)
        {
            lock (CapabilityCollection)
            {
                PurgeCache();
                
                CapabilityInfo capinfo;
                if (! CapabilityCollection.TryGetValue(cap, out capinfo))
                {
                    acct = null;
                    dlist = null;
                    return false;
                }
                
                acct = capinfo.Account;
                dlist = capinfo.DomainList;
                capinfo.LastRefresh = Util.EnvironmentTickCount();

                return true;
            }
        }
                        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public bool UpdateCapability(UUID cap, HashSet<String> dlist, int span)
        {
            lock (CapabilityCollection)
            {
                
                CapabilityInfo capinfo;
                if (! CapabilityCollection.TryGetValue(cap, out capinfo))
                    return false;
                
                capinfo.DomainList = dlist;
                capinfo.LifeSpan = span * 1000;
                capinfo.LastRefresh = Util.EnvironmentTickCount();

                return true;
            }
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        protected void PurgeCache()
        {
            int now = Util.EnvironmentTickCount();
            if (Util.EnvironmentTickCountCompare(now,LastPurge) < PurgeInterval)
                return;
                
            List<UUID> purgelist = new List<UUID>();
            foreach (KeyValuePair<UUID,CapabilityInfo> kvp in CapabilityCollection)
            {
                int span = kvp.Value.LifeSpan;
                int refresh = kvp.Value.LastRefresh;
                
                if (span > 0 && Util.EnvironmentTickCountCompare(now,refresh) > span)
                    purgelist.Add(kvp.Key);
            }
                
            foreach (UUID cap in purgelist)
                CapabilityCollection.Remove(cap);

            LastPurge = now;
        }
    }
}
