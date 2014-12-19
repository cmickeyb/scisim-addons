#!/usr/bin/perl -w
# -----------------------------------------------------------------
# Copyright (c) 2012 Intel Corporation
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

=head1 NAME

name

=head1 SYNOPSIS

synopsis

=head1 DESCRIPTION

description

=head2 COMMON OPTIONS

=head2 COMMANDS

=head1 CUSTOMIZATION

customization

=head1 SEE ALSO

see also

=head1 AUTHOR

Mic Bowman, E<lt>mic.bowman@intel.comE<gt>

=cut


use FindBin;
use lib "$FindBin::Bin/../lib";

my $gCommand = $FindBin::Script;

use Carp;
use JSON;

use threads;
use Sys::Hostname;
use IO::Socket::INET;
use IO::Select;

use FileHandle;
use Getopt::Long;
use Term::ReadKey;

use OpenSim::RemoteControl;
use OpenSim::RemoteControl::Stream;

my $gRemoteControl;

my $gAssetID;
my $gBasePosition = ();
my $gSceneName;
my $gEndPointURL;
my $gPort = 18888;
my $gSleepTime = 2;

my $gOptions = {
    'a|asset=s'		=> \$gAssetID,
    'l|location=f{3}'	=> \@gBasePosition,
    's|scene=s'		=> \$gSceneName,
    'u|url=s' 		=> \$gEndPointURL,
    'sleep=i'		=> \$gSleepTime
};

# -----------------------------------------------------------------
# NAME: AuthenticateRequest
# DESC: Return a valid capability
# -----------------------------------------------------------------
sub AuthenticateRequest
{
    # Check for a valid capability already stored in the environment
    if (exists $ENV{'OS_REMOTECONTROL_CAP'})
    {
        my ($cap, $exp) = split(':',$ENV{'OS_REMOTECONTROL_CAP'});
        return $cap if time < $exp;

        die "saved capability has expired; please authenticate\n";
    }

    die "no capability found; please authenticate\n";
}

# -----------------------------------------------------------------
# NAME: CheckGlobals
# DESC: Check to make sure all of the required globals are set
# -----------------------------------------------------------------
sub CheckGlobals
{
    my $cmd = shift(@_);

    if (! defined $gAssetID)
    {
        die "Missing required parameter; no tile asset specified\n";
    }

    unless (@gBasePosition)
    {
        @gBasePosition = (128.0, 128.0, 25.1);
    }

    $gEndPointURL = $ENV{'OS_REMOTECONTROL_URL'} unless defined $gEndPointURL;
    $gSceneName = $ENV{'OS_REMOTECONTROL_SCENE'} unless defined $gSceneName;

    $gRemoteControl = OpenSim::RemoteControl::Stream->new(URL => $gEndPointURL, SCENE => $gSceneName);
    $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;
}
    
# -----------------------------------------------------------------
# NAME: CreateTheObject
# -----------------------------------------------------------------
sub CreateTheObject
{
    my $rot = [0.0, 0.0, 0.0, 1.0];
    my $vel = [0.0, 0.0, 0.0];
    my $name = "Test Object";
    my $desc = $name;
    my $parm = 0;

    my $result = $gRemoteControl->CreateObject($gAssetID,\@gBasePosition,$rot,$vel,$name,$desc,$parm);
    unless (defined $result)
    {
        print STDERR "Object creation failed; no response\n";
        return undef;
    }
    if ($result->{_Success} <= 0)
    {
        print STDERR "Object creation failed; " . $result->{_Message} . "\n";
        return undef;
    }

    print STDERR "Created object " . $result->{ObjectID} . "\n";
    return $result->{ObjectID};
}

# -----------------------------------------------------------------
# NAME: CreateTheEndPoint
# -----------------------------------------------------------------
sub CreateTheEndPoint
{
    my @endpoints = ();

    for (my $i = 0; $i < 3; $i++)
    {
        my $result = $gRemoteControl->CreateEndPoint(hostname,$gPort,5000);
        unless (defined $result)
        {
            print STDERR "Callback registration failed; no response\n";
            return undef;
        }
        if ($result->{_Success} <= 0)
        {
            print STDERR "Callback registration failed; " . $result->{_Message} . "\n";
            return undef;
        }

        print STDERR "Created endpoint " . $result->{EndPointID} . "\n";

        push(@endpoints,$result->{EndPointID})
    }

    return \@endpoints;
}


# -----------------------------------------------------------------
# NAME: RegisterTheCallback
# -----------------------------------------------------------------
sub RegisterTheCallback
{
    my ($object, $endpoints) = @_;

    my @requests = ();

    foreach my $endpoint (@{$endpoints})
    {
        for (my $i = 0; $i < 3; $i++)
        {
            my $result = $gRemoteControl->RegisterTouchCallback($object,$endpoint);
            unless (defined $result)
            {
                print STDERR "Callback registration failed; no response\n";
                return undef;
            }
            if ($result->{_Success} <= 0)
            {
                print STDERR "Callback registration failed; " . $result->{_Message} . "\n";
                return undef;
            }

            print STDERR "Callback registered for object $object using endpoint $endpoint\n";
            push(@requests,$result->{RequestID});
        }
    }

    return \@requests;
}

# -----------------------------------------------------------------
# NAME: RenewHandlerShutdown
# -----------------------------------------------------------------
sub RenewHandlerShutdown
{
    my $endpoints = shift;

    print STDERR "keep alive thread shutdown\n";
    foreach my $endpoint (@{$endpoints})
    {
        $gRemoteControl->CloseEndPoint($endpoint);
    }

    threads->exit();
}

# -----------------------------------------------------------------
# NAME: RenewTheEndPoint
# -----------------------------------------------------------------
sub RenewTheEndPoint
{
    my $endpoints = shift;

    $SIG{'KILL'} = sub { &RenewHandlerShutdown($endpoints); };

    while (1)
    {
        sleep $gSleepTime;

        foreach my $endpoint (@{$endpoints})
        {
            my $result = $gRemoteControl->RenewEndPoint($endpoint,5000);
            unless (defined $result)
            {
                print STDERR "EndPoint renewal failed; no response\n";
            }
            if ($result->{_Success} <= 0)
            {
                print STDERR "EndPoint renewal failed; " . $result->{_Message} . "\n";
            }
            print STDERR "Renewed endpoint " . $endpoint . "\n";
        }
    }
}

# -----------------------------------------------------------------
# NAME: TouchHandlerShutdown
# -----------------------------------------------------------------
sub TouchHandlerShutdown
{
    my ($object, $requests) = @_;

    print STDERR "touch handler thread shutdown\n";
    foreach my $request (@{$requests})
    {
        $gRemoteControl->UnregisterTouchCallback($object,$request);
    }

    threads->exit();
}

# -----------------------------------------------------------------
# NAME: RespondToTouchEvents
# -----------------------------------------------------------------
sub RespondToTouchEvents
{
    my ($object, $requests) = @_;

    $SIG{'KILL'} = sub { &TouchHandlerShutdown($object,$requests); };

    my $socket = new IO::Socket::INET(LocalPort => $gPort, Proto => 'udp') ||
        die "Unable to open listening socket; $!\n";

    my $select =  IO::Select->new($socket);

    while (1)
    {
        print STDERR "wait for touch event\n";
        my @ready = $select->can_read($gSleepTime);
        if (@ready)
        {
            my $data;
            $socket->recv($data,2000);
            $gRemoteControl->SendChatMessage("received: $data",[128.0, 128.0, 20.0]);
        }
    }
}

# -----------------------------------------------------------------
# NAME: Main
# -----------------------------------------------------------------
sub Main
{
    if (! GetOptions(%{$gOptions}))
    {
        die "Unknown option\n";
    }

    &CheckGlobals;

    my $objectID = &CreateTheObject;
    my $endpoints = &CreateTheEndPoint;
    my $requests = &RegisterTheCallback($objectID, $endpoints);

    my $renewthr = threads->create('RenewTheEndPoint',$endpoints);
    my $touchthr = threads->create('RespondToTouchEvents',$objectID,$requests);

    $SIG{'INT'} = sub {
        print STDERR "kill touch handler\n";
        $touchthr->kill('KILL');
        $touchthr->join();

        print STDERR "kill keep alive handler\n";
        $renewthr->kill('KILL');
        $renewthr->join();

        print STDERR "main thread shutdown\n";
        exit;
    };

    while (1) 
    {
        sleep 60;
    }
}

&Main;
