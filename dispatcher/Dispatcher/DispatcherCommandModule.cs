/*
 * Copyright (c) Contributors 
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using Mono.Addins;

using System;
using System.Reflection;
using System.Text;

using log4net;
using Nini.Config;

using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System.Collections.Generic;


namespace Dispatcher
{
    public class DispatcherCommandModule  
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
        public DispatcherCommandModule(IConfig config, DispatcherModule dispatcher)
        {
            m_dispatcher = dispatcher;
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
        
        public void RegionsLoaded()
        {
            MainConsole.Instance.Commands.AddCommand("Dispatcher", false, "dispatcher stats", "dispatcher stats",
                                                     "Display statistics about the state of the Dispatcher module",
                                                     CmdStats);
        }

#endregion

#region Commands

        private void CmdStats(string module, string[] cmd)
        {
            foreach (KeyValuePair<String,DispatcherStat> kvp in m_dispatcher.DispatcherStats)
            {
                UInt64 tr = kvp.Value.TotalRequests;
                UInt64 tt = kvp.Value.TotalTime;
                double avg = (1.0 * tt) / tr;
                
                MainConsole.Instance.OutputFormat("{0,15}: {1,8} requests, avg resp {2:#.###}", kvp.Key, tr, avg);
            }
            
            MainConsole.Instance.OutputFormat("Current async queue depth {0}", m_dispatcher.RequestQueue.Count);
        }

#endregion

    }
}
