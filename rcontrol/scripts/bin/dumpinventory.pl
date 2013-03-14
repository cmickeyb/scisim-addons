#!/usr/bin/perl -w
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
use lib "/share/opensim/lib";

use File::Path qw/make_path/;
use File::Spec;

my $gCommand = $FindBin::Script;

use JSON;
use Digest::MD5 qw(md5_hex);
use Getopt::Long;

use Simian;
use RemoteControl;
use Helper::CommandInfo;

# these describe how the user is identified
my $gIDValue;
my $gIDType;

my $gInventoryRoot;
my $gAssetRoot;
my $gSceneName;
my $gEndPointURL;

my $gAvatarName;
my $gAvatarUUID;
my $gSimianURL;
my $gSimian;
my $gPath = '';

my $gOptions = {
    'i|uuid=s'          => \$gAvatarUUID,
    'n|name=s'		=> \$gAvatarName,
    'p|path=s'		=> \$gPath,
    'root=s'		=> \$gInventoryRoot,
    'simian=s' 		=> \$gSimianURL,
    'dispatch=s'	=> \$gEndPointURL,
    's|scene=s'		=> \$gSceneName
};

my $gCmdinfo = Helper::CommandInfo->new(USAGE => "USAGE: $gCommand <command> <options>");

$gCmdinfo->AddCommand('globals','options common to all commands');
$gCmdinfo->AddCommandParams('globals','-i|--uuid',' <string>','avatars uuid');
$gCmdinfo->AddCommandParams('globals','-n|--name',' <string>','avatars full name');
$gCmdinfo->AddCommandParams('globals','-r|--root',' <string>','directory where inventory will be dumped');
$gCmdinfo->AddCommandParams('globals','-p|--path',' <string>','inventory path to dump');
$gCmdinfo->AddCommandParams('globals','-u|--url',' <string>','URL for simian grid functions');

# -----------------------------------------------------------------
# NAME: InitializeSimian
# DESC: Check to make sure all of the required globals are set
# -----------------------------------------------------------------
sub InitializeSimian
{
    my $cmd = shift(@_);

    $gSimianURL = $ENV{'SIMIAN'} unless defined $gSimianURL;
    if (! defined $gSimianURL)
    {
        $gCmdinfo->DumpCommands(undef,"No Simian URL specified, please set SIMIAN environment variable");
    }

    $gIDValue = $gAvatarName, $gIDType = 'Name' if defined $gAvatarName;
    $gIDValue = $gAvatarUUID, $gIDType = 'UserID' if defined $gAvatarUUID;

    if (! defined $gIDValue)
    {
        $gCmdinfo->DumpCommands(undef,"Avatar name not fully specified");
    }

    $gSimian = Simian->new(URL => $gSimianURL);
}
    
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
sub InitializeDispatcher
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
# NAME: RequireLocalAsset
# DESC: Make sure there is a local copy of an asset
# -----------------------------------------------------------------
sub RequireLocalAsset
{
    my $assetid = shift(@_);
    my $file = &MakeAssetPath($assetid);

    return if -e $file;

    my $result = $gRemoteControl->GetAsset($assetid);
    print STDERR "Unable to retrieve asset $assetid; " . $result->{_Message} . "\n", return
        if $result->{_Success} <= 0;

    my $asset = $result->{'Asset'};

    print STDERR "unable to open asset file $file; $!\n", return unless open($fh,">$file");
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

    print STDERR "unable to open asset file $file; $!\n", return unless open($fh,">$file");
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
    print STDERR "Unable to retrieve dependencies for $asset; " . $result->{_Message} . "\n", return ()
        if $result->{_Success} <= 0;

    return @{$result->{'DependentAssets'}};
}

# -----------------------------------------------------------------
# NAME: FindInventoryNodeByName
# -----------------------------------------------------------------
sub FindInventoryNodeByName
{
    my $uuid = shift(@_);
    my $itemID = shift(@_);
    my $pname = shift(@_);

    my $params = {
        'IncludeFolders' => 1,
        'IncludeItems' => 1,
        'ChildrenOnly' => 1
    };

    my $items = $gSimian->GetInventoryNode($itemID,$uuid,$params);
    foreach my $item (@{$items})
    {
        # return $item->{'ID'} if ($item->{'Name'} eq $pname) && ($item->{'Type'} eq "Folder");
        return $item->{'ID'} if $item->{'Name'} eq $pname;
    }

    die "unable to locate inventory node; $pname";
}

# -----------------------------------------------------------------
# NAME: FindInventoryNodeByPath
# -----------------------------------------------------------------
sub FindInventoryNodeByPath
{
    my $uuid = shift(@_);
    my $path = shift(@_);
    my @pathQ = File::Spec->splitdir($path);

    my $itemID = $uuid;
    while (@pathQ)
    {
        my $pname = shift(@pathQ);
        next if $pname eq "";

        $itemID = &FindInventoryNodeByName($uuid,$itemID,$pname);
    }
    
    return $itemID;
}


# -----------------------------------------------------------------
# NAME:
# -----------------------------------------------------------------
sub CleanName
{
    my $name = shift(@_);
    $name =~ s@/@_@g;

    return $name;
}

# -----------------------------------------------------------------
# NAME:
# -----------------------------------------------------------------
sub CleanPath
{
    my $path = shift(@_);
    $path =~ s/[:;'"-]//g;
    $path =~ s@/\s+@/@;
    $path =~ s@\s+/@/@;
    $path =~ s@\s+$@@;
    $path =~ s@\s+@_@g;

    return $path;
}

# -----------------------------------------------------------------
# NAME:
# -----------------------------------------------------------------
sub ProcessFolder
{
    my $ipath = shift(@_);
    my $path = &CleanPath($gInventoryRoot . $ipath);

    make_path($path) unless -d $path;
}

# -----------------------------------------------------------------
# NAME:
# -----------------------------------------------------------------
sub ProcessItem
{
    my $path = shift(@_);
    my $item = shift(@_);

    # ---------- Grab and save the asset ----------
    my $assetid = $item->{'AssetID'};
    my $result = $gRemoteControl->GetAsset($assetid);
    print STDERR "Failed to get asset for $assetid; " . $result->{_Message} . "\n", return
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
    my $name = &CleanName($item->{'Name'});

    $invitem->{'AssetID'} = $asset->{'AssetID'};
    $invitem->{'AssetType'} = $asset->{'ContentType'};
    $invitem->{'Description'} = $item->{'Description'} || $asset->{'Description'};
    $invitem->{'CreatorID'} = $asset->{'CreatorID'};
    $invitem->{'AssetDependencies'} = \@depends;

    my $perms = {}; 
    my $iperms = $item->{'ExtraData'}->{'Permissions'};
    $perms->{'NextOwnerMask'} = $iperms->{'NextOwnerMask'} || 0;
    $perms->{'OwnerMask'} = $iperms->{'OwnerMask'} || 0;
    $perms->{'EveryoneMask'} = $iperms->{'EveryoneMask'} || 0;
    $perms->{'BaseMask'} = $iperms->{'BaseMask'} || 0;
    
    $invitem->{'Permissions'} = $perms;

    my $file = &CleanPath($gInventoryRoot . $path . '/' . $name);
    open($fh,">$file") || die "unable to open file $file; $!\n";
    print $fh to_json($invitem,{pretty => 1});
    close $fh;
}


# -----------------------------------------------------------------
# NAME: Main
# -----------------------------------------------------------------
sub Main
{
    if (! GetOptions(%{$gOptions}))
    {
        $gCmdinfo->DumpCommands('dumpinventory','Unknown option');
    }

    &InitializeSimian;
    &InitializeDispatcher;

    my $info = $gSimian->GetUser($gIDValue,$gIDType);
    return unless defined $info;

    my $uuid = $info->{"UserID"};
    
    my $params = {
        'IncludeFolders' => 1,
        'IncludeItems' => 1,
        'ChildrenOnly' => 1
    };

    my $path;
    my %folders;

    my $root = &FindInventoryNodeByPath($uuid, $gPath);

    my @itemQ = ( $root );
    while (@itemQ)
    {
        my $itemID = shift(@itemQ);
        my $items = $gSimian->GetInventoryNode($itemID,$uuid,$params);

        $path = $folders{$itemID}{'path'} || '';
        &ProcessFolder($path);

        foreach my $item (sort { $b->{'Name'} cmp $a->{'Name'} } @{$items})
        {
            my $id = $item->{"ID"};
            next if $id eq $itemID;

            if ($item->{"Type"} eq "Folder")
            {
                unshift(@itemQ,$id);
                $folders{$id}{'path'} = $path . '/' . $item->{"Name"};
            }
            else
            {
                &ProcessItem($path,$item)
            }
        }
    }
}

&Main;



