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
    public class AvatarHandlers : BaseHandler
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

#region ControlInterface
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// -----------------------------------------------------------------
        public AvatarHandlers(Scene scene, IDispatcherModule dispatcher, string domain) : base(scene,dispatcher,domain)
        { }

        /// -----------------------------------------------------------------
        /// <summary>
        /// need to remove all references to the scene in the subscription
        /// list to enable full garbage collection of the scene object
        /// </summary>
        /// -----------------------------------------------------------------
        public override void UnregisterHandlers()
        {
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(GetAvatarAppearanceRequest));
            m_dispatcher.UnregisterOperationHandler(m_scene,m_domain,typeof(SetAvatarAppearanceRequest));
        }

        // -----------------------------------------------------------------
        /// <summary>
        /// Called when all modules have been added for a region. This is 
        /// where we hook up events
        /// </summary>
        // -----------------------------------------------------------------
        public override void RegisterHandlers()
        {
            //m_log.WarnFormat("[TerrainHandlers] register methods");

            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(GetAvatarAppearanceRequest),GetAvatarAppearanceHandler);
            m_dispatcher.RegisterOperationHandler(m_scene,m_domain,typeof(SetAvatarAppearanceRequest),SetAvatarAppearanceHandler);
        }
#endregion

#region ScriptInvocationInteface
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public ResponseBase GetAvatarAppearanceHandler(RequestBase irequest)
        {
            if (irequest.GetType() != typeof(GetAvatarAppearanceRequest))
                return OperationFailed("wrong type");

            GetAvatarAppearanceRequest request = (GetAvatarAppearanceRequest)irequest;

            UUID id = request.AvatarID == UUID.Zero ? request._UserAccount.PrincipalID : request.AvatarID;
            ScenePresence sp = m_scene.GetScenePresence(id);
            if (sp == null || sp.IsChildAgent)
                return OperationFailed(String.Format("cannot find user {0}",request._UserAccount.PrincipalID));
            
            OSDMap osd = sp.Appearance.Pack();
            String appearance = OSDParser.SerializeJsonString(osd);
            
            return new AvatarAppearanceResponse(appearance);
        }
        
        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        public ResponseBase SetAvatarAppearanceHandler(RequestBase irequest)
        {
            if (irequest.GetType() != typeof(SetAvatarAppearanceRequest))
                return OperationFailed("wrong type");

            SetAvatarAppearanceRequest request = (SetAvatarAppearanceRequest)irequest;

            // Get the scenepresence for the avatar we are going to update
            UUID id = request.AvatarID == UUID.Zero ? request._UserAccount.PrincipalID : request.AvatarID;
            ScenePresence sp = m_scene.GetScenePresence(id);
            if (sp == null || sp.IsChildAgent)
                return OperationFailed(String.Format("cannot find user {0}",request._UserAccount.PrincipalID));

            // Clean out the current outfit folder, this is to keep v3 viewers from 
            // reloading the old appearance
            CleanCurrentOutfitFolder(sp);

            // Delete existing npc attachments
            m_scene.AttachmentsModule.DeleteAttachmentsFromScene(sp, false);

            // ---------- Update the appearance and save it ----------
            int serial = sp.Appearance.Serial;
            OSDMap osd = (OSDMap)OSDParser.DeserializeJson(request.SerializedAppearance);
            sp.Appearance = new AvatarAppearance(osd);
            sp.Appearance.Serial = serial + 1;
            
            m_scene.AvatarService.SetAppearance(sp.UUID,sp.Appearance);
            m_scene.EventManager.TriggerAvatarAppearanceChanged(sp);
            
            // ---------- Send out the new appearance to everyone ----------

            // Rez needed attachments
            m_scene.AttachmentsModule.RezAttachments(sp);

            // this didn't work, still looking for a way to get the viewer to change its appearance
            //sp.ControllingClient.SendWearables(sp.Appearance.Wearables, sp.Appearance.Serial++);

            // this didn't work either, 
            //AddWearablesToCurrentOutfitFolder(sp);

            sp.SendAvatarDataToAllClients();
            sp.SendAppearanceToAllOtherClients();
            sp.SendAppearanceToClient(sp); // the viewers seem to ignore this packet when it describes their own avatar
            
            return new ResponseBase(ResponseCode.Success,"");
        }

#endregion

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        private void CleanCurrentOutfitFolder(ScenePresence sp)
        {
            IInventoryService iservice = m_scene.InventoryService;
            InventoryFolderBase folder = iservice.GetFolderForType(sp.UUID,AssetType.CurrentOutfitFolder);
            if (folder == null)
            {
                m_log.WarnFormat("[AvatarHandlers] Unable to find current outfit folder for {0}",sp.UUID);
                return;
            }
            
            InventoryCollection contents = iservice.GetFolderContent(sp.UUID,folder.ID);
            if (contents == null)
            {
                m_log.WarnFormat("[AvatarHandlers] Unable to retrieve current outfit contents");
                return;
            }

            List<UUID> items = new List<UUID>();
            foreach (InventoryItemBase item in contents.Items)
                items.Add(item.ID);
            
            // Delete the items from the Current Outfit inventory folder
            iservice.DeleteItems(sp.UUID,items);
            
            IClientCore core = (IClientCore)sp.ControllingClient;
            IClientInventory inv;
            
            if (core.TryGet<IClientInventory>(out inv))
                inv.SendRemoveInventoryItems(items.ToArray());
        }

        /// -----------------------------------------------------------------
        /// <summary>
        /// </summary>
        // -----------------------------------------------------------------
        private void AddWearablesToCurrentOutfitFolder(ScenePresence sp)
        {
            IInventoryService iservice = m_scene.InventoryService;
            InventoryFolderBase folder = iservice.GetFolderForType(sp.UUID,AssetType.CurrentOutfitFolder);
            if (folder == null)
            {
                m_log.WarnFormat("[AvatarHandlers] Unable to find current outfit folder for {0}",sp.UUID);
                return;
            }
            
            List<InventoryItemBase> items = new List<InventoryItemBase>();
            for (int i = 0; i < AvatarWearable.MAX_WEARABLES; i++)
            {
                for (int j = 0; j < sp.Appearance.Wearables[i].Count; j++)
                {
                    // trying to create links.. this could go horribly wrong
                    InventoryItemBase item = new InventoryItemBase(sp.Appearance.Wearables[i][j].ItemID,sp.UUID);

                    // Fill in the rest of the details from the current item
                    item = iservice.GetItem(item);

                    // And morph it into a link, it appears that the asset is really a reference to another
                    // inventory item
                    item.AssetID = item.ID;
                    item.ID = UUID.Random();
                    item.Folder = folder.ID;
                    items.Add(item);

                    iservice.AddItem(item);
                }
            }
            
            // Add the items from the Current Outfit inventory folder
            IClientCore core = (IClientCore)sp.ControllingClient;
            IClientInventory inv;
            
            if (core.TryGet<IClientInventory>(out inv))
                inv.SendBulkUpdateInventory(new InventoryFolderBase[] { folder }, items.ToArray());
        }
    }
}
