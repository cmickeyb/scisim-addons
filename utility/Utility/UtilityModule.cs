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

using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System.Collections.Generic;

[assembly: Addin("Utility", "0.1")]
[assembly: AddinDependency("OpenSim", "0.5")]

namespace QuickSortViz
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]

    public class UtilityModule : INonSharedRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private IConfig m_config  = null;
        private bool   m_configured = false;
        private bool   m_enabled    = false;

        private Scene m_scene = null;
        private IScriptModuleComms m_comms;

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
                if ((m_config = config.Configs["Utility"]) == null)
                {
                    // There is no configuration, the module is disabled
                    m_log.InfoFormat("[Utility] no configuration info");
                    return;
                }

                m_enabled = m_config.GetBoolean("Enabled", m_enabled);
                if (m_enabled)
                {
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[Utility] initialization error: {0}",e.Message);
                return;
            }

            // m_log.ErrorFormat("[Utility] module {0} enabled",(m_enabled ? "is" : "is not"));
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
        public void AddRegion(Scene scene) {}

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
                    m_log.ErrorFormat("[Utility] ScriptModuleComms interface not defined");
                    m_enabled = false;
                    return;
                }

                m_comms.RegisterScriptInvocation(this,"GISConvertFromUTM");
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

            throw new Exception("Utility Runtime Error: " + msg);
        }
        
        // -----------------------------------------------------------------
        /// <summary>
        /// returns == [ latitude, longitude ]
        /// This algorithm is modified from one of several implementations found
        /// the convert UTM to Lat/Lon
        /// </summary>
        // -----------------------------------------------------------------
        public object[] GISConvertFromUTM(UUID hostID, UUID scriptID, float easting, float northing, int zone)
        {
            double latitude;
            double longitude;
 
            double RAD_TO_DEG = 180.0 / Math.PI;
            double const0 = 0.99960000000000004;
            double const1 = 6378137; // polar radius
            double const2 = 0.0066943799999999998;
            double const3 = const2 / (1 - const2);
            double const4 = (1 - Math.Sqrt(1 - const2)) / (1 + Math.Sqrt(1 - const2));
 
            double zonenorm = ((zone - 1) * 6 - 180) + 3;
            double northnorm = northing / const0;
            double nvar0 = northnorm / (const1 * (1 - const2 / 4 - (3 * const2 * const2) / 64 - (5 * Math.Pow(const2,3) ) / 256));
            double nvar1 = nvar0 +
                ((3 * const4) / 2 - (27 * Math.Pow(const4,3) ) / 32) * Math.Sin(2 * nvar0) +
                ((21 * const4 * const4) / 16 - (55 * Math.Pow(const4,4) ) / 32) * Math.Sin(4 * nvar0) +
                ((151 * Math.Pow(const4,3) ) / 96) * Math.Sin(6 * nvar0);
 
            double nvar2 = const1 / Math.Sqrt(1 - const2 * Math.Sin(nvar1) * Math.Sin(nvar1));
            double nvar3 = Math.Tan(nvar1) * Math.Tan(nvar1);
            double nvar4 = const3 * Math.Cos(nvar1) * Math.Cos(nvar1);
            double nvar5 = (const1 * (1 - const2)) / Math.Pow(1 - const2 * Math.Sin(nvar1) * Math.Sin(nvar1), 1.5);
            double evar1 = (easting - 500000) / (nvar2 * const0);
 
            double latrad = nvar1 - ((nvar2 * Math.Tan(nvar1)) / nvar5) * (((evar1 * evar1) / 2 - (((5 + 3 * nvar3 + 10 * nvar4) - 4 * nvar4 * nvar4 - 9 * const3) * Math.Pow(evar1,4) ) / 24) + (((61 + 90 * nvar3 + 298 * nvar4 + 45 * nvar3 * nvar3) - 252 * const3 - 3 * nvar4 * nvar4) * Math.Pow(evar1,6) ) / 720);
            latitude = latrad * RAD_TO_DEG;
 
            double lonrad = ((evar1 - ((1 + 2 * nvar3 + nvar4) * Math.Pow(evar1,3) ) / 6) + (((((5 - 2 * nvar4) + 28 * nvar3) - 3 * nvar4 * nvar4) + 8 * const3 + 24 * nvar3 * nvar3) * Math.Pow(evar1,5) ) / 120) / Math.Cos(nvar1);
            longitude = zonenorm + lonrad * RAD_TO_DEG;
 

            object[] result = new object[2];
            result[0] = (float)latitude;
            result[1] = (float)longitude;

            return result;
        }
#endregion

    }
}
