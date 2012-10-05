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
using System.Collections.Generic;
using System.Text.RegularExpressions;

            
[assembly: Addin("ModInvokeTest", "0.1")]
[assembly: AddinDependency("OpenSim", "0.5")]

namespace ModInvokeTest
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]

    public class ModInvokeTestModule  : INonSharedRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IConfig m_config = null;
        private bool m_enabled = true;
        private Scene m_scene = null;
        
        private IScriptModuleComms m_comms;
        
#region IRegionModule Members

        public string Name
        {
            get { return this.GetType().Name; }
        }

        public void Initialise(IConfigSource config)
        {
            try 
            {
                if ((m_config = config.Configs["ModInvoke"]) != null)
                    m_enabled = m_config.GetBoolean("Enabled", m_enabled);
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[ModInvoke] initialization error: {0}",e.Message);
                return;
            }
        }

        public void PostInitialise() { }
        public void Close() { }

        public void AddRegion(Scene scene) { }
        public void RemoveRegion(Scene scene)  { }

        public void RegionLoaded(Scene scene)
        {
            if (m_enabled)
            {
                m_scene = scene;
                m_comms = m_scene.RequestModuleInterface<IScriptModuleComms>();
                if (m_comms == null)
                {
                    m_log.WarnFormat("[ModInvoke] ScriptModuleComms interface not defined");
                    m_enabled = false;

                    return;
                }

                m_comms.RegisterScriptInvocation(this,"ModTest0");
                m_comms.RegisterScriptInvocation(this,"ModTest1");
                m_comms.RegisterScriptInvocation(this,"ModTest2");
                m_comms.RegisterScriptInvocation(this,"ModTest3");
                m_comms.RegisterScriptInvocation(this,"ModTest4");
                m_comms.RegisterScriptInvocation(this,"ModTest5");
                m_comms.RegisterScriptInvocation(this,"ModTest6");
                m_comms.RegisterScriptInvocation(this,"ModTest7");
                m_comms.RegisterScriptInvocation(this,"ModTest8");

                m_comms.RegisterScriptInvocation(this,"ModTestR1");

                m_comms.RegisterScriptInvocation(this,"ModTest1p");
                m_comms.RegisterScriptInvocation(this,"ModTest2p");
                m_comms.RegisterScriptInvocation(this,"ModTest3p");
                m_comms.RegisterScriptInvocation(this,"ModTest4p");
                m_comms.RegisterScriptInvocation(this,"ModTest5p");

                m_comms.RegisterConstant("ModConstantInt1",25);
                m_comms.RegisterConstant("ModConstantFloat1",25.000f);
                m_comms.RegisterConstant("ModConstantString1","abcdefg");
            }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

#endregion

#region ScriptInvocationInteface
        public string ModTest0(UUID hostID, UUID scriptID)
        {
            m_log.WarnFormat("[ModInvoke] ModTest0 parameter");
            return "";
        }

        public string ModTest1(UUID hostID, UUID scriptID, string value)
        {
            m_log.WarnFormat("[ModInvoke] ModTest1 parameter: {0}",value);
            return value;
        }

        public int ModTest2(UUID hostID, UUID scriptID, int value)
        {
            m_log.WarnFormat("[ModInvoke] ModTest2 parameter: {0}",value);
            return value;
        }

        public float ModTest3(UUID hostID, UUID scriptID, float value)
        {
            m_log.WarnFormat("[ModInvoke] ModTest3 parameter: {0}",value);
            return value;
        }

        public UUID ModTest4(UUID hostID, UUID scriptID, UUID value)
        {
            m_log.WarnFormat("[ModInvoke] ModTest4 parameter: {0}",value.ToString());
            return value;
        }

        public OpenMetaverse.Vector3 ModTest5(UUID hostID, UUID scriptID, OpenMetaverse.Vector3 value)
        {
            m_log.WarnFormat("[ModInvoke] ModTest5 parameter: {0}",value.ToString());
            return value;
        }

        public OpenMetaverse.Quaternion ModTest6(UUID hostID, UUID scriptID, OpenMetaverse.Quaternion value)
        {
            m_log.WarnFormat("[ModInvoke] ModTest6 parameter: {0}",value.ToString());
            return value;
        }

        public object[] ModTest7(UUID hostID, UUID scriptID, int count, string val)
        {
            object[] result = new object[count];
            for (int i = 0; i < count; i++)
                result[i] = val;
            
            return result;
        }

        public object[] ModTest8(UUID hostID, UUID scriptID, object[] lparm)
        {
            object[] result = new object[lparm.Length];

            for (int i = 0; i < lparm.Length; i++)
                result[lparm.Length - i - 1] = lparm[i];
            
            return result;
        }

        public object[] ModTestR1(UUID hostID, UUID scriptID)
        {
            object[] result = new object[2];
            result[0] = hostID;
            result[1] = scriptID;
            return result;
        }

        public string ModTest1p(UUID hostID, UUID scriptID, string p1)
        {
            m_log.WarnFormat("[ModInvoke] ModTestP1 parameter");
            return "";
        }

        public string ModTest2p(UUID hostID, UUID scriptID, string p1, string p2)
        {
            m_log.WarnFormat("[ModInvoke] ModTest0 parameter");
            return "";
        }

        public string ModTest3p(UUID hostID, UUID scriptID, string p1, string p2, string p3)
        {
            m_log.WarnFormat("[ModInvoke] ModTest0 parameter");
            return "";
        }

        public string ModTest4p(UUID hostID, UUID scriptID, string p1, string p2, string p3, string p4)
        {
            m_log.WarnFormat("[ModInvoke] ModTest0 parameter");
            return "";
        }


        public string ModTest5p(UUID hostID, UUID scriptID, string p1, string p2, string p3, string p4, string p5)
        {
            m_log.WarnFormat("[ModInvoke] ModTest0 parameter");
            return "";
        }




#endregion
    }
}
