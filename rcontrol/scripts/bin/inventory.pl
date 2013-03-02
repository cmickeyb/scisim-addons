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

# -----------------------------------------------------------------
# Set up the library locations
# -----------------------------------------------------------------
use FindBin;
use lib "$FindBin::Bin/../lib";
use lib "/share/opensim/lib";

my $gCommand = $FindBin::Script;

use Carp;
use JSON;

use Getopt::Long;
use Time::HiRes;

use RemoteControl;
use Helper::CommandInfo;

# -----------------------------------------------------------------
# Globals
# -----------------------------------------------------------------
my $gRemoteControl;

# -----------------------------------------------------------------
# Configuration variables
# -----------------------------------------------------------------
my $gAssetID;
my $gSceneName;
my $gEndPointURL;


my $gOptions = {
    'a|asset=s'		=> \$gAssetID,
    's|scene=s'		=> \$gSceneName,
    'u|url=s' 		=> \$gEndPointURL,
};

### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
### INITIALIZATION ROUTINES
### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

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
# NAME: Initialize
# -----------------------------------------------------------------
sub Initialize
{
    $gEndPointURL = $ENV{'OS_REMOTECONTROL_URL'} unless defined $gEndPointURL;
    $gSceneName = $ENV{'OS_REMOTECONTROL_SCENE'} unless defined $gSceneName;

    $gRemoteControl = RemoteControlStream->new(URL => $gEndPointURL, SCENE => $gSceneName);
    $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;
}

# -----------------------------------------------------------------
# NAME: TESt
# DESC: Get an asset
# -----------------------------------------------------------------
sub TEST
{

    if (! GetOptions(%{$gOptions}))
    {
        die "Unknown option\n";
    }

    &Initialize();

    die "Missing required parameter; asset\n" unless defined $gAssetID;
    
    my $result = $gRemoteControl->TestAsset($gAssetID);
    if ($result->{_Success} <= 0)
    {
        print STDERR "Operation failed; " . $result->{_Message} . "\n";
    }

    print "$gAssetID " . ($result->{Exists} ? "does" : "does not") . " exist\n";

    exit($result->{Exists});
}

# -----------------------------------------------------------------
# NAME: GET
# DESC: Get an asset
# -----------------------------------------------------------------
sub GET
{
    my $file;

    $gOptions->{'f|file=s'} = \$file;
    if (! GetOptions(%{$gOptions}))
    {
        die "Unknown option\n";
    }

    &Initialize();

    die "Missing required parameter; asset\n" unless defined $gAssetID;
    
    my $result = $gRemoteControl->GetAsset($gAssetID);

    print to_json($result,{pretty => 1});
}

# -----------------------------------------------------------------
# NAME: Main
# DESC: Main controling routine.
# -----------------------------------------------------------------
sub Main {
    my $cmd = ($#ARGV >= 0) ? shift @ARGV : "HELP";

    &TEST, exit   if ($cmd =~ m/^test$/i);
    &GET, exit   if ($cmd =~ m/^get$/i);

    &Usage();
}

# -----------------------------------------------------------------
&Main;
