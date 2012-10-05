
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

integer qsstate = 0;

list qsstates = ["Touch to Create", "Creating", "Touch to Sort", "Sorting", "Touch to Reset", "Reseting"];
integer S_PRECREATE = 0;
integer S_CREATE = 1;
integer S_PRESORT = 2;
integer S_SORT = 3;
integer S_PREDESTROY = 4;
integer S_DESTROY = 5;

SetStateText()
{
    llSetText(llList2String(qsstates,qsstate),<1,1,1>,1);
}

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// STATE: default
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
default
{
    state_entry()
    {
        llSay(0, "Script running");
        state precreate;
    }
}

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// STATE: precreate
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
state precreate
{
    state_entry()
    {
        qsstate = S_PRECREATE;
        SetStateText();
    }

    touch_start(integer i)
    {
        state create;
    }

    changed(integer change)
    {
        if (change & CHANGED_REGION_RESTART)
            llResetScript();
    }
}

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// STATE: create
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
state create
{
    state_entry()
    {
        qsstate = S_CREATE;
        SetStateText();

        QuickSortState("create");
    }
    
    link_message(integer sender, integer num, string msg, key id)
    {
        if (sender != -1)
            return;

        state presort;
    }

    changed(integer change)
    {
        if (change & CHANGED_REGION_RESTART)
            llResetScript();
    }
}

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// STATE: presort
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
state presort
{
    state_entry()
    {
        qsstate = S_PRESORT;
        SetStateText();
    }
    
    touch_start(integer i)
    {
        state sort;
    }

    changed(integer change)
    {
        if (change & CHANGED_REGION_RESTART)
            llResetScript();
    }
}

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// STATE: sort
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
state sort
{
    state_entry()
    {
        qsstate = S_SORT;
        SetStateText();

        QuickSortState("sort");
    }
    
    link_message(integer sender, integer num, string msg, key id)
    {
        if (sender != -1)
            return;

        state predestroy;
    }

    changed(integer change)
    {
        if (change & CHANGED_REGION_RESTART)
            llResetScript();
    }
}

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// STATE: predestroy
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
state predestroy
{
    state_entry()
    {
        qsstate = S_PREDESTROY;
        SetStateText();
    }
    
    touch_start(integer i)
    {
        state destroy;
    }

    changed(integer change)
    {
        if (change & CHANGED_REGION_RESTART)
            llResetScript();
    }
}

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// STATE: destroy
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
state destroy
{
    state_entry()
    {
        qsstate = S_DESTROY;
        SetStateText();

        QuickSortState("destroy");
    }
    
    link_message(integer sender, integer num, string msg, key id)
    {
        if (sender != -1)
            return;

        state precreate;
    }

    changed(integer change)
    {
        if (change & CHANGED_REGION_RESTART)
            llResetScript();
    }

}

