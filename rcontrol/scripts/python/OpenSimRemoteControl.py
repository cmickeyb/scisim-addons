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

import urllib2
import uuid
import json
import md5

class OpenSimRemoteControl() :

    # -----------------------------------------------------------------
    def __init__(self, endpoint, request = 'sync'):
        self.EndPoint = endpoint
        self.RequestType = request

        self.Capability = uuid.UUID(int=0)
        self.Scene = ''
        self.DomainList = ['Dispatcher', 'RemoteControl', 'RemoteSensor']

    # -----------------------------------------------------------------
    def _PostDebug(self, domain, operation, parms):
        oparms = parms;

        oparms['$type'] = operation
        oparms['_domain'] = domain
        oparms['_asyncrequest'] = self.RequestType

        if self.Capability and self.Capability.int != 0 :
            oparms['_capability'] = str(self.Capability)
        if self.Scene :
            oparms['_scene'] = self.Scene

        print json.dumps(oparms,sort_keys=True)

    # -----------------------------------------------------------------
    def _PostRequest(self, domain, operation, parms):
        oparms = parms;

        oparms['$type'] = operation
        oparms['_domain'] = domain
        oparms['_asyncrequest'] = (self.RequestType == 'async')

        if self.Capability and self.Capability.int != 0 :
            oparms['_capability'] = str(self.Capability)
        if self.Scene :
            oparms['_scene'] = self.Scene

        data = json.dumps(oparms,sort_keys=True)
        datalen = len(data)
        headers = { 'Content-Type' : 'application/json', 'Content-Length' : datalen }
        request = urllib2.Request(self.EndPoint,data,headers)

        # print json.dumps(oparms,sort_keys=True)

        try:
            response = urllib2.urlopen(request)
        except urllib2.HTTPError as e:
            print e.code
            print e.read()
            return json.loads('{"_Success" : 0, "_Message" : "connection failed"}');

        try:
            data = response.read()
            result = json.loads(data)
        except TypeError :
            print 'failed to parse response: ' + data
            return json.loads('{"_Success" : 0, "_Message" : "failed to parse response"}');

        # print json.dumps(result,sort_keys=True,indent=4)
        return result

    # -----------------------------------------------------------------
    def AuthenticateAvatarByUUID(self, uuid, passwd, lifespan = 3600) :
        m = md5.new()
        m.update(passwd)

        parms = dict()
        parms['hashedpasswd'] = '$1$' + m.hexdigest()
        parms['userid'] = str(uuid)
        parms['lifespan'] = lifespan
        parms['domainlist'] = self.DomainList
        
        return self._PostRequest('Dispatcher','Dispatcher.Messages.CreateCapabilityRequest',parms)

    # -----------------------------------------------------------------
    def AuthenticateAvatarByName(self, name, passwd, lifespan = 3600) :
        m = md5.new()
        m.update(passwd)

        names = name.split(' ',2)

        parms = dict()
        parms['hashedpasswd'] = '$1$' + m.hexdigest()
        parms['firstname'] = names[0]
        parms['lastname'] = names[1]
        parms['lifespan'] = lifespan
        parms['domainlist'] = self.DomainList
        
        return self._PostRequest('Dispatcher','Dispatcher.Messages.CreateCapabilityRequest',parms)

    # -----------------------------------------------------------------
    def AuthenticateAvatarByEmail(self, email, passwd, lifespan = 3600) :
        m = md5.new()
        m.update(passwd)

        parms = dict()
        parms['hashedpasswd'] = '$1$' + m.hexdigest()
        parms['emailaddress'] = email
        parms['lifespan'] = lifespan
        parms['domainlist'] = self.DomainList
        
        return self._PostRequest('Dispatcher','Dispatcher.Messages.CreateCapabilityRequest',parms)


    # -----------------------------------------------------------------
    def RenewCapability(self, lifespan) :
        parms = dict()
        parms['lifespan'] = lifespan
        parms['domainlist'] = self.DomainList

        return self._PostRequest('Dispatcher','Dispatcher.Messages.RenewCapabilityRequest',parms)

    # -----------------------------------------------------------------
    def Info(self) :
        return self._PostRequest('Dispatcher','Dispatcher.Messages.InfoRequest',{})

    # -----------------------------------------------------------------
    def MessageFormatRequest(self, message) :
        parms = dict()
        parms['MessageName'] = message
        
        return self._PostRequest('Dispatcher','Dispatcher.Messages.MessageFormatRequest',parms)

    # -----------------------------------------------------------------
    # NAME: SendChatMessage
    # -----------------------------------------------------------------
    def SendChatMessage(self, msg, pos) :
        parms = dict()
        parms['Message'] = msg
        parms['Position'] = pos

        return self._PostRequest('RemoteControl','RemoteControl.Messages.ChatRequest',parms);

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

