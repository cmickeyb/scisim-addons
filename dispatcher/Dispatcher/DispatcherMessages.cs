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
using OpenSim.Framework.Monitoring;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

using System.Collections;
using System.Collections.Generic;

using System.Runtime.Serialization;

using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Dispatcher.Messages
{
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    public enum ResponseCode
    {
        Failure = 0,
        Success = 1,
        Queued = 2
    }

    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    public class MessageBase
    {
        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        protected static Dictionary<Type, string> TypeToName = new Dictionary<Type, string>();
        protected static Dictionary<string, Type> NameToType = new Dictionary<string, Type>();
        
        public static void RegisterMessageType(Type messagetype)
        {
            TypeToName[messagetype] = messagetype.FullName;
            NameToType[messagetype.FullName] = messagetype;
        }

        public static void UnregisterMessageType(Type messagetype)
        {
            if (TypeToName.ContainsKey(messagetype))
                TypeToName.Remove(messagetype);
            if (NameToType.ContainsKey(messagetype.FullName))
                NameToType.Remove(messagetype.FullName);
        }
                
        public static bool FindRegisteredTypeByName(string fullname, out Type messagetype)
        {
            return NameToType.TryGetValue(fullname,out messagetype);
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        protected static float Convert2Float(JsonReader reader)
        {
            switch (reader.TokenType)
            {
            case JsonToken.Float:
                return Convert.ToSingle((double)reader.Value);

            case JsonToken.Integer:
                return Convert.ToSingle((Int64)reader.Value);
                    
            case JsonToken.String:
                return Convert.ToSingle((string)reader.Value);

            default:
                throw new Exception(String.Format("Unable to convert token type {0} to float",reader.TokenType));
            }

            return 0.0f;
        }
            
        // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        public class UUIDConverter : JsonConverter
        {
            // -----------------------------------------------------------------
            /// <summary>
            /// 
            /// </summary>
            // -----------------------------------------------------------------
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value.GetType() != typeof(UUID))
                    return;

                UUID id = (UUID)value;
                writer.WriteValue(id.ToString());
            }
        
            // -----------------------------------------------------------------
            /// <summary>
            /// 
            /// </summary>
            // -----------------------------------------------------------------
            public override object ReadJson(JsonReader reader, Type objectType, object existingObject, JsonSerializer serializer)
            {
                if (objectType != typeof(UUID))
                    return null;

                UUID cobj = (UUID)existingObject;
                cobj = UUID.Zero;
                if (reader.TokenType == JsonToken.String)
                {
                    UUID id;
                    if (UUID.TryParse((string)reader.Value, out id))
                        cobj = id;
                }

                return cobj;
            }

            // -----------------------------------------------------------------
            /// <summary>
            /// 
            /// </summary>
            // -----------------------------------------------------------------
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(UUID);
            }
        }
    
        // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        public class Vector3Converter : JsonConverter
        {
            // -----------------------------------------------------------------
            /// <summary>
            /// 
            /// </summary>
            // -----------------------------------------------------------------
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value.GetType() != typeof(Vector3))
                    return;

                Vector3 cobj = (Vector3)value;
                writer.WriteStartArray();
                writer.WriteValue(cobj.X);
                writer.WriteValue(cobj.Y);
                writer.WriteValue(cobj.Z);
                writer.WriteEndArray();
            }

            // -----------------------------------------------------------------
            /// <summary>
            /// 
            /// </summary>
            // -----------------------------------------------------------------
            public override object ReadJson(JsonReader reader, Type objectType, object existingObject, JsonSerializer serializer)
            {
                if (objectType != typeof(Vector3))
                    return null;

                Vector3 cobj = (Vector3)existingObject;
                if (reader.TokenType == JsonToken.StartArray)
                {
                    reader.Read();
                    cobj.X = Convert2Float(reader);

                    reader.Read();
                    cobj.Y = Convert2Float(reader);
                    
                    reader.Read();
                    cobj.Z = Convert2Float(reader);

                    reader.Read();
                    while (reader.TokenType != JsonToken.EndArray)
                        reader.Read();
                }

                return cobj;
            }

            // -----------------------------------------------------------------
            /// <summary>
            /// 
            /// </summary>
            // -----------------------------------------------------------------
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vector3);
            }
        }

        // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        public class QuaternionConverter : JsonConverter
        {
            // -----------------------------------------------------------------
            /// <summary>
            /// 
            /// </summary>
            // -----------------------------------------------------------------
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value.GetType() != typeof(Quaternion))
                    return;

                Quaternion cobj = (Quaternion)value;
                writer.WriteStartArray();
                writer.WriteValue(cobj.X);
                writer.WriteValue(cobj.Y);
                writer.WriteValue(cobj.Z);
                writer.WriteValue(cobj.W);
                writer.WriteEndArray();
            }

            // -----------------------------------------------------------------
            /// <summary>
            /// 
            /// </summary>
            // -----------------------------------------------------------------
            public override object ReadJson(JsonReader reader, Type objectType, object existingObject, JsonSerializer serializer)
            {
                if (objectType != typeof(Quaternion))
                    return null;

                Quaternion cobj = (Quaternion)existingObject;
                if (reader.TokenType == JsonToken.StartArray)
                {
                    reader.Read();
                    cobj.X = Convert2Float(reader);

                    reader.Read();
                    cobj.Y = Convert2Float(reader);

                    reader.Read();
                    cobj.Z = Convert2Float(reader);

                    reader.Read();
                    cobj.W = Convert2Float(reader);

                    reader.Read();
                    while (reader.TokenType != JsonToken.EndArray)
                        reader.Read();
                }

                return cobj;
            }

            // -----------------------------------------------------------------
            /// <summary>
            /// 
            /// </summary>
            // -----------------------------------------------------------------
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Quaternion);
            }
        }

        // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        public class TypeNameSerializationBinder : SerializationBinder
        {
            public string TypeFormat { get; private set; }

             // -----------------------------------------------------------------
            /// <summary>
            /// 
            /// </summary>
            // -----------------------------------------------------------------
            public TypeNameSerializationBinder(string typeFormat)
            {
                TypeFormat = typeFormat;
            }

            // -----------------------------------------------------------------
            /// <summary>
            /// 
            /// </summary>
            // -----------------------------------------------------------------
            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = null;
                if (TypeToName.TryGetValue(serializedType, out typeName))
                    return;
        
                typeName = serializedType.Name;
            }

            // -----------------------------------------------------------------
            /// <summary>
            /// 
            /// </summary>
            // -----------------------------------------------------------------
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (NameToType.ContainsKey(typeName))
                    return NameToType[typeName];
                 string resolvedTypeName = string.Format(TypeFormat, typeName, assemblyName);
                return Type.GetType(resolvedTypeName, true);
            }
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        public static RequestBase DeserializeFromBinaryStream(Stream bstream)
        {
            List<JsonConverter> clist = new List<JsonConverter>();
            clist.Add(new Vector3Converter());
            clist.Add(new UUIDConverter());
            clist.Add(new QuaternionConverter());
        
            TypeNameSerializationBinder binder = new TypeNameSerializationBinder("{0}");
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Binder = binder;
            settings.TypeNameHandling = TypeNameHandling.Objects;
            settings.Converters = clist;

            JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(settings);

            BsonReader bson = new BsonReader(bstream);

            object result = jsonSerializer.Deserialize(bson, null);
            if (result == null)
                return null;
        
            return ((RequestBase)result);
        }

        public static RequestBase DeserializeFromBinaryData(byte[] data)
        {
            MemoryStream ms = new MemoryStream(data);
            return DeserializeFromBinaryStream(ms);
        }
        
        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        public byte[] SerializeToBinaryData()
        {
            List<JsonConverter> clist = new List<JsonConverter>();
            clist.Add(new Vector3Converter());
            clist.Add(new UUIDConverter());
            clist.Add(new QuaternionConverter());

            // The type name handling is pretty verbose here, but makes deserialization
            // symmetric, its enough information to figure out how to handle the structures
            TypeNameSerializationBinder binder = new TypeNameSerializationBinder("{0}, {1}");
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Binder = binder;
            settings.TypeNameHandling = TypeNameHandling.Objects;
            settings.Converters = clist;

            JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(settings);

            MemoryStream ms = new MemoryStream();
            BsonWriter bson = new BsonWriter(ms);
            
            jsonSerializer.Serialize(bson, this, null);
            return ms.ToArray();
        }
        
        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        public static RequestBase DeserializeFromString(string source)
        {
            List<JsonConverter> clist = new List<JsonConverter>();
            clist.Add(new Vector3Converter());
            clist.Add(new UUIDConverter());
            clist.Add(new QuaternionConverter());
        
            TypeNameSerializationBinder binder = new TypeNameSerializationBinder("{0}");
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Binder = binder;
            settings.TypeNameHandling = TypeNameHandling.Objects;
            settings.Converters = clist;
             object result = JsonConvert.DeserializeObject(source,settings);
            if (result == null)
                return null;
        
            return ((RequestBase)result);
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        public string SerializeToString()
        {
            List<JsonConverter> clist = new List<JsonConverter>();
            clist.Add(new Vector3Converter());
            clist.Add(new UUIDConverter());
            clist.Add(new QuaternionConverter());

            // The type name handling is pretty verbose here, but makes deserialization
            // symmetric, its enough information to figure out how to handle the structures
            TypeNameSerializationBinder binder = new TypeNameSerializationBinder("{0}, {1}");
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Binder = binder;
            settings.TypeNameHandling = TypeNameHandling.Objects;
            settings.Converters = clist;

            return JsonConvert.SerializeObject(this, Formatting.None, settings);
        }
    }
    
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class RequestBase : MessageBase
    {
        [JsonProperty]
        public string _Scene { get; set; }

        [JsonProperty]
        public string _Domain { get; set; }

        [JsonProperty]
        public UUID _Capability { get; set; }
            
        [JsonProperty]
        public bool _AsyncRequest { get; set; }
            
        // This is filled in by the dispatcher
        public UserAccount _UserAccount { get; set; }

        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        public RequestBase()
        {
            _Scene = "";
            _Domain = "";
            _UserAccount = null;
            _AsyncRequest = false;
        }
        
    }

    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class ResponseBase : MessageBase
    {

        [JsonProperty]
        public ResponseCode _Success { get; set; }

        [JsonProperty]
        public string _Message { get; set; }
        
        // -----------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        // -----------------------------------------------------------------
        public ResponseBase(ResponseCode result, string msg)
        {
            _Success = result;
            _Message = msg;
        }

        public ResponseBase() : this(ResponseCode.Success,"") {}
    }

    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class CallbackBase : MessageBase
    {
        // -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public virtual bool Prepare() { return true;  }
        
        // -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public CallbackBase() {}
    }
}
