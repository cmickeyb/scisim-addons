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
using System.Collections.Generic;

using log4net;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;

using Dispatcher;
using Dispatcher.Messages;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RemoteControl.Messages
{
    // -----------------------------------------------------------------
    // ObjectQuery Messages
    // -----------------------------------------------------------------

    /// <summary>
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class FindObjectsRequest : Dispatcher.Messages.RequestBase
    {
        [JsonProperty]
        public String Pattern { get; set; }

        [JsonProperty]
        public Vector3 CoordinateA { get; set; }

        [JsonProperty]
        public Vector3 CoordinateB { get; set; }
        
        [JsonProperty]
        public UUID OwnerID { get; set; }

        public FindObjectsRequest()
        {
            CoordinateA = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            CoordinateB = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Pattern = String.Empty;
            OwnerID = UUID.Zero;
        }
    }

    /// <summary>
    ///    
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class FindObjectsResponse : Dispatcher.Messages.ResponseBase
    {
        [JsonProperty]
        public List<UUID> Objects { get; set; }
        
        public FindObjectsResponse() : base(ResponseCode.Success,"")
        {
            Objects = new List<UUID>();
        }
    }

    // -----------------------------------------------------------------
    // ObjectPosition Messages
    // -----------------------------------------------------------------

    /// <summary>
    ///    
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class PartInformation
    {
        [JsonProperty]
        public int LinkNum { get; set; }

        [JsonProperty]
        public string Name { get; set; }
        
        [JsonProperty]
        public string Description { get; set; }

        [JsonProperty]
        public OpenSim.Region.Framework.Scenes.PrimType Type { get; set; }
        
        [JsonProperty]
        public Vector3 OffsetPosition { get; set; }

        [JsonProperty]
        public Quaternion OffsetRotation { get; set; }

        public PartInformation()
        {
            LinkNum = 0;
            Name = "";
            Description = "";
            OffsetPosition = Vector3.Zero;
            OffsetRotation = Quaternion.Identity;
        }
    }
    
    /// <summary>
    ///    
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class GetObjectPartsRequest : Dispatcher.Messages.RequestBase
    {
        [JsonProperty]
        public UUID ObjectID { get; set; }

        public GetObjectPartsRequest()
        {
                    ObjectID = UUID.Zero;
        }
    }

    /// <summary>
    ///    
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class GetObjectPartsResponse : Dispatcher.Messages.ResponseBase
    {
        [JsonProperty]
        public List<PartInformation> Parts { get; set; }
        
        public GetObjectPartsResponse() : base(ResponseCode.Success,"")
        {
            Parts = new List<PartInformation>();
        }
    }

    /// <summary>
    ///    
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class GetObjectDataRequest : Dispatcher.Messages.RequestBase
    {
        [JsonProperty]
        public UUID ObjectID { get; set; }

        public GetObjectDataRequest()
        {
            ObjectID = UUID.Zero;
        }
    }

    /// <summary>
    ///    
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class GetObjectDataResponse : Dispatcher.Messages.ResponseBase
    {
        [JsonProperty]
        public UUID ObjectID { get; set; }

        [JsonProperty]
        public String Name { get; set; }

        [JsonProperty]
        public Vector3 Position { get; set; }

        [JsonProperty]
        public Quaternion Rotation { get; set; }

        [JsonProperty]
        public UUID OwnerID { get; set; }

        public GetObjectDataResponse(SceneObjectGroup sog) : base(ResponseCode.Success,"")
        {
            ObjectID = sog.UUID;
            Name = sog.Name;
            Position = sog.AbsolutePosition;
            Rotation = sog.GroupRotation;
            OwnerID = sog.OwnerID;
        }
    }

    /// <summary>
    ///    
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class GetObjectPositionRequest : Dispatcher.Messages.RequestBase
    {
        [JsonProperty]
        public UUID ObjectID { get; set; }

        public GetObjectPositionRequest()
        {
            ObjectID = UUID.Zero;
        }
    }

    /// <summary>
    ///    
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class SetObjectPositionRequest : Dispatcher.Messages.RequestBase
    {
        [JsonProperty]
        public UUID ObjectID { get; set; }

        [JsonProperty]
        public Vector3 Position { get; set; }

        public SetObjectPositionRequest()
        {
            ObjectID = UUID.Zero;
            Position = new Vector3(128.0f, 128.0f, 20.0f);
        }
    }

    /// <summary>
    ///    
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class ObjectPositionResponse : Dispatcher.Messages.ResponseBase
    {
        [JsonProperty]
        public UUID ObjectID { get; set; }

        [JsonProperty]
        public Vector3 Position { get; set; }

        public ObjectPositionResponse(UUID oid, Vector3 pos) : base(ResponseCode.Success,"")
        {
            ObjectID = oid;
            Position = pos;
        }
    }

    // -----------------------------------------------------------------
    // ObjectRotation Messages
    // -----------------------------------------------------------------

    /// <summary>
    ///    
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class GetObjectRotationRequest : Dispatcher.Messages.RequestBase
    {
        [JsonProperty]
        public UUID ObjectID { get; set; }

        public GetObjectRotationRequest()
        {
            ObjectID = UUID.Zero;
        }
    }

    /// <summary>
    ///    
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class SetObjectRotationRequest : Dispatcher.Messages.RequestBase
    {
        [JsonProperty]
        public UUID ObjectID { get; set; }

        [JsonProperty]
        public Quaternion Rotation { get; set; }

        public SetObjectRotationRequest()
        {
            ObjectID = UUID.Zero;
            Rotation = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
        }
    }

    /// <summary>
    ///    
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class ObjectRotationResponse : Dispatcher.Messages.ResponseBase
    {
        [JsonProperty]
        public UUID ObjectID { get; set; }

        [JsonProperty]
        public Quaternion Rotation { get; set; }

        public ObjectRotationResponse(UUID oid, Quaternion rot) : base(ResponseCode.Success,"")
        {
            ObjectID = oid;
            Rotation = rot;
        }
    }

    // -----------------------------------------------------------------
    // Object Creation Messages
    // -----------------------------------------------------------------

    /// <summary>
    ///    
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class DeleteAllObjectsRequest : Dispatcher.Messages.RequestBase
    {
        public DeleteAllObjectsRequest() {}
    }

    /// <summary>
    ///    
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class DeleteObjectRequest : Dispatcher.Messages.RequestBase
    {
        [JsonProperty]
        public UUID ObjectID { get; set; }

        public DeleteObjectRequest()
        {
            ObjectID = UUID.Zero;
        }
    }
    
    /// <summary>
    ///    
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class CreateObjectRequest : Dispatcher.Messages.RequestBase
    {
        [JsonProperty]
        public String Name { get; set; }

        [JsonProperty]
        public String Description { get; set; }

        [JsonProperty]
        public String StartParameter { get; set; }

        [JsonProperty]
        public Vector3 Position { get; set; }

        [JsonProperty]
        public Quaternion Rotation { get; set; }

        [JsonProperty]
        public Vector3 Velocity { get; set; }

        [JsonProperty]
        public UUID AssetID { get; set; }

        public CreateObjectRequest()
        {
            Name = "RemoteControl Object";
            Description = "RemoteControl Generated Object";
            StartParameter = "{}";
            Position = new Vector3(128.0f, 128.0f, 20.0f);
            Rotation = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
            Velocity = new Vector3(0.0f, 0.0f, 0.0f);
            AssetID = UUID.Zero;
        }
    }

    /// <summary>
    ///    
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class CreateObjectResponse : Dispatcher.Messages.ResponseBase
    {
        [JsonProperty]
        public UUID ObjectID { get; set; }

        public CreateObjectResponse(UUID oid) : base(ResponseCode.Success,"")
        {
            ObjectID = oid;
        }
    }

    // -----------------------------------------------------------------
    // Object Communication Messages
    // -----------------------------------------------------------------

    /// <summary>
    ///    
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class MessageObjectRequest : Dispatcher.Messages.RequestBase
    {
        [JsonProperty]
        public UUID ObjectID { get; set; }

        [JsonProperty]
        public String Message { get; set; }

        public MessageObjectRequest()
        {
            ObjectID = UUID.Zero;
            Message = "";
        }
    }
}


