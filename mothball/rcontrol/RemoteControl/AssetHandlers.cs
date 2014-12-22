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
using System.Collections.Generic;

using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;


using OpenSim.Framework;
using OpenSim.Framework.Client;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;

using OpenSim.Region.Framework.Scenes.Serialization;
            
using Dispatcher;
using Dispatcher.Messages;

using RemoteControl.Messages;

namespace RemoteControl.Handlers
{
    public class AssetHandlers : BaseHandler
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        IImprovedAssetCache m_cache = null;
        
#region ControlInterface
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public AssetHandlers(Scene scene, IDispatcherModule dispatcher, string domain) : base(scene,dispatcher,domain)
        {
            IImprovedAssetCache cache = scene.RequestModuleInterface<IImprovedAssetCache>();
            if (cache is ISharedRegionModule)
                m_cache = cache;
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// need to remove all references to the scene in the subscription
        /// list to enable full garbage collection of the scene object
        /// </summary>
        /// -----------------------------------------------------------------
        public override void UnregisterHandlers()
        {
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(TestAssetRequest));
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(GetAssetRequest));
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(GetAssetFromObjectRequest));
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(AddAssetRequest));
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(GetDependentAssetsRequest));
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Called when all modules have been added for a region. This is 
        /// where we hook up events
        /// </summary>
        // -----------------------------------------------------------------
        public override void RegisterHandlers()
        {
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(TestAssetRequest),TestAssetHandler);
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(GetAssetRequest),GetAssetHandler);
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(GetAssetFromObjectRequest),GetAssetFromObjectHandler);
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(AddAssetRequest),AddAssetHandler);
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(GetDependentAssetsRequest),GetDependentAssetsHandler);

            m_dispatcher.RegisterMessageType(typeof(TestAssetResponse));
            m_dispatcher.RegisterMessageType(typeof(GetAssetResponse));
            m_dispatcher.RegisterMessageType(typeof(AddAssetResponse));
            m_dispatcher.RegisterMessageType(typeof(GetDependentAssetsResponse));
        }
#endregion

#region ScriptInvocationInteface
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public ResponseBase TestAssetHandler(RequestBase irequest)
        {
            if (m_cache == null)
                return OperationFailed("No asset cache");

            if (irequest.GetType() != typeof(TestAssetRequest))
                return OperationFailed("wrong type");

            TestAssetRequest request = (TestAssetRequest)irequest;
            AssetBase asset = m_cache.Get(request.AssetID.ToString());
            return new TestAssetResponse(asset != null ? true : false);
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public ResponseBase GetAssetHandler(RequestBase irequest)
        {
            if (m_cache == null)
                return OperationFailed("No asset cache");

            if (irequest.GetType() != typeof(GetAssetRequest))
                return OperationFailed("wrong type");

            GetAssetRequest request = (GetAssetRequest)irequest;
            AssetBase asset = m_scene.AssetService.Get(request.AssetID.ToString());
            // AssetBase asset = m_cache.Get(request.AssetID.ToString());

            if (asset == null)
                return OperationFailed("no such asset");
            
            return new GetAssetResponse(asset);
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        /// TODO: should probably move this to the object messages domain, it is the
        /// only message that requires access to the scene
        public ResponseBase GetAssetFromObjectHandler(RequestBase irequest)
        {
            if (irequest.GetType() != typeof(GetAssetFromObjectRequest))
                return OperationFailed("wrong type");

            GetAssetFromObjectRequest request = (GetAssetFromObjectRequest)irequest;

            SceneObjectGroup sog = m_scene.GetSceneObjectGroup(request.ObjectID);
            if (sog == null)
                return OperationFailed("no such object");

            string itemXml;
            itemXml = SceneObjectSerializer.ToOriginalXmlFormat(sog,false);

            AssetBase asset = new AssetBase();

            asset.FullID = UUID.Random();
            asset.Data = Utils.StringToBytes(itemXml);
            asset.Name = sog.GetPartName(sog.RootPart.LocalId);
            asset.Description = sog.GetPartDescription(sog.RootPart.LocalId);
            asset.Type = (sbyte)AssetType.Object;
            asset.CreatorID = sog.OwnerID.ToString();
            asset.Local = true;
            asset.Temporary = false;

            m_cache.Cache(asset);
            
            return new GetAssetResponse(asset);
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public ResponseBase AddAssetHandler(RequestBase irequest)
        {
            if (m_cache == null)
                return OperationFailed("No asset cache");

            if (irequest.GetType() != typeof(AddAssetRequest))
                return OperationFailed("wrong type");

            AddAssetRequest request = (AddAssetRequest)irequest;

            UUID id = request.Asset.AssetID;
            if ((id == UUID.Zero) || (m_cache.Get(id.ToString()) == null))
            {
                if (id == UUID.Zero)
                    id = UUID.Random();
            
                AssetBase asset = (AssetBase)request;

                asset.Local = true;
                asset.Temporary = false;
                
                m_cache.Cache(asset);
            }
            
            
            return new AddAssetResponse(id);
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public ResponseBase GetDependentAssetsHandler(RequestBase irequest)
        {
            if (irequest.GetType() != typeof(GetDependentAssetsRequest))
                return OperationFailed("wrong type");

            GetDependentAssetsRequest request = (GetDependentAssetsRequest)irequest;
            AssetBase asset = m_cache.Get(request.AssetID.ToString());
            if (asset == null)
                return OperationFailed("no such asset");

            UuidGatherer gatherer = new UuidGatherer(m_scene.AssetService);
            Dictionary<UUID,sbyte> assetids = new Dictionary<UUID,sbyte>();
            //gatherer.GatherAssetUuids(request.AssetID, (AssetType)asset.Type, assetids);
            gatherer.GatherAssetUuids(request.AssetID, (sbyte)asset.Type, assetids);
            
            List<UUID> assets = new List<UUID>(assetids.Keys);
            return new GetDependentAssetsResponse(assets);
        }
        

#endregion
    }
}
