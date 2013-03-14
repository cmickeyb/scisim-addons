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

use File::Basename;
use File::Path qw/make_path/;

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
my $gInventoryRoot;
my $gAssetRoot;
my $gSceneName;
my $gEndPointURL;

my $gOptions = {
    'root=s'		=> \$gInventoryRoot,
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
    $gInventoryRoot = $ENV{'OS_INVENTORY_ROOT'} unless defined $gInventoryRoot;
    $gInventoryRoot = $ENV{'HOME'} . '/xfer' unless defined $gInventoryRoot;
    $gAssetRoot = $gInventoryRoot . "/.assets";

    die "unable to find inventory root directly; $gInventoryRoot\n" unless -d $gInventoryRoot;
    make_path($gAssetRoot);     # make sure the asset directory exists

    $gRemoteControl = RemoteControlStream->new(URL => $gEndPointURL, SCENE => $gSceneName);
    $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;
}

# -----------------------------------------------------------------
# NAME: SlurpFile
# DESC: Read an entire file into a string
# -----------------------------------------------------------------
sub SlurpFile
{
    my $file = shift;

    local $/ = undef;
    open(FILE,"<$file") || die "unable to open $file for reading; $!\n";
    my $string = <FILE>;
    close(FILE);

    return $string;
}

# -----------------------------------------------------------------
# NAME: MakeAssetPath
# -----------------------------------------------------------------
sub MakeAssetPath
{
    my $uuid = shift(@_);

    my $p1 = substr($uuid,0,3);
    my $p2 = substr($uuid,3,3);

    make_path("$gAssetRoot/$p1/$p2");
    return "$gAssetRoot/$p1/$p2/$uuid";
}

# -----------------------------------------------------------------
# NAME: RequireRemoteAsset
# DESC: Make sure there is a copy of the asset on the server
# -----------------------------------------------------------------
sub RequireRemoteAsset
{
    my $assetid = shift(@_);
    my $file = &MakeAssetPath($assetid);

    croak "there is no local copy of the asset $assetid\n" unless -e $file;
    
    my $result = $gRemoteControl->TestAsset($assetid);
    die "TestAsset returned an error; " . $result->{_Message} . "\n"
        if $result->{_Success} <= 0;

    return if $result->{Exists};
    
    my $asset = decode_json(SlurpFile($file));
    $result = $gRemoteControl->AddAsset($asset);
    die "AddAsset returned an error; " . $result->{_Message} . "\n"
        if $result->{_Success} <= 0;
}

# -----------------------------------------------------------------
# NAME: RequireLocalAsset
# DESC: Make sure there is a local copy of an asset
# -----------------------------------------------------------------
sub RequireLocalAsset
{
    my $assetid = shift(@_);
    my $file = &MakeAssetPath($assetid);

    return if -e $file;

    my $result = $gRemoteControl->GetAsset($assetid);
    die "Unable to retrieve asset $assetid; " . $result->{_Message} . "\n"
        if $result->{_Success} <= 0;

    my $asset = $result->{'Asset'};

    die "unable to open asset file $file; $!\n" unless open($fh,">$file");
    print $fh encode_json($result->{'Asset'});
    close $fh;
}

# -----------------------------------------------------------------
# NAME: SaveAsset
# DESC: Save a local copy of an asset
# -----------------------------------------------------------------
sub SaveAsset
{
    my $asset = shift(@_);
    my $assetid = $asset->{'AssetID'};

    my $file = &MakeAssetPath($assetid);

    return if -e $file;

    die "unable to open asset file $file; $!\n" unless open($fh,">$file");
    print $fh encode_json($asset);
    close $fh;
}

# -----------------------------------------------------------------
# NAME: GetAssetDependencies
# DESC: Get the list of assets that are required 
# -----------------------------------------------------------------
sub GetAssetDependencies
{
    my $asset = shift(@_);
    
    my $result = $gRemoteControl->GetDependentAssets($asset);
    die "Unable to retrieve dependencies for $asset; " . $result->{_Message} . "\n"
        if $result->{_Success} <= 0;

    return @{$result->{'DependentAssets'}};
}

# -----------------------------------------------------------------
# NAME: cGETOBJECT
# DESC: create a local inventory entry from an object
# -----------------------------------------------------------------
sub cGETOBJECT
{
    my $invpath;
    my $object;
    my $description;

    $gOptions->{'o|object=s'} = \$object;
    $gOptions->{'p|path=s'} = \$invpath;
    $gOptions->{'d|description=s'} = \$description;

    if (! GetOptions(%{$gOptions}))
    {
        die "Unknown option\n";
    }

    &Initialize();

    die "Missing required parameter; object\n" unless defined $object;
    die "Missing required parameter; path\n" unless defined $invpath;
    
    # ---------- Get the asset associated with the object ----------
    my $result = $gRemoteControl->GetAssetFromObject($object);
    die "Failed to get asset for the object; " . $result->{_Message} . "\n"
        if $result->{_Success} <= 0;

    my $asset = $result->{'Asset'};
    &SaveAsset($asset);

    # ---------- Pull all of the dependent assets ----------
    my @depends = &GetAssetDependencies($asset->{'AssetID'});
    foreach my $dasset (@depends)
    {
        &RequireLocalAsset($dasset);
    }

    # ---------- Save the inventory file ----------
    my $invitem = {};
    $invitem->{'AssetID'} = $asset->{'AssetID'};
    $invitem->{'AssetType'} = $asset->{'ContentType'};
    $invitem->{'Description'} = $description || $asset->{'Description'};
    $invitem->{'CreatorID'} = $asset->{'CreatorID'};
    $invitem->{'AssetDependencies'} = \@depends;

    my $perms = {};
    $perms->{'NextOwnerMask'} = 0;
    $perms->{'OwnerMask'} = 0;
    $perms->{'EveryoneMask'} = 0;
    $perms->{'BaseMask'} = 0;

    $invitem->{'Permissions'} = $perms;

    open($fh,">$gInventoryRoot/$invpath") || croak "unable to open file $invpath; $!\n";
    print $fh to_json($invitem,{pretty => 1});
    close $fh;
}

# -----------------------------------------------------------------
# NAME: cPUTOBJECT
# DESC: Rez an object from an inventory entry
# -----------------------------------------------------------------
sub cPUTOBJECT
{
    my $invpath;
    my @position;
    my @velocity;
    my @rotation;
    my $startparam;

    $gOptions->{'p|path=s'} = \$invpath;
    $gOptions->{'l|location=f{3}'} = \@position;
    $gOptions->{'v|velocity=f{3}'} = \@velocity;
    $gOptions->{'r|rotation=f{4}'} = \@rotation;
    $gOptions->{'start=s'} = \$startparam;

    if (! GetOptions(%{$gOptions}))
    {
        die "Unknown option\n";
    }

    &Initialize();

    @position = (128.0, 128.0, 30.0) unless @position;
    @rotation = (0.0, 0.0, 0.0, 1.0) unless @rotation;
    @velocity = (0.0, 0.0, 0.0) unless @velocity;

    die "Missing required parameter; path\n" unless defined $invpath;
    die "No such inventory item; $invpath\n" unless -e "$gInventoryRoot/$invpath";

    my $invitem = from_json(&SlurpFile("$gInventoryRoot/$invpath"));
    my $type = $invitem->{'AssetType'};
    die "Item of type $type cannot be created; expecting application/vnd.ll.primitive\n"
        if $type ne 'application/vnd.ll.primitive';

    my $assetid = $invitem->{'AssetID'};
    my $description = $invitem->{'Description'};
    my $name = basename($invpath);

    # ---------- Make sure all the assets exist ----------
    foreach my $dasset (@{$invitem->{'AssetDependencies'}})
    {
        &RequireRemoteAsset($dasset);
    }

    # ---------- And create the new object ----------
    my $result = $gRemoteControl->CreateObject($assetid,\@position,\@rotation,\@velocity,$name,$description,$startparam);
    die "Unable to create the object; " . $result->{_Message} . "\n"
        if $result->{_Success} <= 0;

    print $result->{ObjectID} . "\n";
}

# -----------------------------------------------------------------
# NAME: Main
# DESC: Main controling routine.
# -----------------------------------------------------------------
sub Main {
    my $cmd = ($#ARGV >= 0) ? shift @ARGV : "HELP";

    &cGETOBJECT, exit	if ($cmd =~ m/^get$/i);
    &cPUTOBJECT, exit	if ($cmd =~ m/^put$/i);

    &Usage();
}

# -----------------------------------------------------------------
&Main;
