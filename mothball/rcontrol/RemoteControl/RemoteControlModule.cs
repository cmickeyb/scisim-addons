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

using Dispatcher;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using OpenSim.Region.Framework.Scenes.Serialization;
            
using RemoteControl.Messages;
using RemoteControl.Handlers;

[assembly: Addin("RemoteControlModule", "0.1")]
[assembly: AddinDependency("OpenSim", "0.5")]

namespace RemoteControl
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]

    /// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    /// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    public class RemoteControlModule  : INonSharedRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private List<BaseHandler> m_handlers = new List<BaseHandler>();
        
        private IConfig m_config = null;
        private bool m_enabled = true;
        private Scene m_scene = null;
        private string m_domain = "RemoteControl";
        
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
                if ((m_config = config.Configs["RemoteControl"]) != null)
                {
                    m_enabled = m_config.GetBoolean("Enabled", m_enabled);
                    m_domain = m_config.GetString("Domain", m_domain);
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[ModInvoke] initialization error: {0}",e.Message);
                return;
            }
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// all of the shared modules have been loaded and initialized
        /// </summary>
        // -----------------------------------------------------------------
        public void PostInitialise() { }

        // -----------------------------------------------------------------
        /// <summary>
        /// Nothing to do on close
        /// </summary>
        // -----------------------------------------------------------------
        public void Close() { }

        // -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public void AddRegion(Scene scene) { }

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
                foreach (BaseHandler handler in m_handlers)
                    handler.UnregisterHandlers();

                m_handlers.Clear();
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
            if (m_enabled)
            {
                m_scene = scene;

                IDispatcherModule m_dispatcher = m_scene.RequestModuleInterface<IDispatcherModule>();
                if (m_dispatcher == null)
                {
                    m_log.WarnFormat("[RemoteControlModule] IDispatcherModule interface not defined");
                    m_enabled = false;

                    return;
                }

                m_handlers.Add(new ObjectHandlers(m_scene,m_dispatcher,m_domain));
                m_handlers.Add(new RegionHandlers(m_scene,m_dispatcher,m_domain));
                m_handlers.Add(new TerrainHandlers(m_scene,m_dispatcher,m_domain));
                m_handlers.Add(new CommunicationHandlers(m_scene,m_dispatcher,m_domain));
                m_handlers.Add(new EventHandlers(m_scene,m_dispatcher,m_domain));
                m_handlers.Add(new ObjectPartHandlers(m_scene,m_dispatcher,m_domain));
                m_handlers.Add(new AvatarHandlers(m_scene,m_dispatcher,m_domain));
                m_handlers.Add(new AssetHandlers(m_scene,m_dispatcher,m_domain));
                
                foreach (BaseHandler handler in m_handlers)
                    handler.RegisterHandlers();
            }
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
    }
}
