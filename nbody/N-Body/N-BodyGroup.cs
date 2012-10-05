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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System.Collections.Generic;

namespace NBodySpace
{
    public class NBodyGroup
    {
        protected static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static Int32 SimulationThreads = 4;
        
        public enum AttractorType {
            Gravity = 1,
            SimpleDynamicCosine = 2,
            SimpleStaticCosine = 3,
            FullDynamicCosine = 4,
            FullStaticCosine = 5
        }
        
        public static NBodyGroup CreateNBodyGroup(Scene scene, AttractorType atype)
        {
            switch (atype)
            {
            case AttractorType.Gravity:
                return new NewtonianGravity(scene);
            case AttractorType.SimpleDynamicCosine:
                return new CosineSimilarity(scene,true,false);
            case AttractorType.SimpleStaticCosine:
                return new CosineSimilarity(scene,false,false);
            case AttractorType.FullDynamicCosine:
                return new CosineSimilarity(scene,true,true);
            case AttractorType.FullStaticCosine:
                return new CosineSimilarity(scene,false,true);
            }

            return null;
        }

        protected class VelocityPair
        {
            public Vector3 ABVelocity { get; set; }
            public Vector3 BAVelocity { get; set; }

            public VelocityPair()
            {
                ABVelocity = Vector3.Zero;
                BAVelocity = Vector3.Zero;
            }
        }
        
        protected class NBodyValue
        {
            // Value vector must be normalized to a unit vector!!!
            public double Mass { get; set; }
            public double[] Value { get; set; }
            public Vector3 Velocity { get; set; }
            public Vector3 Position { get; set; } 
            public bool CanAttract { get; set; }
            public bool CanMove { get; set; }
            public UUID Identity { get; set; }
            public UInt32 LocalID { get; set; }

            public NBodyValue(UUID identity, UInt32 localid)
            {
                Identity = identity;
                LocalID = localid;
            }
        }

        protected static double MaxDelta = 50.0;
        
        protected Scene m_scene = null;

        protected Dictionary<UUID,NBodyValue> m_entityList;
        protected UUID[] m_localIDList;

        protected bool Tainted { get; set; }
        protected UInt32 NextLocalID { get; set; }

        public bool Enabled { get; set; }
        public int Dimension { get; set; }
        public Vector3 CenterPoint { get; set; }
        public double MaxRange { get; set; }
        public double TimeScale { get; set; }

        // private PerfProfiler m_profiler;
        
        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        public NBodyGroup(Scene scene) 
        {
            m_scene = scene;
            // m_profiler = new PerfProfiler("NBodyGroup");

            CenterPoint = Vector3.Zero;
            MaxRange = 10.0;
            Dimension = 0;
            TimeScale = 1.0;
            Enabled = false;

            Tainted = false;
            NextLocalID = 0;
            
            m_entityList = new Dictionary<UUID,NBodyValue>();
            m_localIDList = new UUID[6000];
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        public void AddEntity(UUID entityID, double[] value, int canmove, int canattract, double mass, Vector3 velocity)
        {
            // make sure the object exists
            SceneObjectPart sop = m_scene.GetSceneObjectPart(entityID);
            if (sop == null)
            {
                m_log.WarnFormat("[N-BodyGroup] Cannot find SOP associated with {0}",entityID);
                return;
            }

            lock (m_entityList)
            {
                NBodyValue nb;
                if (! m_entityList.TryGetValue(entityID,out nb))
                {
                    nb = new NBodyValue(entityID,NextLocalID++);

                    m_localIDList[nb.LocalID] = entityID;
                    m_entityList[entityID] = nb;
                }

                nb.Value = value;
                nb.Mass = mass;
                nb.Velocity = velocity;
                nb.CanMove = (canmove != 0);
                nb.CanAttract = (canattract != 0);
                nb.Position = OSPosition2NBPosition(sop.AbsolutePosition);
            
                Tainted = true;
            }

            m_log.DebugFormat("[N-BodyGroup] add entity {0}",entityID);
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        public void RemoveEntity(UUID entityID)
        {
            lock (m_entityList)
            {
                if (m_entityList.ContainsKey(entityID))
                {
                    m_localIDList[m_entityList[entityID].LocalID] = UUID.Zero;
                    m_entityList.Remove(entityID);

                    Tainted = true;
                }
            }
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        public void Dump()
        {
            m_log.WarnFormat("[N-BodyGroup] dump entity list");
            
            lock (m_entityList)
            {
                //foreach (KeyValuePair<UUID,NBodyValue> kp in m_entityList)
                //    m_log.WarnFormat("[N-BodyGroup] PRE id={0}, mass={1}, velocity={2}, pos={3}",
                //        kp.Key,kp.Value.Mass,kp.Value.Velocity,kp.Value.Position);

                // iterate through the entities that can move
                foreach (KeyValuePair<UUID,NBodyValue> kpmover in m_entityList)
                {
                    if (! kpmover.Value.CanMove)
                        continue;
                
                    SceneObjectPart sopmover = m_scene.GetSceneObjectPart(kpmover.Key);
                    if (sopmover == null)
                        continue;

                    // Copy the position in from the scene
                    kpmover.Value.Position = OSPosition2NBPosition(sopmover.AbsolutePosition);
                    Vector3 velocity = ComputeInitialVelocity(kpmover.Value);
                
                    // iterate through the entities that can attract
                    foreach (KeyValuePair<UUID,NBodyValue> kpattractor in m_entityList)
                    {
                        if (! kpattractor.Value.CanAttract)
                            continue;

                        SceneObjectPart sopattractor = m_scene.GetSceneObjectPart(kpattractor.Key);
                        if (sopattractor == null)
                            continue;
                    
                        if (sopattractor == sopmover)
                            continue;
                    
                        // Copy the current position in from the scene
                        kpattractor.Value.Position = OSPosition2NBPosition(sopattractor.AbsolutePosition);

                        // Compute the change in velocity caused by this
                        VelocityPair vp = ComputeVelocityDelta(kpmover.Value,kpattractor.Value);
                        m_log.WarnFormat("[N-BodyGroup] {0}->{1} : {2}",kpmover.Key,kpattractor.Key,vp.ABVelocity);
                        velocity += vp.ABVelocity;
                    }

                    m_log.WarnFormat("[N-BodyGroup] ****** {0} : {1}",kpmover.Key,velocity);
                }
            }
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        public Int32 Update(double timestep)
        {
            Int32 curtick = Util.EnvironmentTickCount();
            float timedelta = (float)(timestep * TimeScale);

            lock (m_entityList)
            {
                int length = m_localIDList.Length;

                // Pass one... initialize all the velocities and update positions from the scene
                for (int i = 0; i < length; i++)
                {
                    UUID uuid = m_localIDList[i];
                    if (uuid == UUID.Zero)
                        continue;

                    SceneObjectPart sop = m_scene.GetSceneObjectPart(uuid);
                    if (sop == null)
                    {
                        m_entityList.Remove(uuid);
                        m_localIDList[i] = UUID.Zero;
                        continue;
                    }

                    NBodyValue value = m_entityList[uuid];
                    value.Position = OSPosition2NBPosition(sop.AbsolutePosition);
                    if (value.CanMove)
                        value.Velocity = ComputeInitialVelocity(value);
                }
                
                // Pass two... take care of an pre-computations that need to be done
                if (Tainted)
                {
                    HandleSimulationTaint();
                    Tainted = false;
                }
                
                // Pass three... compute the new velocities
                // for (int i = 0; i < length; i++)
                ParallelOptions opts = new ParallelOptions();
                opts.MaxDegreeOfParallelism = SimulationThreads;
                
                System.Threading.Tasks.Parallel.For(0,length,opts,i =>
                {
                    UUID idA = m_localIDList[i];
                    if (idA != UUID.Zero)
                    {
                        NBodyValue valA = m_entityList[idA];
                        UpdateVelocity(i,valA,timedelta);
                    }
                });

                // Pass four... send out the updates
                System.Threading.Tasks.Parallel.For(0,length,opts,i =>
                {
                    UUID idA = m_localIDList[i];
                    if (idA != UUID.Zero)
                    {
                        NBodyValue valA = m_entityList[idA];
                        UpdatePosition(valA,timedelta);
                    }
                });
            }

            return Util.EnvironmentTickCountSubtract(curtick);
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Compute the new velocity for one entity
        /// </summary>
        // -----------------------------------------------------------------
        protected void UpdateVelocity(int start, NBodyValue valA, float timedelta)
        {
            int length = m_localIDList.Length;
            for (int j = start + 1; j < length; j++)
            {
                UUID idB = m_localIDList[j];
                if (idB == UUID.Zero)
                    continue;
                        
                NBodyValue valB = m_entityList[idB];
                VelocityPair vp = ComputeVelocityDelta(valA,valB);

                if (valA.CanMove && valB.CanAttract)
                    valA.Velocity += vp.ABVelocity * timedelta;

                if (valB.CanMove && valA.CanAttract)
                {
                    Vector3 nv = valB.Velocity + (vp.BAVelocity * timedelta);
                    valB.Velocity = nv;
                }
            }
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Compute the new position
        /// This assumes velocity is in units/sec and timestep is proportion
        /// of a second; also assumes that the position in the velocity was
        /// updated "recently enough"
        /// </summary>
        // -----------------------------------------------------------------
        protected void UpdatePosition(NBodyValue val, float timedelta)
        {
            if (! val.CanMove)
                return;
                
            SceneObjectPart sop = m_scene.GetSceneObjectPart(val.Identity);
            if (sop == null)
                return;

            // If the object is selected, clear the velocity and move on...
            if (sop.ParentGroup.IsSelected)
            {
                val.Velocity = Vector3.Zero;
                return;
            }
                    
            val.Position += val.Velocity * timedelta;
            sop.UpdateGroupPosition(NBPosition2OSPosition(val.Position));
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        protected Vector3 NBPosition2OSPosition(Vector3 nbposition)
        {
            double nmag = Vector3.Mag(nbposition);
            if (nmag <= MaxRange / 2)
                return Vector3.Add(CenterPoint,nbposition);
            
            double mdelta = nmag - MaxRange / 2.0; 
            double omag = nmag * (1 - (mdelta * mdelta) / (nmag * nmag));
            
            return Vector3.Add(CenterPoint,nbposition * (float)(omag / nmag));
        }
    
        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        protected Vector3 OSPosition2NBPosition(Vector3 osposition)
        {
            Vector3 nbposition = Vector3.Subtract(osposition,CenterPoint);
            
            double omag = Vector3.Mag(nbposition);
            if (omag <= MaxRange / 2)
                return nbposition;
            
            if (omag >= MaxRange)
            {
                m_log.WarnFormat("[N-BodyGroup] position outside range, {0}",omag);
                omag = MaxRange - 0.00001;
            }
            
            double nmag = MaxRange * MaxRange / (4 * (MaxRange - omag));
            return nbposition * (float)(nmag / omag);
        }
        
        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        protected virtual VelocityPair ComputeVelocityDelta(NBodyValue v1, NBodyValue v2)
        {
            return new VelocityPair();
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        protected virtual Vector3 ComputeInitialVelocity(NBodyValue v1)
        {
            return v1.Velocity;
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        protected virtual void HandleSimulationTaint()
        {
        }
    }
    
    /// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    /// CLASS: CosineSimilarity
    /// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    public class CosineSimilarity : NBodyGroup
    {
        protected bool UseVelocity  { get; set; }
        protected bool UseMass { get; set; }
        protected float[][] CachedAttraction { get; set; }

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        public CosineSimilarity(Scene scene, bool usevelocity, bool usemass) : base(scene)
        {
            UseVelocity = usevelocity;
            UseMass = usemass;

            InitializeAttractionMatrix();
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        protected override Vector3 ComputeInitialVelocity(NBodyValue v1) 
        {
            return UseVelocity ? v1.Velocity : Vector3.Zero;
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        protected override void HandleSimulationTaint()
        {
            m_log.WarnFormat("[N-BodyGroup] bump cosine similarity matrix to {0}",NextLocalID);

            InitializeAttractionMatrix();
            
            for (int i = 0; i < NextLocalID-1; i++)
            {
                if (m_localIDList[i] == UUID.Zero)
                    continue;

                NBodyValue v1 = m_entityList[m_localIDList[i]];
                
                for (int j = i+1; j < NextLocalID; j++)
                {
                    if (m_localIDList[j] == UUID.Zero)
                        continue;
                    
                    NBodyValue v2 = m_entityList[m_localIDList[j]];

                    double cosforce = ComputeCosineAttraction(v1,v2);
                    CachedAttraction[i][j] = (float)(UseMass ? cosforce * v2.Mass : cosforce);
                    CachedAttraction[j][i] = (float)(UseMass ? cosforce * v1.Mass : cosforce);
                }
            }
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        protected override VelocityPair ComputeVelocityDelta(NBodyValue v1, NBodyValue v2)
        {
            UInt32 v1ID = v1.LocalID;
            UInt32 v2ID = v2.LocalID;

            Vector3 direction = Vector3.Normalize(v2.Position - v1.Position);

            VelocityPair vp = new VelocityPair();
            vp.ABVelocity = direction * CachedAttraction[(int)v1ID][(int)v2ID];
            vp.BAVelocity = (-direction) * CachedAttraction[(int)v2ID][(int)v1ID];
            return vp;
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        private void InitializeAttractionMatrix()
        {
            CachedAttraction = new float[NextLocalID][];
            for (int i = 0; i < NextLocalID; i++)
                CachedAttraction[i] = new float[NextLocalID];
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        private double ComputeCosineAttraction(NBodyValue v1, NBodyValue v2)
        {
            double c = 0;
            double vs1 = 0;
            double vs2 = 0;
            
            for (int i = 0; i < Dimension; i++)
            {
                vs1 += v1.Value[i] * v1.Value[i];
                vs2 += v2.Value[i] * v2.Value[i];
                c += v1.Value[i] * v2.Value[i];
            }
            
            // double force = c / Math.Sqrt(vs1 * vs2);
            double force = (c * c) / (vs1 * vs2);
            return force - 0.5;
        }
    }

    
    /// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    /// CLASS: NewtonianGravity
    /// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    public class NewtonianGravity : NBodyGroup
    {
        // protected const double GravitationalConstant = 6.67384E-11;
        protected const double GravitationalConstant = 1.0;
        

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        public NewtonianGravity(Scene scene) : base(scene) {}

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        protected override VelocityPair ComputeVelocityDelta(NBodyValue v1, NBodyValue v2)
        {
            VelocityPair vp = new VelocityPair();

            Vector3 direction = Vector3.Normalize(v2.Position-v1.Position);
            double dsquared = Vector3.DistanceSquared(v1.Position,v2.Position);
            if (dsquared == 0)
                dsquared = 0.0000000001;

            // F = s * (m1 * m2) / d^2
            // acceleration is the similarity attraction * mass of the attractor divided
            // by the distance separating the body and attractor
            double force = GravitationalConstant * v1.Mass * v2.Mass / dsquared;


            // a = F/m
            vp.ABVelocity = direction * (float)(force / v1.Mass);
            vp.BAVelocity = (-direction) * (float)(force / v2.Mass);
                    
            return vp;
        }
    }
}
