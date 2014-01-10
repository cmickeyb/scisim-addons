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
import json
import OpenSimRemoteControl
 
# -----------------------------------------------------------------
def DumpSimulatorInfo(rc) :
    response = rc.Info()
    if not response['_Success'] :
        print 'Failed: ' + response['_Message']
        sys.exit(-1)

    print json.dumps(response,sort_keys=True,indent=4)
    sys.exit(0)

# -----------------------------------------------------------------
def DumpMessageInfo(rc, message) :
    for mtype in message :
        print 'Message Type: ' + mtype
        response = rc.MessageFormatRequest(mtype)
        if not response['_Success'] :
            print 'Failed: ' + response['_Message']
            sys.exit(-1)

           # pretty print the sample message
        message = response['SampleMessage']
        pmessage = json.loads(message)
        print json.dumps(pmessage,sort_keys=True,indent=4)
        print 

    sys.exit(0)

# -----------------------------------------------------------------
def main() :
    endpoint = os.environ.get('OS_REMOTECONTROL_URL')
    eprequired = not endpoint

    parser = argparse.ArgumentParser()
    parser.add_argument('--binary', help='Use binary transport encoding', dest='binary', action='store_true')
    parser.add_argument('--no-binary', help='Use binary transport encoding', dest='binary', action='store_false')
    parser.add_argument('--endpoint', help='URL of the simulator dispatcher', required=eprequired, default=endpoint)
    parser.add_argument('command', help='Command to execute', choices=['info', 'message'], default='info')
    parser.add_argument('message', help='Message type to expand', nargs='*')
    parser.set_defaults(binary = False)

    args = parser.parse_args()

    rc = OpenSimRemoteControl.OpenSimRemoteControl(args.endpoint)
    rc.Binary = args.binary

    if args.command == 'info' :
        DumpSimulatorInfo(rc)
    elif args.command == 'message' :
        DumpMessageInfo(rc, args.message)

# -----------------------------------------------------------------
# -----------------------------------------------------------------
if __name__ == '__main__':
    main()
