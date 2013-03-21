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
using System.Collections;
using System.Collections.Generic;

using OpenMetaverse;
using OpenMetaverse.StructuredData;

using log4net;
using Dispatcher;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using OpenSim.Framework;

namespace Dispatcher.Messages
{
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class CreateCapabilityRequest : RequestBase
    {
        [JsonProperty]
        public UUID UserID { get; set; }

        [JsonProperty]
        public string HashedPasswd { get; set; }

        [JsonProperty]
        public double LifeSpan { get; set; }

        [JsonProperty]
        public String FirstName { get; set; }

        [JsonProperty]
        public String LastName { get; set; }

        [JsonProperty]
        public String EmailAddress { get; set; }

        [JsonProperty]
        public List<String> DomainList { get; set; }
        
        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        public CreateCapabilityRequest()
        {
            UserID = UUID.Zero;
            HashedPasswd = "";
            LifeSpan = 300.0;
            FirstName = "";
            LastName = "";
            EmailAddress = "";
            DomainList = new List<String>();
        }
    }
    
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class RenewCapabilityRequest : RequestBase
    {
        [JsonProperty]
        public double LifeSpan { get; set; }

        [JsonProperty]
        public List<String> DomainList { get; set; }
        
        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        public RenewCapabilityRequest()
        {
            LifeSpan = 30;
            DomainList = new List<String>();
        }
    }
    
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class CapabilityResponse : ResponseBase
    {
        [JsonProperty]
        public UUID Capability { get; set; }

        [JsonProperty]
        public double LifeSpan { get; set; }

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        public CapabilityResponse(UUID cap, double lifespan) : base(ResponseCode.Success,"")
        {
            Capability = cap;
            LifeSpan = lifespan;
        }
    }
}
