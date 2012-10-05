
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

integer sortch = -5234;
integer userch = 5;
string name = "QuickSort";
integer objectcount = 8000;
integer samplesize = 20;
integer shortsize = 30;

// string name = "Selection Sort";
// integer objectcount = 500;
// integer samplesize = 1;
// integer shortsize = 501;

// string name = "QuickSort";
// integer objectcount = 2000;
// integer samplesize = 15;
// integer shortsize = 30;

SayFormat(integer ch, string msg, list params)
{
    string str = osFormatString(msg,params);
    llSay(ch,str);
}

SendConfiguration()
{
    QuickSortConfig("object-count",objectcount);
    QuickSortConfig("sample-size",samplesize);

    QuickSortConfig("short-size",shortsize);

    vector size = llGetScale() - <0.5, 0.5, 0.5>;
    QuickSortConfig("range",size);

    vector pos = llGetPos() - size / 2.0;
    QuickSortConfig("position",pos);
}

default
{
    state_entry()
    {
        string title = osFormatString("{0}\nCount: {1}\nSample Size: {2}\nShort Size: {3}",
                                      [name, objectcount, samplesize, shortsize]);

        llSay(0, "Script running");
        llSetText(title,<1,1,1>,1);

        SendConfiguration();
    }

    touch_start(integer i)
    {
        SendConfiguration();
    }
}
