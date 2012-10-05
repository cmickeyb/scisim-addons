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

[assembly: Addin("QuickSort", "0.1")]
[assembly: AddinDependency("OpenSim", "0.5")]

namespace QuickSortViz
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]

    public class QuickSortModule : INonSharedRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static Random m_random;
        
        private class WorkElement
        {
            public float m_rangemin = 0;
            public float m_rangemax = 0;
            public List<SceneObjectGroup> m_prims = new List<SceneObjectGroup>();

            public WorkElement(float rmin, float rmax)
                {
                    m_rangemin = rmin;
                    m_rangemax = rmax;
                }
        }
        
        private IConfig m_config  = null;
        private bool   m_configured = false;
        private bool   m_enabled    = false;

        private Scene m_scene = null;
        private IScriptModuleComms m_comms;

        private enum SortState { PreCreate, Create, PreSort, Sort, PreDestroy, Destroy };
        private SortState m_state = SortState.PreCreate;

        private bool m_abort = false;
        
        // these cover configurable properties
        private int m_objectcount = 100;
        private Vector3 m_startingpos;
        private Vector3 m_range;

        private int m_samplesize = 5;
        private int m_shortsize = 10;

        private System.Timers.Timer m_probeTimer = new System.Timers.Timer();

        private Queue<WorkElement> m_sortqueue = new Queue<WorkElement>();
        private List<SceneObjectGroup> m_sorted = new List<SceneObjectGroup>();

        private UUID m_scriptID;
        private UUID m_reqID;
        
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
                if ((m_config = config.Configs["QuickSort"]) == null)
                {
                    // There is no configuration, the module is disabled
                    m_log.InfoFormat("[QuickSort] no configuration info");
                    return;
                }

                m_enabled = m_config.GetBoolean("Enabled", m_enabled);
                if (m_enabled)
                {
                    m_startingpos = new Vector3(128.0f,128.0f,30.0f);
                    m_range = new Vector3(20.0f,5.0f,5.0f);
                        
                    m_objectcount = m_config.GetInt("object-count",m_objectcount);
                    m_samplesize = m_config.GetInt("sample-size",m_samplesize);
                    m_shortsize = m_config.GetInt("short-size",m_shortsize);

                    string position = m_config.GetString("position","<128.0,128.0,30.0>");
                    m_startingpos = Vector3.Parse(position);
                        
                    string range = m_config.GetString("range","<20.0,5.0,5.0>");
                    m_range = Vector3.Parse(range);

                    m_random = new Random();
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[QuickSort] initialization error: {0}",e.Message);
                return;
            }

            m_log.ErrorFormat("[QuickSort] module {0} enabled",(m_enabled ? "is" : "is not"));
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
                    m_log.ErrorFormat("[QuickSort] ScriptModuleComms interface not defined");
                    m_enabled = false;
                    return;
                }

                //m_comms.RegisterScriptInvocation("QuickSortConfig",SI_QuickSortConfig, new Type[] { typeof(string), typeof(string) }, typeof(int));
                //m_comms.RegisterScriptInvocation("QuickSortState",SI_QuickSortState, new Type[] { typeof(string) }, typeof(string));
                m_comms.RegisterScriptInvocation(this,"QuickSortConfig");
                m_comms.RegisterScriptInvocation(this,"QuickSortState");
                
                m_probeTimer.Enabled = false;
                m_probeTimer.AutoReset = false;
                m_probeTimer.Interval = 500; // 500 milliseconds wait to start async ops
                m_probeTimer.Elapsed += new ElapsedEventHandler(UpdateSortObjects);
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

            throw new Exception("QuickSort Runtime Error: " + msg);
        }
        
        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        public int QuickSortConfig(UUID hostID, UUID scriptID, string field, string value)
        {
            switch (field)
            {
            case "object-count":
                m_objectcount = Convert.ToInt32(value);
                m_log.DebugFormat("[QuickSort] set object-count = {0}",m_objectcount);
                break;

            case "sample-size":
                m_samplesize = Convert.ToInt32(value);
                m_log.DebugFormat("[QuickSort] set sample-size = {0}",m_samplesize);
                break;
                
            case "short-size":
                m_shortsize = Convert.ToInt32(value);
                m_log.DebugFormat("[QuickSort] set short-size = {0}",m_shortsize);
                break;

            case "position":
                m_startingpos = Vector3.Parse(value);
                m_log.DebugFormat("[QuickSort] set position = {0}",m_startingpos.ToString());
                break;

            case "range":
                m_range = Vector3.Parse(value);
                m_log.DebugFormat("[QuickSort] set range = {0}",m_range.ToString());
                break;

            default:
                m_log.WarnFormat("[QuickSort] unknown configuration variable {0}",field);
                break;
            }

            return 0;
        }


        // -----------------------------------------------------------------
        /// <summary>
        /// Listen for inworld commands and configuration
        /// </summary>
        // -----------------------------------------------------------------
        protected string QuickSortState(UUID hostID, UUID scriptID, string command)
        {
            command = command.ToLower();
            
            m_scriptID = scriptID;
            m_reqID = UUID.Random();

            m_log.DebugFormat("[QuickSort] set state to {0} in script {1}",command,m_scriptID);
            

            try
            {
                switch (command)
                {
                case "abort":
                    if (m_state == SortState.Sort)
                    {
                        m_log.DebugFormat("[QuickSort] abort");
                        m_abort = true;
                        return m_reqID.ToString();
                    }
                    break;
                    
                case "create":
                    if (m_state == SortState.PreCreate)
                    {
                        m_log.DebugFormat("[QuickSort] create");
                        m_state = SortState.Create;
                        m_probeTimer.Start();
                        return m_reqID.ToString();
                    }
                    break;

                case "destroy":
                    if ((m_state == SortState.PreSort) || (m_state == SortState.PreDestroy))
                    {
                        m_log.DebugFormat("[QuickSort] destroy");
                        m_state = SortState.Destroy;
                        m_probeTimer.Start();
                        return m_reqID.ToString();
                    }
                    break;

                case "sort":
                    if (m_state == SortState.PreSort) 
                    {
                        m_log.DebugFormat("[QuickSort] sort");
                        m_state = SortState.Sort;
                        m_probeTimer.Start();
                        return m_reqID.ToString();
                    }
                    break;

                default:
                    m_log.WarnFormat("[QuickSort] unknown command {0}",command);
                    break;
                }
            }
            catch (Exception e)
            { 
                m_log.WarnFormat("[QuickSort] error processing command channel input: {0}",e.ToString());
            }

            return UUID.Zero.ToString();
        }

#endregion

#region Control Members

        // -----------------------------------------------------------------
        /// <summary>
        /// Thread timer handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ea"></param>
        // -----------------------------------------------------------------
        private void UpdateSortObjects(object sender, EventArgs ea)
        {
            m_log.DebugFormat("[QuickSort] timer event fired");

            switch (m_state)
            {
            case SortState.Create:
                CreateSortableObjects();
                m_comms.DispatchReply(m_scriptID,1,"create",UUID.Zero.ToString());
                m_state = SortState.PreSort;
                break;
                
            case SortState.Destroy:
                DestroySortableObjects();
                m_comms.DispatchReply(m_scriptID,1,"destroy",UUID.Zero.ToString());
                m_state = SortState.PreCreate;
                break;
                
            case SortState.Sort:
                SortSortableObjects();
                m_comms.DispatchReply(m_scriptID,1,"sort",UUID.Zero.ToString());
                m_state = SortState.PreDestroy;
                break;
            }
        }

#endregion

#region Object Creation

        // -----------------------------------------------------------------
        /// <summary>
        /// Create the list of objects for sorting
        /// </summary>
        // -----------------------------------------------------------------
        private void CreateSortableObjects()
        {
            WorkElement prims = new WorkElement(m_startingpos.X,m_startingpos.X + m_range.X);
            for (int i = 0; i < m_objectcount; i++)
            {
                SceneObjectGroup sog = CreateSortableObject();
                m_scene.AddNewSceneObject(sog, false); 
                prims.m_prims.Add(sog);
            }

            m_sortqueue.Enqueue(prims);
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Create one of the objects
        /// </summary>
        // -----------------------------------------------------------------
        private SceneObjectGroup CreateSortableObject()
        {
            Quaternion rot = new Quaternion(Vector3.Zero,1);
            PrimitiveBaseShape shape = PrimitiveBaseShape.CreateSphere();
            Vector3 pos = new Vector3();

            pos.X = (float)(m_random.NextDouble() * m_range.X) + m_startingpos.X;
            pos.Y = (float)(m_random.NextDouble() * m_range.Y) + m_startingpos.Y;
            pos.Z = (float)(m_random.NextDouble() * m_range.Z) + m_startingpos.Z;

            SceneObjectPart sop = new SceneObjectPart(UUID.Zero,shape,pos,rot,Vector3.Zero);

            sop.Name = "quicksort object";
            sop.Scale = new Vector3(0.2f,0.2f,0.2f);
            sop.ObjectFlags |= (uint)PrimFlags.Phantom;

            
            // Change the color of the object
            // Vector3 color = new Vector3(m_random.Next(256),m_random.Next(256),m_random.Next(256));
            Vector3 color = RandomColor();
            Color4 texcolor;
            Primitive.TextureEntry tex = sop.Shape.Textures;
            texcolor = tex.DefaultTexture.RGBA;
            texcolor.R = Util.Clip((float)color.X,0.0f, 1.0f);
            texcolor.G = Util.Clip((float)color.Y,0.0f, 1.0f);
            texcolor.B = Util.Clip((float)color.Z,0.0f, 1.0f);
            tex.DefaultTexture.RGBA = texcolor;
            sop.Shape.Textures = tex;

            SceneObjectGroup sog = new SceneObjectGroup(sop);
            // sog.SetRootPart(sop);
            sog.Color = System.Drawing.Color.FromArgb(0,(int)(color.X * 0xff),(int)(color.Y * 0xff),(int)(color.Z * 0xff));
            sog.Text = string.Format("obj: {0}",SortValue(sog));

            // sog.SetText("obj",color,1.0);
            
            return sog;
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Compute a random color, this should probably be an HSB computation
        /// with the saturation constant
        /// </summary>
        // -----------------------------------------------------------------
        private Vector3 RandomColor()
        {
            float rc = (float)m_random.NextDouble();
            float gc = (float)m_random.NextDouble();
            float bc = (float)m_random.NextDouble();
            return new Vector3(rc,gc,bc);
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Destroy all of the objects and remove them from the scene
        /// </summary>
        // -----------------------------------------------------------------
        private void DestroySortableObjects()
        {
            try 
            {
                foreach (SceneObjectGroup sog in m_sorted)
                {
                    m_scene.DeleteSceneObject(sog,false);
                    Thread.Sleep(10);
                }
                m_sorted = new List<SceneObjectGroup>();

                while (m_sortqueue.Count > 0)
                {
                    WorkElement current = m_sortqueue.Dequeue();
                    foreach (SceneObjectGroup sog in current.m_prims)
                    {
                        m_scene.DeleteSceneObject(sog,false);
                        Thread.Sleep(10);
                    }
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[QuickSort] exception while deleting {0}",e.ToString());
            }
        }

#endregion

#region Sort Members

        // -----------------------------------------------------------------
        /// <summary>
        /// Get the sortable value, this should really be a method on a subclass
        /// of SceneObjectGroup... get over it!
        /// </summary>
        /// <param name="sog">Item</param>
        // -----------------------------------------------------------------
        private float SortValue(SceneObjectGroup sog)
        {
            return sog.Color.GetHue();
        }
        
        // -----------------------------------------------------------------
        /// <summary>
        /// Compute the average position for a sample of this region, could
        /// just split it into equal sized segments but that tends to clump
        /// objects
        /// </summary>
        /// <param name="item">The current work element</param>
        // -----------------------------------------------------------------
        private float ComputeAvgX(WorkElement item)
        {
            // return (float)((item.m_rangemax - item.m_rangemin) / 2.0);
            
            int count = item.m_prims.Count;
            float sum = 0.0f;
            for (int i = 0; i < m_samplesize; i++)
            {
                int pos = m_random.Next(count);
                sum += item.m_prims[pos].AbsolutePosition.X;
            }
            
            return sum/(float)m_samplesize;
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Compute the average value of objects to get a good split value
        /// </summary>
        /// <param name="item">The current work element</param>
        // -----------------------------------------------------------------
        private float ComputeAvgC(WorkElement item)
        {
            int count = item.m_prims.Count;
            float sum = 0;
            for (int i = 0; i < m_samplesize; i++)
            {
                int pos = m_random.Next(count);
                sum += SortValue(item.m_prims[pos]);
            }

            return sum/m_samplesize;
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Split a long segment around a pivot point breaking the sort into two pieces
        /// </summary>
        /// <param name="item">The current work element</param>
        // -----------------------------------------------------------------
        private void SortLongQueue(WorkElement item)
        {
            m_log.DebugFormat("[QuickSort] long sort with {0} elements in region {1} to {2}",
                              item.m_prims.Count, item.m_rangemin, item.m_rangemax);
            
            float avgC = ComputeAvgC(item);
            float avgX = ComputeAvgX(item);
            float rangeU = item.m_rangemax - avgX;
            float rangeL = avgX - item.m_rangemin;

            m_log.DebugFormat("[QuickSort] split at {0} based on {1}",avgX,avgC);

            WorkElement itemSmall = new WorkElement(item.m_rangemin,avgX);
            WorkElement itemLarge = new WorkElement(avgX,item.m_rangemax);
            
            foreach (SceneObjectGroup sog in item.m_prims)
            {
                Vector3 pos = sog.AbsolutePosition;
                if (SortValue(sog) < avgC)
                {
                    if ((pos.X > avgX) || (pos.X < item.m_rangemin))
                    {
                        pos.X = item.m_rangemin + (float)m_random.NextDouble() * rangeL;
                        sog.UpdateGroupPosition(pos);
                        Thread.Sleep(10);
                    }
                    
                    itemSmall.m_prims.Add(sog);
                } else {
                    if ((pos.X < avgX) || (pos.X > item.m_rangemax))
                    {
                        pos.X = item.m_rangemax - (float)m_random.NextDouble() * rangeU;
                        sog.UpdateGroupPosition(pos);
                        Thread.Sleep(10);
                    }                        

                    itemLarge.m_prims.Add(sog);
                }
                
            }

            if (itemSmall.m_prims.Count > 0) m_sortqueue.Enqueue(itemSmall);
            if (itemLarge.m_prims.Count > 0) m_sortqueue.Enqueue(itemLarge);
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Sort a short work element using a simple selection sort
        /// </summary>
        /// <param name=""></param>
        // -----------------------------------------------------------------
        private void SortShortQueue(WorkElement item)
        {
            m_log.DebugFormat("[QuickSort] short sort with {0} elements in region {1} to {2}",
                              item.m_prims.Count, item.m_rangemin, item.m_rangemax);

            int count = item.m_prims.Count;
            for (int i = 0; i < count - 1; i++)
            {
                for (int j = i+1; j < count; j++)
                {
                    if (SortValue(item.m_prims[i]) < SortValue(item.m_prims[j]))
                    {
                        // The list order is wrong... fix it
                        SceneObjectGroup sog = item.m_prims[i];
                        item.m_prims[i] = item.m_prims[j];
                        item.m_prims[j] = sog;
                    }
                    
                    // The list is in the right order, now we need to make sure
                    // the scene is in the right order
                    Vector3 psmall = item.m_prims[i].AbsolutePosition;
                    Vector3 plarge = item.m_prims[j].AbsolutePosition;

                    if (psmall.X > plarge.X)
                    {
                        float x = psmall.X;
                        psmall.X = plarge.X;
                        plarge.X = x;
                        
                        item.m_prims[i].UpdateGroupPosition(psmall);
                        item.m_prims[j].UpdateGroupPosition(plarge);
                        Thread.Sleep(10);
                    }
                }

                // Add it to the sorted list for later clean up
                m_sorted.Add(item.m_prims[i]);
            }

            // Add the last one into the sorted list
            m_sorted.Add(item.m_prims[count - 1]);
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Process the sort event queue
        /// </summary>
        // -----------------------------------------------------------------
        private void SortSortableObjects()
        {
            // We only care if m_abort is set while we're running
            m_abort = false;
            
            while (m_sortqueue.Count > 0)
            {
                m_log.DebugFormat("[QuickSort] sort queue contains {0} items",m_sortqueue.Count);
                
                WorkElement current = m_sortqueue.Dequeue();
                if (current.m_prims.Count < m_shortsize)
                {
                    SortShortQueue(current);
                }
                else 
                {
                    SortLongQueue(current);
                }

                if (m_abort)
                {
                    m_log.Debug("[QuickSort] sort aborted");
                    break;
                }
                
                Thread.Sleep(100);
            }

            m_log.Debug("[QuickSort] sort finished");
        }

#endregion
    }
}
