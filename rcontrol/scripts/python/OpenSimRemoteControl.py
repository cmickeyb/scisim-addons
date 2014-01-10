#!/usr/bin/python
# -----------------------------------------------------------------
# Copyright (c) 2013 Intel Corporation
# All rights reserved.

# Redistribution and use in source and binary forms, with or without
# modification, are permitted provided that the following conditions are
# met:

#     * Redistributions of source code must retain the above copyright
#       notice, this list of conditions and the following disclaimer.

#     * Redistributions in binary form must reproduce the above
#       copyright notice, this list of conditions and the following
#       disclaimer in the documentation and/or other materials provided
#       with the distribution.

#     * Neither the name of the Intel Corporation nor the names of its
#       contributors may be used to endorse or promote products derived
#       from this software without specific prior written permission.

# THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
# "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
# LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
# A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE INTEL OR
# CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
# EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
# PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
# PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
# LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
# NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
# SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

# EXPORT LAWS: THIS LICENSE ADDS NO RESTRICTIONS TO THE EXPORT LAWS OF
# YOUR JURISDICTION. It is licensee's responsibility to comply with any
# export regulations applicable in licensee's jurisdiction. Under
# CURRENT (May 2000) U.S. export regulations this software is eligible
# for export from the U.S. and can be downloaded by or otherwise
# exported or reexported worldwide EXCEPT to U.S. embargoed destinations
# which include Cuba, Iraq, Libya, North Korea, Iran, Syria, Sudan,
# Afghanistan and any other country to which the U.S. has embargoed
# goods and services.
# -----------------------------------------------------------------

import sys, os, warnings
import string

import urllib2, socket
import uuid
import json
import md5
from bson import BSON, SON

## XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
## XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
class Parameters(SON) :
    def __init__(self, oscontrol, domain, operation) :
        SON.__init__(self)
        self['$type'] = operation
        self['_domain'] = domain
        self['_asyncrequest'] = (oscontrol.RequestType == 'async')

        if oscontrol.Capability and oscontrol.Capability.int != 0 :
            self['_capability'] = str(oscontrol.Capability)
        if oscontrol.Scene :
            self['_scene'] = oscontrol.Scene

## XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
## XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
class BulkUpdateItem() :

    # -----------------------------------------------------------------
    def __init__(self, objectid, pos = None, vel = None, rot = None, acc = None) :
        self.ObjectID = objectid
        self.Position = pos
        self.Velocity = vel
        self.Rotation = rot
        self.Acceleration = acc

    # -----------------------------------------------------------------
    def ConvertForEncoding(self) :
        info = dict()
        info['ObjectID'] = self.ObjectID
        if self.Position :
            info['Position'] = self.Position
        if self.Velocity :
            info['Velocity'] = self.Velocity
        if self.Rotation :
            info['Rotation'] = self.Rotation
        if self.Acceleration :
            info['Acceleration'] = self.Acceleration
            
        return info
    
## XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
## XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
class OpenSimRemoteControl() :

    # -----------------------------------------------------------------
    def __init__(self, endpoint, request = 'sync', logfile = None):
        self.EndPoint = endpoint
        self.RequestType = request
        self.MessagesSent = 0
        self.BytesSent = 0
        self.LogFile = logfile

        self.Binary = False

        self.Capability = uuid.UUID(int=0)
        self.Scene = ''
        self.DomainList = ['Dispatcher', 'RemoteControl', 'RemoteSensor']

    # -----------------------------------------------------------------
    def _PostDebug(self, oparms):
        print json.dumps(oparms,sort_keys=True)

    # -----------------------------------------------------------------
    def _PostRequest(self, oparms):
        if self.Binary :
            data = BSON.encode(oparms)
            datalen = len(data)
            headers = { 'Content-Type' : 'application/bson', 'Content-Length' : datalen }
        else :
            data = json.dumps(oparms,sort_keys=True)
            datalen = len(data)
            headers = { 'Content-Type' : 'application/json', 'Content-Length' : datalen }
            
        request = urllib2.Request(self.EndPoint,data,headers)

        # print json.dumps(oparms,sort_keys=True)

        try:
            self.MessagesSent += 1
            self.BytesSent += datalen
            if self.LogFile :
                with open(self.LogFile,"a") as fp :
                    fp.write(data)
                    fp.write("\n")

            response = urllib2.urlopen(request)
        except urllib2.HTTPError as e:
            warnings.warn('[OpenSimRemoteControl] invocation failed with status code %d' % (e.code))
            # print e.code
            # print e.read()
            return json.loads('{"_Success" : 0, "_Message" : "connection failed"}');
        except urllib2.URLError as e:
            warnings.warn('[OpenSimRemoteControl] invalid URL; %s' % (e.args))
            return json.loads('{"_Success" : 0, "_Message" : "unknown connection error"}');
        except :
            return json.loads('{"_Success" : 0, "_Message" : "unknown error"}');

        try:
            data = response.read()
            if self.Binary :
                result = BSON(data).decode()
            else :
                result = json.loads(data)
        except TypeError as detail:
            warnings.warn('[OpenSimRemoteControl] failed to parse response; %s' % (detail))
            return json.loads('{"_Success" : 0, "_Message" : "failed to parse response"}');
            

        # print json.dumps(result,sort_keys=True,indent=4)
        return result

    # XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    # XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    # OpenSim Remote Control Functions
    # XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    # XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

    # -----------------------------------------------------------------
    def AuthenticateAvatarByUUID(self, uuid, passwd, lifespan = 3600) :
        m = md5.new()
        m.update(passwd)

        parms = Parameters(self,'Dispatcher','Dispatcher.Messages.CreateCapabilityRequest')
        parms['hashedpasswd'] = '$1$' + m.hexdigest()
        parms['userid'] = str(uuid)
        parms['lifespan'] = lifespan
        parms['domainlist'] = self.DomainList
        
        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    def AuthenticateAvatarByName(self, name, passwd, lifespan = 3600) :
        m = md5.new()
        m.update(passwd)

        names = name.split(' ',2)

        parms = Parameters(self,'Dispatcher','Dispatcher.Messages.CreateCapabilityRequest')
        parms['hashedpasswd'] = '$1$' + m.hexdigest()
        parms['firstname'] = names[0]
        parms['lastname'] = names[1]
        parms['lifespan'] = lifespan
        parms['domainlist'] = self.DomainList
        
        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    def AuthenticateAvatarByEmail(self, email, passwd, lifespan = 3600) :
        m = md5.new()
        m.update(passwd)

        parms = Parameters(self,'Dispatcher','Dispatcher.Messages.CreateCapabilityRequest')
        parms['hashedpasswd'] = '$1$' + m.hexdigest()
        parms['emailaddress'] = email
        parms['lifespan'] = lifespan
        parms['domainlist'] = self.DomainList
        
        return self._PostRequest(parms)


    # -----------------------------------------------------------------
    def RenewCapability(self, lifespan) :
        parms = Parameters(self,'Dispatcher','Dispatcher.Messages.RenewCapabilityRequest')
        parms['lifespan'] = lifespan
        parms['domainlist'] = self.DomainList

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    def Info(self) :
        parms = Parameters(self,'Dispatcher','Dispatcher.Messages.InfoRequest')

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    def MessageFormatRequest(self, message) :
        parms = Parameters(self,'Dispatcher','Dispatcher.Messages.MessageFormatRequest')
        parms['MessageName'] = message
        
        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: CreateEndPoint
    # -----------------------------------------------------------------
    def CreateEndPoint(self, host, port, life) :
        parms = Parameters(self,'Dispatcher','Dispatcher.Messages.CreateEndPointRequest')
        parms['CallbackHost'] = host
        parms['CallbackPort'] = port
        if life :
            parms['LifeSpan'] = life

        return self._PostRequest(parms)
    
    # -----------------------------------------------------------------
    # NAME: RenewEndPoint
    # -----------------------------------------------------------------
    def RenewEndPoint(self, endpointid, life) :
        parms = Parameters(self,'Dispatcher','Dispatcher.Messages.RenewEndPointRequest')
        parms['EndPointID'] = str(endpointid)
        if life :
            parms['LifeSpan'] = life
        
        return self._PostRequest(parms)
    
    # -----------------------------------------------------------------
    # NAME: CloseEndPoint
    # -----------------------------------------------------------------
    def CloseEndPoint(self, endpointid) :
        parms = Parameters(self,'Dispatcher','Dispatcher.Messages.CloseEndPointRequest')
        parms['EndPointID'] = str(endpointid)

        return self._PostRequest(parms)
    
    # -----------------------------------------------------------------
    # NAME: SendChatMessage
    # -----------------------------------------------------------------
    def SendChatMessage(self, msg, pos) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.ChatRequest')
        parms['Message'] = msg
        parms['Position'] = pos

        return self._PostRequest(parms);

    # -----------------------------------------------------------------
    # NAME: GetAvatarAppearance
    # -----------------------------------------------------------------
    def GetAvatarAppearance(self, avatarid) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.GetAvatarAppearanceRequest')
        parms['AvatarID'] = str(avatarid)

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: SetAvatarAppearance
    # -----------------------------------------------------------------
    def SetAvatarAppearance(self, appearance, avatarid) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.SetAvatarAppearanceRequest')

        # Serialized appearance
        parms['SerializedAppearance'] = appearance 
        parms['AvatarID'] = str(avatarid)

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: FindObjects
    # -----------------------------------------------------------------
    def FindObjects(self, coord1 = None, coord2 = None, pattern = None, owner = None) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.FindObjectsRequest')
        if coord1 :
            parms['CoordinateA'] = coord1

        if coord2 :
            parms['CoordinateB'] = coord2

        if pattern :
            parms['Pattern'] = pattern

        if owner :
            parms['OwnerID'] = str(owner)

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: CreateObject
    # -----------------------------------------------------------------
    def CreateObject(self, asset, pos = None, rot = None, vel = None, name = None, desc = None, parm = "{}") :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.CreateObjectRequest')
        parms['AssetID'] = str(asset)

        if name :
            parms['Name'] = name
        if desc :
            parms['Description'] = desc

        parms['Position'] = pos if pos else [128.0, 128.0, 50.0]
        parms['Rotation'] = rot if rot else [0.0, 0.0, 0.0, 1.0]
        parms['Velocity'] = vel if vel else [0.0, 0.0, 0.0]
        parms['StartParameter'] = parm

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: DeleteObject
    # -----------------------------------------------------------------
    def DeleteObject(self, objectid) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.DeleteObjectRequest')
        parms['ObjectID'] = str(objectid)

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: DeleteAllObject
    # -----------------------------------------------------------------
    def DeleteAllObjects(self) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.DeleteAllObjectsRequest')

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: GetObjectParts
    # -----------------------------------------------------------------
    def GetObjectParts(self, objectid) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.GetObjectPartsRequest')
        parms['ObjectID'] = str(objectid)

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: GetObjectData
    # -----------------------------------------------------------------
    def GetObjectData(self, objectid) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.GetObjectDataRequest')
        parms['ObjectID'] = str(objectid)

        return self._PostRequest(parms)


    # -----------------------------------------------------------------
    # NAME: BulkDynamics
    # -----------------------------------------------------------------
    def BulkDynamics(self, updates) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.BulkDynamicsRequest')
        parms['Updates'] = []
        for update in updates :
            parms['Updates'].append(update.ConvertForEncoding())

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: GetObjectPosition
    # -----------------------------------------------------------------
    def GetObjectPosition(self, objectid) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.GetObjectPositionRequest')
        parms['ObjectID'] = str(objectid)

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: SetObjectPosition
    # -----------------------------------------------------------------
    def SetObjectPosition(self, objectid, pos = None) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.SetObjectPositionRequest')
        parms['ObjectID'] = str(objectid)
        parms['Position'] = pos if pos else [128.0, 128.0, 50.0]

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: GetObjectRotation
    # -----------------------------------------------------------------
    def GetObjectRotation(self, objectid) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.GetObjectRotationRequest')
        parms['ObjectID'] = str(objectid)

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: SetObjectRotation
    # -----------------------------------------------------------------
    def SetObjectRotation(self, objectid, rot = None) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.SetObjectRotationRequest')
        parms['ObjectID'] = str(objectid)
        parms['Rotation'] = rot if rot else [0.0, 0.0, 0.0, 1.0]

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: MessageObject
    # -----------------------------------------------------------------
    def MessageObject(self, objectid, msg) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.MessageObjectRequest')
        parms['ObjectID'] = str(objectid)
        parms['Message'] = msg

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: SetPartPosition
    # -----------------------------------------------------------------
    def SetPartPosition(self, objectid, partnum, pos = None) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.SetPartPositionRequest')
        parms['ObjectID'] = str(objectid)
        parms['LinkNum'] = partnum
        parms['Position'] = pos if pos else [0.0, 0.0, 0.0]

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: SetPartRotation
    # -----------------------------------------------------------------
    def SetPartRotation(self, objectid, partnum, rot = None) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.SetPartRotationRequest')
        parms['ObjectID'] = str(objectid)
        parms['LinkNum'] = partnum
        parms['Rotation'] = rot if rot else [0.0, 0.0, 0.0, 1.0]

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: SetPartScale
    # -----------------------------------------------------------------
    def SetPartScale(self, objectid, partnum, scale = None) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.SetPartScaleRequest')
        parms['ObjectID'] = str(objectid)
        parms['LinkNum'] = partnum
        parms['Scale'] = scale if scale else [1.0, 1.0, 1.0]

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: SetPartColor
    # -----------------------------------------------------------------
    def SetPartColor(self, objectid, partnum, color = None, alpha = None) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.SetPartColorRequest')
        parms['ObjectID'] = str(objectid)
        parms['LinkNum'] = part
        parms['Color'] = color if color else [0.0, 0.0, 0.0]
        parms['Alpha'] = alpha if alpha else 0.0

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: RegisterTouchCallback
    # -----------------------------------------------------------------
    def RegisterTouchCallback(self, objectid, endpointid) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.RegisterTouchCallbackRequest')
        parms['ObjectID'] = str(objectid)
        parms['EndPointID'] = str(endpoint)

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: UnregisterTouchCallback
    # -----------------------------------------------------------------
    def UnregisterTouchCallback(self, objectid, requestid) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.UnregisterTouchCallbackRequest')
        parms['ObjectID'] = str(objectid)
        parms['RequestID'] = str(requestid)

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: TestAsset
    # -----------------------------------------------------------------
    def TestAsset(self, assetid) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.TestAssetRequest')
        parms['AssetID'] = str(assetid)

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: GetAsset
    # -----------------------------------------------------------------
    def GetAsset(self, assetid) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.GetAssetRequest')
        parms['AssetID'] = str(assetid)

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: GetAssetFromObject
    # -----------------------------------------------------------------
    def GetAssetFromObject(self, objectid) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.GetAssetFromObjectRequest')
        parms['ObjectID'] = str(objectid)

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: GetDependentAssets
    # -----------------------------------------------------------------
    def GetDependentAssets(self, assetid) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.GetDependentAssetsRequest')
        parms['AssetID'] = assetid

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: AddAsset
    # -----------------------------------------------------------------
    def AddAsset(self, asset) :
        parms = Parameters(self,'RemoteControl','RemoteControl.Messages.AddAsset')
        parms['Asset'] = asset

        return self._PostRequest(parms)

    # -----------------------------------------------------------------
    # NAME: SensorDataRequest
    # -----------------------------------------------------------------
    def SensorDataRequest(self, family, sensorid, values) :
        parms = Parameters(self,'RemoteSensor','RemoteSensor.Messages.SensorDataRequest')
        parms['SensorFamily'] = family
        parms['SensorID'] = str(sensorid)
        parms['SensorData'] = values

        return self._PostRequest(parms)

## XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
## XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
class OpenSimRemoteControlAsync(OpenSimRemoteControl) :

    # -----------------------------------------------------------------
    def __init__(self, endpoint):
        OpenSimRemoteControl.__init__(self, endpoint, 'async')

        (self.Address, self.Port) = string.split(self.EndPoint,':')
        self.Port = int(self.Port)
        # print "Async endpoint set to host %s on port %s" % (self.Address, self.Port)

        self.Socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)


    # -----------------------------------------------------------------
    def _PostRequest(self, oparms):
        oparms['_asyncrequest'] = True

        if self.Binary :
            data = BSON.encode(oparms)
        else :
            data = json.dumps(oparms,sort_keys=True)

        try :
            self.MessagesSent += 1
            self.Socket.sendto(data, (self.Address, self.Port))
        except :
            print 'send failed; ', sys.exc_info()[0]
            return json.loads('{"_Success" : 0, "_Message" : "unknown error"}')

        return json.loads('{"_Success" : 2}')

# -----------------------------------------------------------------
# -----------------------------------------------------------------
# -----------------------------------------------------------------
if __name__ == "__main__":
    rc = OpenSimRemoteControl('http://grid.sciencesim.com/')
    # rc.Capability = uuid.uuid4()
    # rc.Scene = 'scene1'
    # rc.DomainList = ['Dispatcher', 'RemoteControl', 'RemoteSensor']

    # rc.AuthenticateAvatarByName('Mic Bowman','thisismypasswd')
    # rc.AuthenticateAvatarByUUID(uuid.uuid4(),'thisismypasswd')
    rc.Info()

