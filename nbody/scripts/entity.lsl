
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

key kGlobalStore = (key)NULL_KEY;
string sStoreName = "NBodyDemo";

string sMassKey = "mass";
string sValueKey = "value";
string sVelocityKey = "velocity";
string sPositionKey = "position";

integer iScaleEntity = 0;

//}

//{ Globals

key kStoreID;
key kReqID;

list lValueList = [];
float fMass;
vector vPosition;
vector vVelocity;

integer iParticleState = 0;

integer iCanAttract = 0;
integer iCanMove = 1;

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
    kDomainID = (string)JsonGetValue(kStoreID,sDomainKey);

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

// -----------------------------------------------------------------
// TrailOn
// -----------------------------------------------------------------
//{ TrailOn
TrailOff()
{       
    llParticleSystem([]);
}

TrailOn()
{                
    vector vColor = llGetColor(0);

    llParticleSystem([  // start of particle settings
           // Texture Parameters:
           PSYS_SRC_TEXTURE, llGetInventoryName(INVENTORY_TEXTURE, 0),
           PSYS_PART_START_SCALE, <0.02, 0.02, FALSE>,
           PSYS_PART_END_SCALE, <2.0, 2.0, FALSE>, 
           PSYS_PART_START_COLOR, <1.0,1.0,1.0>,
           PSYS_PART_END_COLOR, vColor, 
           PSYS_PART_START_ALPHA, (float) 1.0,
           PSYS_PART_END_ALPHA, (float) 0.0,     
           
           // Production Parameters:
           PSYS_SRC_BURST_PART_COUNT, (integer)    1, 
           PSYS_SRC_BURST_RATE,         (float) 0.05,  
           PSYS_PART_MAX_AGE,           (float) 5.0, 
        // PSYS_SRC_MAX_AGE,            (float)  0.00, 
            
           // Placement Parameters:
           PSYS_SRC_PATTERN, (integer) 1, // 1=DROP, 2=EXPLODE, 4=ANGLE, 8=CONE,
           
           // Placement Parameters (for any non-DROP pattern):
        // PSYS_SRC_BURST_SPEED_MIN, (float) 00.1,   PSYS_SRC_BURST_SPEED_MAX, (float) 00.1, 
        // PSYS_SRC_BURST_RADIUS, (float) 00.00,
           
           // Placement Parameters (only for ANGLE & CONE patterns):
        // PSYS_SRC_ANGLE_BEGIN, (float) 0.25 * PI,   PSYS_SRC_ANGLE_END, (float) 0.00 * PI,  
        // PSYS_SRC_OMEGA, <00.00, 00.00, 00.00>,  
           
           // After-Effect & Influence Parameters:
        // PSYS_SRC_ACCEL, < 00.00, 00.00, 00.0>,
        // PSYS_SRC_TARGET_KEY, (key) llGetLinkKey(llGetLinkNum() + 1), 
                   
           PSYS_PART_FLAGS, (integer) ( 0
                             // Texture Options:     
                                | PSYS_PART_INTERP_COLOR_MASK   
                                | PSYS_PART_INTERP_SCALE_MASK   
                                | PSYS_PART_EMISSIVE_MASK   
                             // | PSYS_PART_FOLLOW_VELOCITY_MASK
                             // After-effect & Influence Options:
                             // | PSYS_PART_WIND_MASK            
                             // | PSYS_PART_BOUNCE_MASK          
                             // | PSYS_PART_FOLLOW_SRC_MASK     
                             // | PSYS_PART_TARGET_POS_MASK     
                             // | PSYS_PART_TARGET_LINEAR_MASK    
                            ) 
            //end of particle settings                     
            ]);
}
//}


// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// STATE: default
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
default
{
    // ---------------------------------------------
    state_entry()
    {
        DebugInfo(DB_DEBUG,"running...",[]);

        // This just makes it so that we can edit the versions we've copied
        // rather than have them die
        if (llGetStartParameter() > 0)
        {
            TrailOff();
            kReqID = JsonReadValue(kGlobalStore,sStoreName);
        }
    }
    
    // ---------------------------------------------
    link_message(integer sender, integer result, string msg, key id)
    {
        if (sender != -1)
            return;
            
        if (msg == "")
        {
            DebugInfo(DB_WARN,"something bad happened reading the storeID",[]);
            return;
        }
        
        kStoreID = (key)msg;
        state create;
    }

    // ---------------------------------------------
    on_rez(integer p)
    {
        llResetScript();
    }
}

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// STATE: default
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
state create
{
    // ---------------------------------------------
    state_entry()
    {
        llListen(700,"",NULL_KEY,"");

        SetupConfig();
        DebugInfo(DB_DEBUG,"domain={0}, center={1}, range={2}, dimension={3}",
                  [kDomainID, (string)vCenterPos, (string)fRange, (string)iDimension]);

        if (sEntitiesAttract == "yes")
            iCanAttract = 1;

        lValueList = [];
        
        string edkey = osFormatString(sEntityListKey,[llGetStartParameter() - 1]) + ".EntityList[0]";
        JsonTakeValueJson(kStoreID,edkey);
    }
    
    // ---------------------------------------------
    link_message(integer sender, integer result, string value, key id)
    {
        if (sender != -1)
            return;

        key kAttractorStoreID = JsonCreateStore(value);
        integer i;
        for (i = 0; i < iDimension; i++)
        {
            float v = (float)JsonGetValue(kAttractorStoreID,osFormatString("value[{0}]",[i]));
            lValueList += [ v ];
        }

        vPosition = (vector) JsonGetValue(kAttractorStoreID,"position");
        llSetPos(vPosition);

        fMass = (float)JsonGetValue(kAttractorStoreID,"mass");
        if (sUseMass != "yes")
            fMass = 10.0;

        vVelocity = (vector)JsonGetValue(kAttractorStoreID,"velocity");
        if (sUseVelocities != "yes")
            vVelocity = ZERO_VECTOR;
        
        JsonDestroyStore(kAttractorStoreID);

        if (iScaleEntity > 0)
        {
            if (iDimension == 3)
            {
                vector color;
                color.x = llList2Float(lValueList,0);
                color.y = llList2Float(lValueList,1);
                color.z = llList2Float(lValueList,2);
                llSetColor(color,ALL_SIDES);
            }
        
            float size = llLog10(fMass) + 0.01;
            llSetScale(<size,size,size>);
        }
        
        DebugInfo(DB_DEBUG,"add mass={0}, pos={1} to domain={2}",[fMass,(string)vPosition,(string)kDomainID]);
        NBAddEntity(kDomainID,llGetKey(),lValueList,iCanMove,iCanAttract,fMass,vVelocity);
    }

    // ---------------------------------------------
    listen(integer ch, string name, key id, string msg)
    {
        NBRemoveEntity(kDomainID,llGetKey());
        state destroy;
    }
    
    // ---------------------------------------------
    touch_start(integer i)
    {
        if (iParticleState == 0)
        {
            // turn on the particles
            TrailOn();
            iParticleState = 1;
        }
        else
        {
            // turn off the particles
            TrailOff();
            iParticleState = 0;
        }
    }
}

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// STATE: destroy
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
state destroy
{
    // ---------------------------------------------
    state_entry()
    {
        float delay = llFrand(30.0) + 1.0;
        llSetTimerEvent(delay);
    }
    
    // ---------------------------------------------
    timer()
    {
        llDie();
    }
}
