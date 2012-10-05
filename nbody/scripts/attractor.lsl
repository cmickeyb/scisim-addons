
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

integer iScaleAttractor = 0;

//}

//{ Globals

key kStoreID;
key kReqID;

list lValueList = [];
float fMass;
vector vPosition;
vector vVelocity;

integer iParticleState = 0;

integer iCanAttract = 1;
integer iCanMove = 0;

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
    vector vColor = llGetColor(ALL_SIDES);

    llParticleSystem(
        [                   //KPSv1.0  
            PSYS_PART_FLAGS , 0 //Comment out any of the following masks to deactivate them
            | PSYS_PART_TARGET_POS_MASK       //Particles follow the target
            | PSYS_PART_TARGET_LINEAR_MASK
            ,PSYS_SRC_PATTERN,           PSYS_SRC_PATTERN_DROP
            ,PSYS_SRC_TEXTURE,           ""                 //UUID of the desired particle texture, or inventory name
            ,PSYS_SRC_MAX_AGE,           0.0                //Time, in seconds, for particles to be emitted. 0 = forever
            ,PSYS_PART_MAX_AGE,          5.0                //Lifetime, in seconds, that a particle lasts
            ,PSYS_SRC_BURST_RATE,        1.0               //How long, in seconds, between each emission
            ,PSYS_SRC_BURST_PART_COUNT,  1                  //Number of particles per emission
            ,PSYS_SRC_BURST_RADIUS,      0.1                //Radius of emission
            ,PSYS_SRC_BURST_SPEED_MIN,   0.0                //Minimum speed of an emitted particle
            ,PSYS_SRC_BURST_SPEED_MAX,   0.0                //Maximum speed of an emitted particle
            ,PSYS_SRC_ACCEL,             <0.0,0.0,0.0>     //Acceleration of particles each second
            ,PSYS_PART_START_COLOR,      vColor             //Starting RGB color
            ,PSYS_PART_END_COLOR,        vColor             //Ending RGB color, if INTERP_COLOR_MASK is on 
            ,PSYS_PART_START_ALPHA,      1.0                //Starting transparency, 1 is opaque, 0 is transparent.
            ,PSYS_PART_END_ALPHA,        1.0                //Ending transparency
            ,PSYS_PART_START_SCALE,      <2.0,2.0,2.0>      //Starting particle size
            ,PSYS_PART_END_SCALE,        <0.0,0.0,0.0>      //Ending particle size, if INTERP_SCALE_MASK is on
            ,PSYS_SRC_ANGLE_BEGIN,       PI                 //Inner angle for ANGLE patterns
            ,PSYS_SRC_ANGLE_END,         PI                 //Outer angle for ANGLE patterns
            ,PSYS_SRC_OMEGA,             <0.0,0.0,0.0>       //Rotation of ANGLE patterns, similar to llTargetOmega()
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
            
        DebugInfo(DB_DEBUG,"got global store = {0}",[msg]);

        if (msg == "")
        {
            DebugInfo(DB_WARN,"something bad happened reading the storeID",[]);
            return;
        }
        
        kStoreID = (key)msg;
        DebugInfo(DB_DEBUG,"global store is {0}",[kStoreID]);

        state create;
    }

    // ---------------------------------------------
    on_rez(integer p)
    {
        llResetScript();
    }
}

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// STATE: create
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
state create
{
    // ---------------------------------------------
    state_entry()
    {
        llListen(700,"",NULL_KEY,"");

        SetupConfig();

        if (sAttractorsMove == "yes")
            iCanMove = 1;

        lValueList = [];

        string adkey = osFormatString(sAttractorListKey,[llGetStartParameter() - 1]) + ".AttractorList[0]";
        JsonTakeValueJson(kStoreID,adkey);
    }
    
    // ---------------------------------------------
    link_message(integer sender, integer result, string value, key id)
    {
        if (sender != -1)
            return;

        DebugInfo(DB_DEBUG,"value={0}",[value]);
        
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

        DebugInfo(DB_DEBUG,"mass={0}, pos={1}",[fMass,(string)vPosition]);
        
        if (iScaleAttractor > 0)
        {
            if (iDimension == 3)
            {
                vector color;
                color.x = llList2Float(lValueList,0);
                color.y = llList2Float(lValueList,1);
                color.z = llList2Float(lValueList,2);
                llSetColor(color,ALL_SIDES);

                llSetPos(vCenterPos + (color - <0.5,0.5,0.5>) * fRange/3.0);
            }

            float size = llLog10(fMass) + 0.1;
            llSetScale(<size,size,size>);
        }

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
        fMass = 2.0 * fMass;

        float size = llLog10(fMass) + 0.1;
        llSetScale(<size,size,size>);

        NBAddEntity(kDomainID,llGetKey(),lValueList,iCanMove,iCanAttract,fMass,vVelocity);
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
