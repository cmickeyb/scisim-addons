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
using System.Text;

using log4net;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

using OpenSim.Framework;

using Dispatcher;
using Dispatcher.Messages;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RemoteControl.Messages
{
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class RegisterTouchCallbackRequest : RequestBase
    {
        [JsonProperty]
        public UUID ObjectID { get; set; }

        [JsonProperty]
        public UUID EndPointID { get; set; }

        public RegisterTouchCallbackRequest()
        {
            ObjectID = UUID.Zero;
            EndPointID = UUID.Zero;
        }
    }

    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class RegisterTouchCallbackResponse : ResponseBase
    {
        [JsonProperty]
        public UUID RequestID { get; set; }

        public RegisterTouchCallbackResponse(UUID req) : base(ResponseCode.Success,"")
        {
            RequestID = req;
        }
    }
    
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class UnregisterTouchCallbackRequest : RequestBase
    {
        [JsonProperty]
        public UUID ObjectID { get; set; }

        [JsonProperty]
        public UUID RequestID { get; set; }
            
        public UnregisterTouchCallbackRequest()
        {
            ObjectID = UUID.Zero;
            RequestID = UUID.Zero;
        }
    }

    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class TouchCallback : CallbackBase
    {
        [JsonProperty]
        public UUID ObjectID { get; set; }

        [JsonProperty]
        public UUID RequestID { get; set; }
            
        [JsonProperty]
        public UUID AvatarID { get; set; }

        [JsonProperty]
        public Vector3 OffsetPosition { get; set; }

        [JsonProperty]
        public SurfaceTouchEventArgs TouchEvent { get; set; }

        public TouchCallback(UUID obj, UUID req, UUID avt, Vector3 off, SurfaceTouchEventArgs ev) 
        {
            ObjectID = obj;
            RequestID = req;
            AvatarID = avt;
            OffsetPosition = off;
            TouchEvent = ev;
        }
    }

    
}


