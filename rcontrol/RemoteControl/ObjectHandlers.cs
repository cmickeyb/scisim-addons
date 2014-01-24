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
using System.Text.RegularExpressions;
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
    public class ObjectHandlers : BaseHandler
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IJsonStoreModule m_jsonstore = null;

#region BaseHandler Methods
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public ObjectHandlers(Scene scene, IDispatcherModule dispatcher, string domain) : base(scene,dispatcher,domain)
        {
            m_jsonstore = m_scene.RequestModuleInterface<IJsonStoreModule>();
            if (m_jsonstore == null)
                m_log.WarnFormat("[RemoteControModule] IJsonStoreModule interface not defined");
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// need to remove all references to the scene in the subscription
        /// list to enable full garbage collection of the scene object
        /// </summary>
        /// -----------------------------------------------------------------
        public override void UnregisterHandlers()
        {
            //m_log.DebugFormat("[ObjectHandlers] unregister methods");

            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(FindObjectsRequest));

            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(GetObjectDataRequest));
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(GetObjectInventoryRequest));
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(GetObjectPositionRequest));
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(SetObjectPositionRequest));
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(GetObjectRotationRequest));
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(SetObjectRotationRequest));
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(MessageObjectRequest));
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(BulkDynamicsRequest));

            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(GetObjectPartsRequest));

            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(CreateObjectRequest));
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(DeleteObjectRequest));
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(DeleteAllObjectsRequest));
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Called when all modules have been added for a region. This is 
        /// where we hook up events
        /// </summary>
        // -----------------------------------------------------------------
        public override void RegisterHandlers()
        {
            //m_log.WarnFormat("[ObjectHandlers] register methods");
            
            m_dispatcher.RegisterMessageType(typeof(FindObjectsResponse));
            m_dispatcher.RegisterMessageType(typeof(GetObjectPartsResponse));
            m_dispatcher.RegisterMessageType(typeof(GetObjectDataResponse));
            m_dispatcher.RegisterMessageType(typeof(GetObjectInventoryResponse));
            m_dispatcher.RegisterMessageType(typeof(ObjectPositionResponse));
            m_dispatcher.RegisterMessageType(typeof(ObjectRotationResponse));
            m_dispatcher.RegisterMessageType(typeof(CreateObjectResponse));

            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(FindObjectsRequest),FindObjectsHandler);

            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(GetObjectDataRequest),GetObjectDataHandler);
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(GetObjectInventoryRequest),GetObjectInventoryHandler);
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(GetObjectPositionRequest),GetObjectPositionHandler);
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(SetObjectPositionRequest),SetObjectPositionHandler);
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(GetObjectRotationRequest),GetObjectRotationHandler);
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(SetObjectRotationRequest),SetObjectRotationHandler);
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(MessageObjectRequest),MessageObjectHandler);
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(BulkDynamicsRequest),SetDynamicsHandler);

            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(GetObjectPartsRequest),GetObjectPartsHandler);

            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(CreateObjectRequest),CreateObjectHandler);
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(DeleteObjectRequest),DeleteObjectHandler);
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(DeleteAllObjectsRequest),DeleteAllObjectsHandler);
        }
#endregion

#region Object Handlers
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase FindObjectsHandler(Dispatcher.Messages.RequestBase irequest)
        {
            if (irequest.GetType() != typeof(FindObjectsRequest))
                return OperationFailed("wrong type of request object");
            
            FindObjectsRequest request = (FindObjectsRequest)irequest;

            // Set up the bounding box such that min.X <= max.X, min.Y <= max.Y, min.Z <= max.Z
            Vector3 min = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 max = new Vector3(Constants.RegionSize, Constants.RegionSize, float.MaxValue);

            min.X = Math.Min(request.CoordinateA.X, request.CoordinateB.X);
            min.Y = Math.Min(request.CoordinateA.Y, request.CoordinateB.Y);
            min.Z = Math.Min(request.CoordinateA.Z, request.CoordinateB.Z);

            max.X = Math.Max(request.CoordinateA.X,request.CoordinateB.X);
            max.Y = Math.Max(request.CoordinateA.Y,request.CoordinateB.Y);
            max.Z = Math.Max(request.CoordinateA.Z,request.CoordinateB.Z);

            // Set up the pattern
            Regex pattern = null;
            if (! String.IsNullOrEmpty(request.Pattern))
                pattern = new Regex(request.Pattern);
                
            Predicate<SceneObjectGroup> pred = sog => SearchPredicate(sog, min, max, pattern, request.OwnerID);
            List<SceneObjectGroup> sceneObjects = m_scene.GetSceneObjectGroups().FindAll(pred);

            FindObjectsResponse resp = new FindObjectsResponse();
            foreach (SceneObjectGroup sog in sceneObjects)
                resp.Objects.Add(sog.UUID);

            return resp;
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase GetObjectPartsHandler(Dispatcher.Messages.RequestBase irequest)
        {
            if (irequest.GetType() != typeof(GetObjectPartsRequest))
                return OperationFailed("wrong type of request object");
            
            GetObjectPartsRequest request = (GetObjectPartsRequest)irequest;

            SceneObjectGroup sog = m_scene.GetSceneObjectGroup(request.ObjectID);
            if (sog == null)
                return OperationFailed("no such object");

            GetObjectPartsResponse response = new GetObjectPartsResponse();
            foreach (SceneObjectPart part in sog.Parts)
            {
                PartInformation info = new PartInformation();
                info.LinkNum = part.LinkNum;
                info.Name = part.Name;
                info.Description = part.Description;
                info.OffsetPosition = part.OffsetPosition;
                info.OffsetRotation = part.RotationOffset;
                info.Type = part.GetPrimType();
                
                response.Parts.Add(info);
            }
            
            return response;
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase DeleteObjectHandler(Dispatcher.Messages.RequestBase irequest)
        {
            if (irequest.GetType() != typeof(DeleteObjectRequest))
                return OperationFailed("wrong type of request object");
            
            DeleteObjectRequest request = (DeleteObjectRequest)irequest;

            SceneObjectGroup sog = m_scene.GetSceneObjectGroup(request.ObjectID);
            if (sog == null)
                return OperationFailed("no such object");

            m_scene.DeleteSceneObject(sog,false,true);

            return new Dispatcher.Messages.ResponseBase(ResponseCode.Success,"");
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase DeleteAllObjectsHandler(Dispatcher.Messages.RequestBase irequest)
        {
            if (irequest.GetType() != typeof(DeleteAllObjectsRequest))
                return OperationFailed("wrong type of request object");
            
            DeleteAllObjectsRequest request = (DeleteAllObjectsRequest)irequest;
            m_scene.DeleteAllSceneObjects();

            return new Dispatcher.Messages.ResponseBase(ResponseCode.Success,"");
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase CreateObjectHandler(Dispatcher.Messages.RequestBase irequest)
        {
            if (irequest.GetType() != typeof(CreateObjectRequest))
                return OperationFailed("wrong type of request object");
            
            CreateObjectRequest request = (CreateObjectRequest)irequest;
            
            if (request.AssetID == UUID.Zero)
                return OperationFailed("missing asset id");
            
            SceneObjectGroup sog = null;

            try
            {
                sog = GetRezReadySceneObject(request.AssetID, request.Name, request.Description,
                                             request._UserAccount.PrincipalID, UUID.Zero);
                if (sog == null)
                    return OperationFailed("unable to create object from asset");

                if (request.ObjectID != UUID.Zero)
                    sog.UUID = request.ObjectID;
                
                if (! String.IsNullOrEmpty(request.StartParameter))
                {
                    if (m_jsonstore != null)
                    {
                        // really should register an event handler on the scene to destroy this
                        // store when we are done
                        UUID storeID = sog.UUID;
                        m_jsonstore.CreateStore(request.StartParameter,ref storeID);
                    }
                }

                if (! m_scene.AddNewSceneObject(sog,false,request.Position,request.Rotation,request.Velocity))
                    return OperationFailed("failed to add the object to the scene");

                sog.CreateScriptInstances(0,true,m_scene.DefaultScriptEngine,3);
                sog.ScheduleGroupForFullUpdate();
            }
            catch (Exception e)
            {
                return OperationFailed(e.Message);
            }
                
            return new CreateObjectResponse(sog.UUID);
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase MessageObjectHandler(Dispatcher.Messages.RequestBase irequest)
        {
            if (irequest.GetType() != typeof(MessageObjectRequest))
                return OperationFailed("wrong type");

            MessageObjectRequest request = (MessageObjectRequest)irequest;

            IScriptModule m_scriptModule = m_scene.RequestModuleInterface<IScriptModule>();
            if (m_scriptModule == null)
                return OperationFailed("unable to locate appropriate handler");
            
            SceneObjectGroup sog = m_scene.GetSceneObjectGroup(request.ObjectID);
            if (sog == null)
                return OperationFailed("no such object");

            object[] args = new object[] { request._UserAccount.PrincipalID.ToString(), request.Message };
            m_scriptModule.PostObjectEvent(sog.RootPart.UUID, "dataserver", args);
            
            return new Dispatcher.Messages.ResponseBase(ResponseCode.Success,"");
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase GetObjectDataHandler(Dispatcher.Messages.RequestBase irequest)
        {
            if (irequest.GetType() != typeof(GetObjectDataRequest))
                return OperationFailed("wrong type");

            GetObjectDataRequest request = (GetObjectDataRequest)irequest;

            SceneObjectGroup sog = m_scene.GetSceneObjectGroup(request.ObjectID);
            if (sog == null)
                return OperationFailed("no such object");

            return new GetObjectDataResponse(sog);
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase GetObjectInventoryHandler(Dispatcher.Messages.RequestBase irequest)
        {
            if (irequest.GetType() != typeof(GetObjectInventoryRequest))
                return OperationFailed("wrong type");

            GetObjectInventoryRequest request = (GetObjectInventoryRequest)irequest;

            SceneObjectGroup sog = m_scene.GetSceneObjectGroup(request.ObjectID);
            if (sog == null)
                return OperationFailed("no such object");

            GetObjectInventoryResponse response = new GetObjectInventoryResponse();
            lock (sog.RootPart.TaskInventory)
            {
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in sog.RootPart.TaskInventory)
                {
                    ObjectInventoryInformation item = new ObjectInventoryInformation(inv.Value);
                    response.Inventory.Add(item);
                }
            }

            return response;
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase GetObjectPositionHandler(Dispatcher.Messages.RequestBase irequest)
        {
            if (irequest.GetType() != typeof(GetObjectPositionRequest))
                return OperationFailed("wrong type");

            GetObjectPositionRequest request = (GetObjectPositionRequest)irequest;

            SceneObjectGroup sog = m_scene.GetSceneObjectGroup(request.ObjectID);
            if (sog == null)
                return OperationFailed("no such object");

            return new ObjectPositionResponse(request.ObjectID,sog.AbsolutePosition);
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase SetObjectPositionHandler(Dispatcher.Messages.RequestBase irequest)
        {
            if (irequest.GetType() != typeof(SetObjectPositionRequest))
                return OperationFailed("wrong type");

            SetObjectPositionRequest request = (SetObjectPositionRequest)irequest;
            
            SceneObjectGroup sog = m_scene.GetSceneObjectGroup(request.ObjectID);
            if (sog == null)
                return OperationFailed("no such object");

            sog.UpdateGroupPosition(request.Position);

            return new ObjectPositionResponse(request.ObjectID,sog.AbsolutePosition);
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase GetObjectRotationHandler(Dispatcher.Messages.RequestBase irequest)
        {
            if (irequest.GetType() != typeof(GetObjectRotationRequest))
                return OperationFailed("wrong type");

            GetObjectRotationRequest request = (GetObjectRotationRequest)irequest;

            SceneObjectGroup sog = m_scene.GetSceneObjectGroup(request.ObjectID);
            if (sog == null)
                return OperationFailed("no such object");

            return new ObjectRotationResponse(request.ObjectID,sog.GroupRotation);
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase SetObjectRotationHandler(Dispatcher.Messages.RequestBase irequest)
        {
            if (irequest.GetType() != typeof(SetObjectRotationRequest))
                return OperationFailed("wrong type");

            SetObjectRotationRequest request = (SetObjectRotationRequest)irequest;
            
            SceneObjectGroup sog = m_scene.GetSceneObjectGroup(request.ObjectID);
            if (sog == null)
                return OperationFailed("no such object");

            sog.UpdateGroupRotationR(request.Rotation);

            return new ObjectRotationResponse(request.ObjectID,sog.GroupRotation);
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public Dispatcher.Messages.ResponseBase SetDynamicsHandler(Dispatcher.Messages.RequestBase irequest)
        {
            if (irequest.GetType() != typeof(BulkDynamicsRequest))
                return OperationFailed("wrong type of request object");
            
            BulkDynamicsRequest request = (BulkDynamicsRequest)irequest;

            foreach (ObjectDynamicsData ddata in request.Updates)
            {
                SceneObjectGroup sog = m_scene.GetSceneObjectGroup(ddata.ObjectID);
                if (sog == null)
                {
                    m_log.WarnFormat("[ObjectHandlers] missing requested object; {0}",ddata.ObjectID.ToString());
                    continue;
                }

                sog.RootPart.Velocity = ddata.Velocity;
                sog.RootPart.Acceleration = ddata.Acceleration;
                sog.UpdateGroupRotationPR(ddata.Position, ddata.Rotation);
            }

            return new ResponseBase(ResponseCode.Success,"");
        }

#endregion
#region SupportFunctions
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        private SceneObjectGroup GetRezReadySceneObject(UUID assetID, string name, string description, UUID ownerID, UUID groupID)
        {
            AssetBase rezAsset = m_scene.AssetService.Get(assetID.ToString());

            if (null == rezAsset)
            {
                m_log.WarnFormat("[ObjectHandlers]: Could not find asset {0}",assetID.ToString());
                return null;
            }

            string xmlData = Utils.BytesToString(rezAsset.Data);
            SceneObjectGroup group = (SceneObjectGroup)SceneObjectSerializer.FromOriginalXmlFormat(xmlData);

            group.ResetIDs();
            group.SetGroup(groupID, null);

            // SceneObjectPart rootPart = group.GetPart(group.UUID);
            group.RootPart.Name = name;
            group.RootPart.Description = description;

            foreach (SceneObjectPart part in group.Parts)
            {
                part.LastOwnerID = ownerID;
                part.OwnerID = ownerID;
                part.Inventory.ChangeInventoryOwner(ownerID);
            }
            
            return group;
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        private bool SearchPredicate(SceneObjectGroup sog, Vector3 min, Vector3 max, Regex pattern, UUID ownerID)
        {
            if (! Util.IsInsideBox(sog.AbsolutePosition, min, max))
                return false;

            if (pattern != null && ! pattern.IsMatch(sog.Name))
                return false;
            
            if (ownerID != UUID.Zero && ownerID != sog.OwnerID)
                return false;
            
            return true;
        }
    }
#endregion
}
