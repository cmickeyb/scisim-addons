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
use Helper::CommandInfo;

# these describe how the user is identified
my $gIDValue;
my $gIDType;

my $gAvatarName;
my $gAvatarUUID;
my $gSimianURL;
my $gSimian;
my $gPath = '';
my $gRoot = '.';

my $gOptions = {
    'i|uuid=s'          => \$gAvatarUUID,
    'n|name=s'		=> \$gAvatarName,
    'p|path=s'		=> \$gPath,
    'r|root=s'		=> \$gRoot,
    'u|url=s' 		=> \$gSimianURL
};

my $gCmdinfo = Helper::CommandInfo->new(USAGE => "USAGE: $gCommand <command> <options>");

$gCmdinfo->AddCommand('globals','options common to all commands');
$gCmdinfo->AddCommandParams('globals','-i|--uuid',' <string>','avatars uuid');
$gCmdinfo->AddCommandParams('globals','-n|--name',' <string>','avatars full name');
$gCmdinfo->AddCommandParams('globals','-r|--root',' <string>','directory where inventory will be dumped');
$gCmdinfo->AddCommandParams('globals','-p|--path',' <string>','inventory path to dump');
$gCmdinfo->AddCommandParams('globals','-u|--url',' <string>','URL for simian grid functions');

# -----------------------------------------------------------------
# NAME: CheckGlobals
# DESC: Check to make sure all of the required globals are set
# -----------------------------------------------------------------
sub CheckGlobals
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
    my $path = &CleanPath($gRoot . $ipath);

    unless (make_path($path))
    {
        print STDERR "failed to create $path\n";
    }
}

# -----------------------------------------------------------------
# NAME:
# -----------------------------------------------------------------
sub ProcessItem
{
    my $path = shift(@_);
    my $item = shift(@_);

    my $name = &CleanName($item->{'Name'});

    my $pkey;
    my $props = {};

    my $perms = $item->{'ExtraData'}->{'Permissions'};
    $props->{'Permissions'} = $perms if defined $perms;

    foreach $pkey (qw/CreatorID OwnerID AssetID Description/)
    {
        $props->{$pkey} = $item->{$pkey};
    }

    my $file = &CleanPath($gRoot . $path . '/' . $name);
    if (open(my $fh, "> $file"))
    { 
        print $fh to_json($props,{pretty => 1});
        close $fh;
    }
    else
    {
        print STDERR "unable to open file $file\n";
    }
}


## XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
## Commands
## XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

# -----------------------------------------------------------------
# NAME: cDUMPINVENTORY
# -----------------------------------------------------------------
sub cDUMPINVENTORY
{
    if (! GetOptions(%{$gOptions}))
    {
        $gCmdinfo->DumpCommands('dumpinventory','Unknown option');
    }

    &CheckGlobals;

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

# -----------------------------------------------------------------
# NAME: Main
# -----------------------------------------------------------------
sub Main
{
    &cDUMPINVENTORY;
}

&Main;



