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

import argparse, time, uuid
import OpenSimRemoteControl
 
# -----------------------------------------------------------------
# -----------------------------------------------------------------
def cmdCHAT(rc, cmdargs) :
    parser = argparse.ArgumentParser()
    parser.add_argument('--message',required=True)
    parser.add_argument('--location',nargs=3,required=True)
    args = parser.parse_args(cmdargs)

    response = rc.SendChatMessage(args.message,args.location)
    if not response['_Success'] :
        print 'Failed: ' + response['_Message']
        sys.exit(-1)

    print 'message sent'

# -----------------------------------------------------------------
# -----------------------------------------------------------------
def cmdDELETE(rc, cmdargs) :
    parser = argparse.ArgumentParser()
    parser.add_argument('--pattern',required=True)
    args = parser.parse_args(cmdargs)

    response = rc.FindObjects(pattern = args.pattern)
    if not response['_Success'] :
        print 'Failed: ' + response['_Message']
        sys.exit(-1)

    count = len(response['Objects'])
    for obj in response['Objects'] :
        # print 'deleting %s' % obj
        response = rc.DeleteObject(obj, async=True)

    print 'Deleted %d objects' % count

# -----------------------------------------------------------------
# -----------------------------------------------------------------
def cmdGETSUN(rc, cmdargs) :

    response = rc.GetSunParameters()
    if not response['_Success'] :
        print 'Failed: ' + response['_Message']
        sys.exit(-1)

    print "YearLength = %s" % response['YearLength']
    print "DayLength = %s" % response['DayLength']
    print "HorizonShift = %s" % response['HorizonShift']
    print "DayTimeSunHourScale = %s" % response['DayTimeSunHourScale']
    print "CurrentTime = %s" % response['CurrentTime']


# -----------------------------------------------------------------
# -----------------------------------------------------------------
def cmdSETSUN(rc, cmdargs) :
    parser = argparse.ArgumentParser()
    parser.add_argument('--time', type=float, default=0.0)
    parser.add_argument('--day', type=float, default=0.0)
    parser.add_argument('--year', type=float, default=0.0)
    args = parser.parse_args(cmdargs)

    response = rc.SetSunParameters(yearlength=args.year, daylength=args.day, currenttime=args.time)
    if not response['_Success'] :
        print 'Failed: ' + response['_Message']
        sys.exit(-1)

    print "YearLength = %s" % response['YearLength']
    print "DayLength = %s" % response['DayLength']
    print "HorizonShift = %s" % response['HorizonShift']
    print "DayTimeSunHourScale = %s" % response['DayTimeSunHourScale']
    print "CurrentTime = %s" % response['CurrentTime']

# -----------------------------------------------------------------
# -----------------------------------------------------------------
def main() :

    capenv = os.environ.get('OS_REMOTECONTROL_CAP')
    if not capenv :
        print 'No capability found in $OS_REMOTECONTROL_CAP'
        sys.exit(-1)

    [capability, expires] = capenv.split(':',1)
    if not expires or int(expires) < int(time.time()) :
        print 'Expired capability found in$OS_REMOTECONTROL_CAP'
        sys.exit(-1)

    # print 'Found capability ' + capability

    scene = os.environ.get('OS_REMOTECONTROL_SCENE')
    srequired = not scene

    endpoint = os.environ.get('OS_REMOTECONTROL_URL')
    erequired = not endpoint

    parser = argparse.ArgumentParser()
    parser.add_argument('--scene', help='name of the scene in which the operation is performed', required=srequired, default=scene)
    parser.add_argument('--endpoint', help='URL of the simulator dispatcher', required=erequired, default=endpoint)
    parser.add_argument('command', help='Command to execute')
    parser.add_argument('cmdargs', help='Command specific arguments', nargs=argparse.REMAINDER)
    args = parser.parse_args()

    rc = OpenSimRemoteControl.OpenSimRemoteControl(args.endpoint)
    rc.Capability = uuid.UUID(capability)
    rc.Scene = args.scene

    if args.command == 'chat' :
        cmdCHAT(rc, args.cmdargs)
    elif args.command == 'delete' : 
        cmdDELETE(rc, args.cmdargs)
    elif args.command == 'getsun' : 
        cmdGETSUN(rc, args.cmdargs)
    elif args.command == 'setsun' : 
        cmdSETSUN(rc, args.cmdargs)
    else :
        print args.command
        print args.cmdargs

# -----------------------------------------------------------------
# -----------------------------------------------------------------

# -----------------------------------------------------------------
# -----------------------------------------------------------------
if __name__ == '__main__':
    main()
