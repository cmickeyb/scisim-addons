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
 
import sys, os
sys.path.append(os.path.join(os.environ.get("OPENSIM","/share/opensim"),"lib","python"))
sys.path.append(os.path.realpath(os.path.join(os.path.dirname(__file__), "..")))
sys.path.append(os.path.realpath(os.path.join(os.path.dirname(__file__), "..", "lib")))
 
import argparse
import time
import getpass
import json
import uuid

import OpenSimRemoteControl
 
# -----------------------------------------------------------------
# -----------------------------------------------------------------
def DumpEndPointInfo(rc) :
    response = rc.Info()
    if not response['_Success'] :
        print 'Failed: ' + response['_Message']
        sys.exit(-1)

    endpoint = response['SynchEndPoint']
    udppoint = response['AsyncEndPoint']

    print 'export OS_REMOTECONTROL_URL=' + endpoint
    print 'export OS_REMOTECONTROL_UDP=' + udppoint

# -----------------------------------------------------------------
# -----------------------------------------------------------------
def AuthByUserName(rc, name, passwd, domains, lifespan) :
    rc.DomainList = domains
    response = rc.AuthenticateAvatarByName(name,passwd,lifespan)
    if not response['_Success'] :
        print 'Failed: ' + response['_Message']
        sys.exit(-1)

    capability = response['Capability']
    expires = response['LifeSpan'] + int(time.time())
    print 'export OS_REMOTECONTROL_CAP=' + capability + ':' + str(expires)

    DumpEndPointInfo(rc)

    sys.exit(0)

# -----------------------------------------------------------------
# -----------------------------------------------------------------
def AuthByUserEmail(rc, email, passwd, domains, lifespan) :
    rc.DomainList = domains
    response = rc.AuthenticateAvatarByEmail(email,passwd,lifespan)
    if not response['_Success'] :
        print 'Failed: ' + response['_Message']
        sys.exit(-1)

    capability = response['Capability']
    expires = response['LifeSpan'] + int(time.time())
    print 'export OS_REMOTECONTROL_CAP=' + capability + ':' + str(expires)

    DumpEndPointInfo(rc)

    sys.exit(0)

# -----------------------------------------------------------------
# -----------------------------------------------------------------
def AuthByUserUUID(rc, userid, passwd, domains, lifespan) :
    rc.DomainList = domains
    response = rc.AuthenticateAvatarByUUID(userid,passwd,lifespan)
    if not response['_Success'] :
        print 'Failed: ' + response['_Message']
        sys.exit(-1)

    capability = response['Capability']
    expires = response['LifeSpan'] + int(time.time())
    print 'export OS_REMOTECONTROL_CAP=' + capability + ':' + str(expires)

    DumpEndPointInfo(rc)

    sys.exit(0)

# -----------------------------------------------------------------
# -----------------------------------------------------------------
def RenewCapability(rc, capability, domains, lifespan) :
    if not capability :
        print 'no valid capability found'
        sys.exit(-1)
    
    rc.DomainList = domains
    rc.Capability = uuid.UUID(capability)
    response = rc.RenewCapability(lifespan)
    if not response['_Success'] :
        print 'Failed: ' + response['_Message']
        sys.exit(-1)

    # capability = response['Capability']
    expires = response['LifeSpan'] + int(time.time())
    print 'export OS_REMOTECONTROL_CAP=' + capability + ':' + str(expires)

    DumpEndPointInfo(rc)

    sys.exit(0)
    

# -----------------------------------------------------------------
# -----------------------------------------------------------------
def GetCapabilityFromEnvironment() :
    capenv = os.environ.get('OS_REMOTECONTROL_CAP')
    if capenv :
        [capstr, expires] = capenv.split(':',1)
        if expires and int(expires) > int(time.time()) :
            return capstr

    return ''

# -----------------------------------------------------------------
# -----------------------------------------------------------------
def main() :

    capability = GetCapabilityFromEnvironment()

    endpoint = os.environ.get('OS_REMOTECONTROL_URL')
    eprequired = not endpoint

    parser = argparse.ArgumentParser()
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument('--av_email', help='avatar email address')
    group.add_argument('--av_name', help='avatars full name')
    group.add_argument('--av_userid', help='avatars userid')
    group.add_argument('--capability', help='existing capability for renewal',action='store_true')

    parser.add_argument('--passwd', help='avatars password')
    parser.add_argument('--endpoint', help='URL of the simulator dispatcher', required=eprequired, default=endpoint)
    parser.add_argument('--lifespan', help='Lifespan of the capability in seconds', default=3600, type=int)
    parser.add_argument('--domain', help='Domains associated with the capability', nargs='+', default=['Dispatcher', 'RemoteControl'])

    # parser.add_option('-c', '--config', dest = 'config', help = 'config file', metavar = 'CONFIG')
    args = parser.parse_args()

    rc = OpenSimRemoteControl.OpenSimRemoteControl(args.endpoint)

    if args.capability :
        RenewCapability(rc,capability,args.domain,args.lifespan)

    passwd = args.passwd
    if not passwd :
        passwd = getpass.getpass()

    if args.av_name :
        AuthByUserName(rc,args.av_name,passwd,args.domain,args.lifespan)

# -----------------------------------------------------------------
# -----------------------------------------------------------------
# -----------------------------------------------------------------
# -----------------------------------------------------------------
if __name__ == '__main__':
    main()
