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
using System.Text;
using System.Collections.Generic;

using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
            
using Dispatcher;
using Dispatcher.Messages;

using RemoteControl.Messages;

namespace RemoteControl.Handlers
{
    public class RegionHandlers : BaseHandler
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private ISunModule m_SunModule;

#region BaseHandler Methods
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public RegionHandlers(Scene scene, IDispatcherModule dispatcher, string domain) : base(scene,dispatcher,domain)
        {}

        /// -----------------------------------------------------------------
        /// <summary>
        /// need to remove all references to the scene in the subscription
        /// list to enable full garbage collection of the scene object
        /// </summary>
        /// -----------------------------------------------------------------
        public override void UnregisterHandlers()
        {
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(GetSunParametersRequest));
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(SetSunParametersRequest));
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Called when all modules have been added for a region. This is 
        /// where we hook up events
        /// </summary>
        // -----------------------------------------------------------------
        public override void RegisterHandlers()
        {
            m_SunModule = m_scene.RequestModuleInterface<ISunModule>();
            if (m_SunModule == null)
            {
                m_log.WarnFormat("[RegionHandlers] unable to find sun module provider");
            }
            else
            {
                m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(GetSunParametersRequest),GetSunParametersHandler);
                m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(SetSunParametersRequest),SetSunParametersHandler);
            }
        }
#endregion

#region Communication Handlers
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase GetSunParametersHandler(Dispatcher.Messages.RequestBase request)
        {
            if (request.GetType() != typeof(GetSunParametersRequest))
                return OperationFailed("wrong type");

            GetSunParametersRequest req = (GetSunParametersRequest)request;
            SunParametersResponse res = new SunParametersResponse();
            res.YearLength = m_SunModule.GetSunParameter("year_length");
            res.DayLength = m_SunModule.GetSunParameter("day_length");
            res.HorizonShift = m_SunModule.GetSunParameter("day_night_offset");
            res.DayTimeSunHourScale = m_SunModule.GetSunParameter("day_time_sun_hour_scale");
            res.CurrentTime = m_SunModule.GetSunParameter("current_time");

            return res;
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase SetSunParametersHandler(Dispatcher.Messages.RequestBase request)
        {
            if (request.GetType() != typeof(SetSunParametersRequest))
                return OperationFailed("wrong type");

            SetSunParametersRequest req = (SetSunParametersRequest)request;

            if (req.YearLength > 0)
                m_SunModule.SetSunParameter("year_length", req.YearLength);

            if (req.DayLength > 0)
                m_SunModule.SetSunParameter("day_length",req.DayLength);

            if (req.HorizonShift > 0)
                m_SunModule.SetSunParameter("day_night_offset",req.HorizonShift);

            if (req.DayTimeSunHourScale > 0)
                m_SunModule.SetSunParameter("day_time_sun_hour_scale",req.DayTimeSunHourScale);

            if (req.CurrentTime > 0)
                m_SunModule.SetSunParameter("current_time",req.CurrentTime);

            SunParametersResponse res = new SunParametersResponse();
            res.YearLength = m_SunModule.GetSunParameter("year_length");
            res.DayLength = m_SunModule.GetSunParameter("day_length");
            res.HorizonShift = m_SunModule.GetSunParameter("day_night_offset");
            res.DayTimeSunHourScale = m_SunModule.GetSunParameter("day_time_sun_hour_scale");
            res.CurrentTime = m_SunModule.GetSunParameter("current_time");

            return res;
        }
        
#endregion
    }
}
