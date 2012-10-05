/*
 * -----------------------------------------------------------------
 * Copyright (c) 2010 Intel Corporation
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

using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System.Collections.Generic;

[assembly: Addin("N-Body", "0.1")]
[assembly: AddinDependency("OpenSim", "0.5")]

namespace NBodySpace
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]

    public class NBodyModule : INonSharedRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private IConfig m_config  = null;
        private bool   m_configured = false;
        private bool   m_enabled    = false;
        private Int32  m_interval   = 200;
        private Int32  m_threads    = 4;

        private Int32 m_lastTick = 0;

        private Scene m_scene = null;
        private IScriptModuleComms m_comms;

        private Dictionary<UUID,NBodyGroup> m_domainStore;

        private System.Timers.Timer m_updateTimer;

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
                if ((m_config = config.Configs["NBody"]) == null)
                {
                    // There is no configuration, the module is disabled
                    m_log.InfoFormat("[NBody] no configuration info");
                    return;
                }

                m_enabled = m_config.GetBoolean("Enabled", m_enabled);
                m_interval = m_config.GetInt("SimulationInterval",m_interval);
                m_threads = m_config.GetInt("SimulationThreads",m_threads);
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[NBody] initialization error: {0}",e.Message);
                return;
            }

            //m_log.ErrorFormat("[NBody] module {0} enabled",(m_enabled ? "is" : "is not"));
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// everything is loaded, perform post load configuration
        /// </summary>
        // -----------------------------------------------------------------
        public void PostInitialise() {}

        // -----------------------------------------------------------------
        /// <summary>
        /// Nothing to do on close
        /// </summary>
        // -----------------------------------------------------------------
        public void Close() {}

        // -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public void AddRegion(Scene scene)
        {
            if (m_enabled)
            {
                m_domainStore = new Dictionary<UUID,NBodyGroup>();

                m_updateTimer = new System.Timers.Timer(m_interval);

                m_updateTimer.Enabled = false;
                m_updateTimer.AutoReset = false;
                m_updateTimer.Interval = m_interval; 
                m_updateTimer.Elapsed += new ElapsedEventHandler(NBodyUpdate);

                NBodyGroup.SimulationThreads = m_threads;
            }
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public void RemoveRegion(Scene scene) {}

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
                m_comms = m_scene.RequestModuleInterface<IScriptModuleComms>();
                if (m_comms == null)
                {
                    m_log.ErrorFormat("[NBody] ScriptModuleComms interface not defined");
                    m_enabled = false;
                    return;
                }

                m_comms.RegisterScriptInvocation(this,"NBCreateDomain");
                m_comms.RegisterScriptInvocation(this,"NBDestroyDomain");
                m_comms.RegisterScriptInvocation(this,"NBSetTimeScale");
                m_comms.RegisterScriptInvocation(this,"NBEnableDomain");
                m_comms.RegisterScriptInvocation(this,"NBDisableDomain");
                
                m_comms.RegisterScriptInvocation(this,"NBAddEntity");
                m_comms.RegisterScriptInvocation(this,"NBRemoveEntity");
                m_comms.RegisterScriptInvocation(this,"NBDump");
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

#region ScriptInvocationInteface
        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        protected void GenerateRuntimeError(string msg)
        {
            //IWorldComm wComm = m_scene.RequestModuleInterface<IWorldComm>();
            //wComm.DeliverMessage(ChatTypeEnum.Shout, ScriptBaseClass.DEBUG_CHANNEL, "", UUID.Zero, msg);

            throw new Exception("NBody Runtime Error: " + msg);
        }
       
        // -----------------------------------------------------------------
        /// <summary>
        /// returns == [ latitude, longitude ] 
        /// </summary>
        // -----------------------------------------------------------------
        public UUID NBCreateDomain(UUID hostID, UUID scriptID,
                                   Vector3 center, float range, int dimension, float timescale, int simtype)
        {
            m_log.DebugFormat("[N-BodyModule] Create domain");
            
            UUID domainID = UUID.Random();

            lock (m_domainStore)
            {
                try 
                {
                    NBodyGroup nbg = NBodyGroup.CreateNBodyGroup(m_scene,(NBodyGroup.AttractorType)simtype);
                    nbg.CenterPoint = center;
                    nbg.MaxRange = range;
                    nbg.Dimension = dimension;
                    nbg.TimeScale = timescale;
                    nbg.Enabled = false;
                
                    m_domainStore.Add(domainID,nbg);
                }
                catch (Exception e)
                {
                    m_log.WarnFormat("[N-BodyModule] exception while creating domain; {0}",e.Message);
                    return UUID.Zero;
                }
            }

            m_lastTick = Util.EnvironmentTickCount();
            m_updateTimer.Start();

            m_log.DebugFormat("[N-BodyModule] created {0}",domainID);
            return domainID;


        }

        // -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public UUID NBDestroyDomain(UUID hostID, UUID scriptID, UUID domainID)

        {
            m_log.DebugFormat("[N-BodyModule] remove domain {0}",domainID);
            lock (m_domainStore)
            {
                if (m_domainStore.ContainsKey(domainID))
                    m_domainStore.Remove(domainID);

                if (m_domainStore.Count == 0)
                    m_updateTimer.Stop();
            }

            return UUID.Zero;
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public float NBSetTimeScale(UUID hostID, UUID scriptID, UUID domainID, float timescale)
        {
            m_log.DebugFormat("[N-BodyModule] set timescale for domain {0}",domainID);
            lock (m_domainStore)
            {
                if (m_domainStore.ContainsKey(domainID))
                {
                    NBodyGroup nbg = m_domainStore[domainID];
                    nbg.TimeScale = timescale;
                }
            }

            return timescale;
        }
        
        // -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public int NBEnableDomain(UUID hostID, UUID scriptID, UUID domainID)
        {
            m_log.DebugFormat("[N-BodyModule] enable domain {0}",domainID);
            lock (m_domainStore)
            {
                if (m_domainStore.ContainsKey(domainID))
                {
                    NBodyGroup nbg = m_domainStore[domainID];
                    nbg.Enabled = true;
                }
            }

            return 1;
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public int NBDump(UUID hostID, UUID scriptID, UUID domainID)
        {
            // m_log.WarnFormat("[N-BodyModule] dump domain {0}",domainID);
            lock (m_domainStore)
            {
                if (m_domainStore.ContainsKey(domainID))
                {
                    NBodyGroup nbg = m_domainStore[domainID];
                    nbg.Dump();
                }
                else
                    m_log.WarnFormat("[N-BodyModule] unknown domain {0}",domainID);
            }

            return 1;
        }
        
        // -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public int NBDisableDomain(UUID hostID, UUID scriptID, UUID domainID)
        {
            m_log.DebugFormat("[N-BodyModule] disable domain {0}",domainID);
            lock (m_domainStore)
            {
                if (m_domainStore.ContainsKey(domainID))
                {
                    NBodyGroup nbg = m_domainStore[domainID];
                    nbg.Enabled = false;
                }
            }

            return 1;
        }
        
        // -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public UUID NBAddEntity(UUID hostID, UUID scriptID,
                                UUID domainID, UUID bodyID,
                                object[] value, int canMove, int canAttract,
                                float mass, Vector3 velocity)
        {
            m_log.DebugFormat("[N-BodyModule] add entity {0} to domain {1}",bodyID,domainID);

            lock (m_domainStore)
            {
                if (m_domainStore.ContainsKey(domainID))
                {
                    NBodyGroup nbg = m_domainStore[domainID];


                    if (! ValidateVector(value,nbg.Dimension))
                        GenerateRuntimeError("nbody value has the wrong length");

                    m_domainStore[domainID].AddEntity(bodyID,NormalizeVector(value),canMove,canAttract,mass,velocity);
                }
            }
            
            return domainID;
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public UUID NBRemoveEntity(UUID hostID, UUID scriptID, UUID domainID, UUID entityID)
        {
            m_log.DebugFormat("[N-BodyModule] remove body {0} from domain {1}",entityID,domainID);

            lock (m_domainStore)
            {
                if (m_domainStore.ContainsKey(domainID))
                    m_domainStore[domainID].RemoveEntity(entityID);
            }

            return domainID;
        }

#endregion

        // -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        private void NBodyUpdate(object sender, EventArgs ea)
        {
            Int32 curtick = Util.EnvironmentTickCount();
            double timestep = Util.EnvironmentTickCountSubtract(curtick,m_lastTick) / 1000.0;

            if (timestep > 1.0)
                timestep = 1.0;
            
            // m_log.DebugFormat("[N-BodyModule] process updates, timestep={0}",timestep);
            
            lock (m_domainStore)
            {
                foreach (KeyValuePair<UUID,NBodyGroup> kpgroup in m_domainStore)
                {
                    try 
                    {
                        if (kpgroup.Value.Enabled)
                        {
                            Int32 tick = kpgroup.Value.Update(timestep);
                            if (tick > 500)
                                m_log.WarnFormat("[N-BodyModule] long update cycle; {0}",tick);

                        }
                    }
                    catch (Exception e)
                    {
                        m_log.WarnFormat("[N-BodyModule] exception while processing domain {0}; {1}",kpgroup.Key,e.Message);
                    }
                }
            }
            
            m_lastTick = curtick;
            m_updateTimer.Start();
        }
        
        // -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        private bool ValidateVector(object[] value, int dimension)
        {
            // Make sure the value has the right length
            if (value.Length != dimension)
                return false;
            
            // Make sure all the values are floats
            for (int i = 0; i < dimension; i++)
                if (! (value[i] is float))
                    return false;
            
            return true;
        }
        
        // -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        private double[] NormalizeVector(object[] value)
        {
            // compute the magnitude for normalization
            double mag = 0.0;
            for (int i = 0; i < value.Length; i++)
            {
                float f = (float)value[i];
                mag += f * f;
            }
            mag = Math.Sqrt(mag);

            // and copy & normalize
            double[] dvalue = new double[value.Length];
            for (int i = 0; i < value.Length; i++)
                dvalue[i] = (float)value[i] / mag;

            return dvalue;
        }
        

    }
}
