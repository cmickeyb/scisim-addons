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

namespace NBodySpace
{
    public class PerfProfiler
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const double Alpha = 0.2;

        private class TickRecord
        {
            public Int32 Tick { get; set; }
            public string Tag { get; set; }
            public TickRecord(string tag)
            {
                Tag = tag;
                Tick = Util.EnvironmentTickCount();
            }
        }

        private class StatRecord
        {
            public double Average { get; set; }
            public int Count { get; set; }
            
            public StatRecord(Int32 tick)
            {
                Count = 1;
                Average = tick;
            }

            public void AddRecord(Int32 tick)
            {
                Average = (Average * Count + tick) / (Count + 1);
                Count++;
            }
        }
        
        private Stack<TickRecord> TagStack;
        private Dictionary<String,StatRecord> Stats;

        public string ProfilerName { get; set; }

        public PerfProfiler(string roottag)
        {
            Stats = new Dictionary<String,StatRecord>();
            TagStack = new Stack<TickRecord>();
            TagStack.Push(new TickRecord(roottag));
        }

        public void Push(string tag)
        {
            TagStack.Push(new TickRecord(tag));
        }
        
        public void Record(string tag)
        {
            TickRecord lsttick = TagStack.Pop();
            TickRecord curtick = new TickRecord(tag);

            Int32 diff = Util.EnvironmentTickCountSubtract(curtick.Tick,lsttick.Tick);
            string path = lsttick.Tag + "==>" + curtick.Tag;
            if (! Stats.ContainsKey(path))
                Stats.Add(path,new StatRecord(diff));
            else
                Stats[path].AddRecord(diff);

            TagStack.Push(curtick);
        }

        public void Pop(string tag)
        {
            Record(tag);
            TagStack.Pop();
        }

        public void Dump()
        {
            foreach (KeyValuePair<string,StatRecord> kp in Stats)
                m_log.WarnFormat("[PROFILER-{0}] {1} == {2}",ProfilerName,kp.Key,kp.Value.Average);
        }
    }
}
