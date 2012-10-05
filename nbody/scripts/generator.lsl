
// -----------------------------------------------------------------
// Copyright (c) 2012 Intel Corporation
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//
//     * Redistributions in binary form must reproduce the above
//       copyright notice, this list of conditions and the following
//       disclaimer in the documentation and/or other materials provided
//       with the distribution.
//
//     * Neither the name of the Intel Corporation nor the names of its
//       contributors may be used to endorse or promote products derived
//       from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE INTEL OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

// EXPORT LAWS: THIS LICENSE ADDS NO RESTRICTIONS TO THE EXPORT LAWS OF
// YOUR JURISDICTION. It is licensee's responsibility to comply with any
// export regulations applicable in licensee's jurisdiction. Under
// CURRENT (May 2000) U.S. export regulations this software is eligible
// for export from the U.S. and can be downloaded by or otherwise
// exported or reexported worldwide EXCEPT to U.S. embargoed destinations
// which include Cuba, Iraq, Libya, North Korea, Iran, Syria, Sudan,
// Afghanistan and any other country to which the U.S. has embargoed
// goods and services.
// -----------------------------------------------------------------

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// Library Functions
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
//{ library

// -----------------------------------------------------------------
// Name: DebugInfo
// Desc: Display debug info as appropriate, 0 -- debug, 1 -- info, 
// -----------------------------------------------------------------
//{ DebugInfo
integer DB_DEBUG = 0;
integer DB_INFO = 1;
integer DB_WARN = 2;
integer DB_ERROR = 3;

integer iDebugLevel = 0; // can't use the constants
DebugInfo(integer level, string fmt, list args)
{
  if (level >= iDebugLevel)
    {
      string msg = osFormatString(fmt,args);
      llOwnerSay(msg);
    }
}
//}

//}

// -----------------------------------------------------------------
// GLOBALS
// -----------------------------------------------------------------
//{ Constants

string sStoreName = "NBodyDemo";
string sNotecard = "NBodyConfig";

key kGlobalStore = (key)NULL_KEY;

//}

//{ Globals
key kStoreID;
key kReqID;
key kInitAsset;

list lAttractorDataCollections;
integer iAttractorDataCount;
integer iNextAttractorDataElement;

list lEntityDataCollections;
integer iEntityDataCount;
integer iNextEntityDataElement;

//}

// -----------------------------------------------------------------
// Name: SetupConfig
// Desc: 
// -----------------------------------------------------------------
//{ SetupConfig

// ---------- Variables ----------
key kDomainID;
integer iDimension;
vector vCenterPos;
float fRange;
string sUseMass;
string sUseVelocities;
string sAttractorsMove;
string sEntitiesAttract;
integer iSimulationType;
float fTimeScale;

// ---------- Keys ----------
string sDomainKey = "NBodyDomain";
string sDimensionKey = "Dimension";
string sCenterKey = "Center";
string sRangeKey = "Range";
string sUseMassKey = "UseMass";
string sUseVelocitiesKey = "UseVelocities";
string sAttractorsMoveKey = "AttractorsMove";
string sEntitiesAttractKey = "EntitiesAttract";
string sSimulationTypeKey = "SimulationType";
string sAttractorListKey = "AttractorData[{0}]";
string sAttractorDataKey = "AttractorDataCollection[{0}]";
string sAttractorDataCountKey = "AttractorDataCollectionCount";
string sEntityListKey = "EntityData[{0}]";
string sEntityDataKey = "EntityDataCollection[{0}]";
string sEntityDataCountKey = "EntityDataCollectionCount";
string sTimeScaleKey = "TimeScale";

SetupConfig()
{
    iDimension = (integer)JsonGetValue(kStoreID,sDimensionKey);
    vCenterPos = (vector)JsonGetValue(kStoreID,sCenterKey);
    fRange = (float)JsonGetValue(kStoreID,sRangeKey);

    sUseMass = (string)JsonGetValue(kStoreID,sUseMassKey);
    sUseVelocities = (string)JsonGetValue(kStoreID,sUseVelocitiesKey);
    sAttractorsMove = (string)JsonGetValue(kStoreID,sAttractorsMoveKey);
    sEntitiesAttract =  (string)JsonGetValue(kStoreID,sEntitiesAttractKey);
    iSimulationType = (integer)JsonGetValue(kStoreID,sSimulationTypeKey);
    fTimeScale = (float)JsonGetValue(kStoreID,sTimeScaleKey);
}
//}

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// STATE: default
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
default
{
    state_entry()
    {
        DebugInfo(DB_DEBUG,"running...",[]);
        llSetText("Touch to Start",<1.0,1.0,1.0>,1.0);
    }

    touch_start(integer i)
    {
        state config;
    }
}

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// STATE: config
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
state config
{
    // ---------------------------------------------
    state_entry()
    {
        DebugInfo(DB_DEBUG,"create the store and start...",[]);
        llSetText("running...",<1.0,1.0,1.0>,1.0);

        kStoreID = JsonCreateStore("{}");
        DebugInfo(DB_DEBUG,"Created NBody store {0}",[kStoreID]);
        
        kInitAsset = llGetInventoryKey(sNotecard);
        DebugInfo(DB_DEBUG,"Json notecard={0}",[kInitAsset]);
        
        kReqID = JsonReadNotecard(kStoreID,"",kInitAsset);
        DebugInfo(DB_DEBUG,"Json read notecard request {0}",[kReqID]);
    }
    
    // ---------------------------------------------
    link_message(integer sender, integer result, string value, key id)
    {
        DebugInfo(DB_DEBUG,"incoming message: {0} with key {1}",[value,id]);
        
        if (sender != -1)
            return;

        SetupConfig();

        DebugInfo(DB_DEBUG,"center={0}, range={1}, dimension={2}",[(string)vCenterPos,fRange,iDimension]);
        
        JsonSetValueJson(kStoreID,"AttractorData","[]");
        JsonSetValueJson(kStoreID,"EntityData","[]");

        state attractor;
    }
}

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// STATE: attractor
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
state attractor
{
    // ---------------------------------------------
    state_entry()
    {
        DebugInfo(DB_DEBUG,"read attractor data...",[]);

        lAttractorDataCollections = [];
        iAttractorDataCount = (integer)JsonGetValue(kStoreID,sAttractorDataCountKey);
        DebugInfo(DB_DEBUG,"{0} attractors to read",[iAttractorDataCount]);

        integer i;
        for (i = 0; i < iAttractorDataCount; i++)
        {
            string adkey = osFormatString(sAttractorDataKey,[i]);
            string adata = (string)JsonGetValue(kStoreID,adkey);
            lAttractorDataCollections += adata;
        }
        
        iNextAttractorDataElement = 0;
        if (iNextAttractorDataElement < iAttractorDataCount)
        {
            kInitAsset = llGetInventoryKey(llList2String(lAttractorDataCollections,iNextAttractorDataElement));
            DebugInfo(DB_DEBUG,"read notecard attractor[{0}]={1}",[iNextAttractorDataElement,kInitAsset]);
            string adkey = osFormatString(sAttractorListKey,["+"]);

            kReqID = JsonReadNotecard(kStoreID,adkey,kInitAsset);
            DebugInfo(DB_DEBUG,"Json read notecard request {0}",[kReqID]);
        }
        else
        {
            state entity;
        }
   }
    
    // ---------------------------------------------
    link_message(integer sender, integer result, string value, key id)
    {
        DebugInfo(DB_DEBUG,"incoming message: {0} with key {1}",[value,id]);
        if (sender != -1)
            return;

        iNextAttractorDataElement++;
        if (iNextAttractorDataElement < iAttractorDataCount)
        {
            kInitAsset = llGetInventoryKey(llList2String(lAttractorDataCollections,iNextAttractorDataElement));
            DebugInfo(DB_DEBUG,"read notecard attractor[{0}]={1}",[iNextAttractorDataElement,kInitAsset]);
            string adkey = osFormatString(sAttractorListKey,["+"]);

            kReqID = JsonReadNotecard(kStoreID,adkey,kInitAsset);
            DebugInfo(DB_DEBUG,"Json read notecard request {0}",[kReqID]);
        }
        else
        {
            state entity;
        }
    }
}

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// STATE: entity
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
state entity
{
    // ---------------------------------------------
    state_entry()
    {
        DebugInfo(DB_DEBUG,"read entity data...",[]);

        lEntityDataCollections = [];
        iEntityDataCount = (integer)JsonGetValue(kStoreID,sEntityDataCountKey);
        integer i;
        for (i = 0; i < iEntityDataCount; i++)
        {
            string edkey = osFormatString(sEntityDataKey,[i]);
            string edata = (string)JsonGetValue(kStoreID,edkey);
            lEntityDataCollections += edata;
        }
        
        iNextEntityDataElement = 0;
        if (iNextEntityDataElement < iEntityDataCount)
        {
            kInitAsset = llGetInventoryKey(llList2String(lEntityDataCollections,iNextEntityDataElement));
            DebugInfo(DB_DEBUG,"read notecard entity[{0}]={1}",[iNextEntityDataElement,kInitAsset]);
            string edkey = osFormatString(sEntityListKey,["+"]);

            kReqID = JsonReadNotecard(kStoreID,edkey,kInitAsset);
            DebugInfo(DB_DEBUG,"Json read notecard request {0}",[kReqID]);
        }
        else
        {
            state running;
        }
   }
    
    // ---------------------------------------------
    link_message(integer sender, integer result, string value, key id)
    {
        DebugInfo(DB_DEBUG,"incoming message: {0} with key {1}",[value,id]);
        if (sender != -1)
            return;

        iNextEntityDataElement++;
        if (iNextEntityDataElement < iEntityDataCount)
        {
            kInitAsset = llGetInventoryKey(llList2String(lEntityDataCollections,iNextEntityDataElement));
            DebugInfo(DB_DEBUG,"read notecard entity[{0}]={1}",[iNextEntityDataElement,kInitAsset]);
            string edkey = osFormatString(sEntityListKey,["+"]);

            kReqID = JsonReadNotecard(kStoreID,edkey,kInitAsset);
            DebugInfo(DB_DEBUG,"Json read notecard request {0}",[kReqID]);
        }
        else
        {
            state running;
        }
    }
}

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// STATE: running
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
state running
{
    // ---------------------------------------------
    state_entry()
    {
        DebugInfo(DB_DEBUG,"running...",[]);
        llSetText("running...",<1.0,1.0,1.0>,1.0);

        // create the domain and save its ID
        kDomainID = NBCreateDomain(vCenterPos,fRange,iDimension,fTimeScale,iSimulationType);
        JsonSetValue(kStoreID,sDomainKey,(string)kDomainID);
        
        // record the store id which will trigger the others to pick up their values
        DebugInfo(DB_DEBUG,"set {0}={1}",[sStoreName,(string)kStoreID]);
        JsonSetValue(kGlobalStore,sStoreName,(string)kStoreID);

        llListen(700,"",NULL_KEY,"");
    }

    // ---------------------------------------------
    listen(integer ch, string name, key id, string msg)
    {
        NBDestroyDomain(kDomainID);
        
        JsonRemoveValue(kGlobalStore,sStoreName);
        JsonDestroyStore(kStoreID);

        llResetScript();
    }
}
