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
use lib "/share/opensim/lib";

my $gCommand = $FindBin::Script;

use Carp;
use JSON;

use Getopt::Long;
use Term::ReadKey;

use RemoteControl;
use Helper::CommandInfo;

my @gDomainList = ();

my $gAvatarEmail;
my $gAvatarName;
my $gAvatarPass;
my $gSynchEP;
my $gAsyncEP;
my $gSceneName;
my $gRemoteControl;
my $gUseAsync = 0;

my $gOptions = {
    'async!'		=> \$gUseAsync,
    'domain=s'		=> \@gDomainList,
    'email=s'		=> \$gAvatarEmail,
    'avname=s'		=> \$gAvatarName,
    'pass=s' 		=> \$gAvatarPass,
    'synchep=s'		=> \$gSynchEP,
    'asyncep=s'		=> \$gAsyncEP,
    's|scene=s'		=> \$gSceneName
};

my $gCmdinfo = Helper::CommandInfo->new(USAGE => "USAGE: $gCommand <command> <options>");

$gCmdinfo->AddCommand('globals','options common to all commands');
$gCmdinfo->AddCommandParams('globals','--email',' <string>','email address, must be unique');
$gCmdinfo->AddCommandParams('globals','--avname',' <string>','avatars full name');
$gCmdinfo->AddCommandParams('globals','--pass',' <string>','avatars password');
$gCmdinfo->AddCommandParams('globals','--synchep',' <string>','URL for HTTP simulator functions');
$gCmdinfo->AddCommandParams('globals','--asynchep',' <string>','Endpoint for UDP simulator functions');
$gCmdinfo->AddCommandParams('globals','-s|--scene',' <string>','name of the scene');

# -----------------------------------------------------------------
# NAME: SlurpFile
# DESC: Read an entire file into a string
# -----------------------------------------------------------------
sub SlurpFile
{
    my $file = shift;

    local $/ = undef;
    open(FILE,"<$file") || die "unable to open file $file; $!\n";
    my $string = <FILE>;
    close(FILE);

    return $string;
}

# -----------------------------------------------------------------
# NAME: GetAvatarPassword
# DESC: Read a password from the terminal
# -----------------------------------------------------------------
sub GetAvatarPassword
{
    my $prompt = shift;

    return $gAvatarPass if defined $gAvatarPass;

    print STDERR "Password for $prompt: ";
    ReadMode('noecho');
    $pass = ReadLine(0);
    ReadMode('restore');
    print STDERR "\n";
    chomp($pass);

    return $pass;
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

        print STDERR "saved capability has expired\n";
    }

    my $result;
    if (defined $gAvatarName)
    {
        $gAvatarPass = &GetAvatarPassword($gAvatarName);
        $result = $gRemoteControl->AuthenticateAvatarByName($gAvatarName,$gAvatarPass,$gLifeSpan);
    }
    elsif (defined $gAvatarEmail)
    {

        $gAvatarPass = &GetAvatarPassword($gAvatarEmail);
        $result = $gRemoteControl->AuthenticateAvatarByEmail($gAvatarEmail,$gAvatarPass,$gLifeSpan);
    }
    else
    {
        die "No method for authentication provided\n";
    }

    die "Authentication failed; " . $result->{_Message} if $result->{_Success} <= 0;
    return $result->{_Capability};
}

# -----------------------------------------------------------------
# NAME: CheckGlobals
# DESC: Check to make sure all of the required globals are set
# -----------------------------------------------------------------
sub CheckGlobals
{
    my ($cmd) = shift;
    
    $gSynchEP = $ENV{'OS_REMOTECONTROL_URL'} unless defined $gSynchEP;
    $gAsyncEP = $ENV{'OS_REMOTECONTROL_UDP'} unless defined $gAsyncEP;
    $gSceneName = $ENV{'OS_REMOTECONTROL_SCENE'} unless defined $gSceneName;

    $gRemoteControl = RemoteControlStream->new(URL => $gSynchEP, SCENE => $gSceneName, REQUESTTYPE => ($gUseAsync ? 'async' : 'sync'));
    $gRemoteControl->{DOMAINLIST} = \@gDomainList if @gDomainList;
}
    
# -----------------------------------------------------------------
# -----------------------------------------------------------------
$gCmdinfo->AddCommand('auth','create a capability to be used later');
$gCmdinfo->AddCommandParams('auth','-l|--lifespan',' <integer>','duration in seconds');

sub cAUTHENTICATE
{
    my $gLifeSpan = 300;
    $gOptions->{'l|lifespan=i'} = \$gLifeSpan;

    if (! GetOptions(%{$gOptions}))
    {
	$gCmdinfo->DumpCommands('auth',"Unknown option");
    }
    
    &CheckGlobals("auth");

    my $result;
    if (defined $gAvatarName)
    {
        $gAvatarPass = &GetAvatarPassword($gAvatarName);
        $result = $gRemoteControl->AuthenticateAvatarByName($gAvatarName,$gAvatarPass,$gLifeSpan);
    }
    elsif (defined $gAvatarEmail)
    {
        $gAvatarPass = &GetAvatarPassword($gAvatarEmail);
        $result = $gRemoteControl->AuthenticateAvatarByEmail($gAvatarEmail,$gAvatarPass,$gLifeSpan);
    }
    else
    {
        die "No method for authentication provided\n";
    }

    die "Authentication failed; " . $result->{_Message} if $result->{_Success} <= 0;

    my $capability = $result->{Capability};
    my $expires = time + $result->{LifeSpan};

    print "export OS_REMOTECONTROL_CAP=$capability:$expires\n";

    $result = $gRemoteControl->Info();
    if (defined $result)
    {
        my $aendpoint = $result->{AsyncEndPoint};
        my $sendpoint = $result->{SynchEndPoint};

        print "export OS_REMOTECONTROL_URL=$sendpoint\n";
        print "export OS_REMOTECONTROL_UDP=$aendpoint\n";
    }
}

# -----------------------------------------------------------------
# -----------------------------------------------------------------
$gCmdinfo->AddCommand('renew','renew a capability to be used later');
$gCmdinfo->AddCommandParams('renew','-l|--lifespan',' <integer>','duration in seconds');

sub cRENEWAUTH
{
    my $gLifeSpan = 300;
    $gOptions->{'l|lifespan=i'} = \$gLifeSpan;

    if (! GetOptions(%{$gOptions}))
    {
	$gCmdinfo->DumpCommands('renew',"Unknown option");
    }
    
    &CheckGlobals("renew");
    $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;

    my $result = $gRemoteControl->RenewCapability($gLifeSpan);
    if ($result->{_Success} <= 0)
    {
        print STDERR "Operation failed; " . $result->{_Message} . "\n";
    }
}

# -----------------------------------------------------------------
# -----------------------------------------------------------------
$gCmdinfo->AddCommand('info','get binding information from the dispatcher');

sub cINFO
{
    if (! GetOptions(%{$gOptions}))
    {
	$gCmdinfo->DumpCommands('info',"Unknown option");
    }

    &CheckGlobals("info");

    ## this is a pre auth message, don't need authentication
    ## $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;

    my $result = $gRemoteControl->Info();
    if ($result->{_Success} <= 0)
    {
        print STDERR "Operation failed; " . $result->{_Message} . "\n";
    }

    print "AsyncEndpoint=" . $result->{'AsyncEndPoint'} . "\n";
    print "SynchEndpoint=" . $result->{'SynchEndPoint'} . "\n";

    print "Scenes\n";
    foreach my $scene (@{$result->{'SceneList'}})
    {
        print "\t$scene\n";
    }

    print "Messages\n";
    foreach my $msg (@{$result->{'MessageList'}})
    {
        print "\t$msg\n";
    }
    
}

# -----------------------------------------------------------------
# -----------------------------------------------------------------
$gCmdinfo->AddCommand('message','get an example of a message from the server');
$gCmdinfo->AddCommandParams('message','-m|--message',' <string>','message type without domain or scene');
sub cMESSAGEFORMAT
{
    my $gMessage;
    $gOptions->{'m|message=s'} = \$gMessage;

    if (! GetOptions(%{$gOptions}))
    {
	$gCmdinfo->DumpCommands('message',"Unknown option");
    }

    $gCmdinfo->DumpCommands('message','Missing message type') unless defined $gMessage;
    &CheckGlobals("message");

    ## this is a pre auth message, don't need authentication
    ## $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;

    my $result = $gRemoteControl->MessageFormatRequest($gMessage);
    if ($result->{_Success} <= 0)
    {
        print STDERR "Operation failed; " . $result->{_Message} . "\n";
    }

    print to_json(decode_json($result->{'SampleMessage'}),{pretty => 1});
}

# -----------------------------------------------------------------
# -----------------------------------------------------------------
$gCmdinfo->AddCommand('chat','post a chat message in world');
$gCmdinfo->AddCommandParams('chat','-m|--message', ' <string>','message to send');
$gCmdinfo->AddCommandParams('chat','-l|--location', ' x y z','position from which to say the message');

sub cREMOTECHAT
{
    my $gMessage;
    my @gPosition;

    $gOptions->{'m|message=s'} = \$gMessage;
    $gOptions->{'l|location=f{3}'} = \@gPosition;

    if (! GetOptions(%{$gOptions}))
    {
	$gCmdinfo->DumpCommands('chat',"Unknown option");
    }

    @gPosition = (128.0, 128.0, 30.0) unless @gPosition;
    $gCmdinfo->DumpCommands('chat','missing required parameter; message') unless defined $gMessage;

    &CheckGlobals("chat");
    $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;

    my $result = $gRemoteControl->SendChatMessage($gMessage, \@gPosition);
    if ($result->{_Success} <= 0)
    {
        print STDERR "Operation failed; " . $result->{_Message} . "\n";
    }
}

# -----------------------------------------------------------------
# NAME: cFINDOBJECTS
# DESC: 
# -----------------------------------------------------------------
$gCmdinfo->AddCommand('find','find objects in the scene that match a query');

sub cFINDOBJECTS
{
    my @gCoordA = ();
    my @gCoordB = ();
    my $gOwner;
    my $gPattern;

    $gOptions->{'min=f{3}'} = \@gCoordA;
    $gOptions->{'max=f{3}'} = \@gCoordB;
    $gOptions->{'pattern=s'} = \$gPattern;
    $gOptions->{'owner=s'} = \$gOwner;
    
    if (! GetOptions(%{$gOptions}))
    {
	$gCmdinfo->DumpCommands('find',"Unknown option");
    }

    &CheckGlobals('find');
    $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;
    
    my $result = $gRemoteControl->FindObjects(\@gCoordA,\@gCoordB,$gPattern,$gOwner);
    if ($result->{_Success} <= 0)
    {
        print STDERR "Operation failed; " . $result->{_Message} . "\n";
    }

    foreach my $obj (@{$result->{Objects}})
    {
        my $details = $gRemoteControl->GetObjectData($obj);
        if ($details->{_Success} > 0)
        {
            printf("%s\t%s\t%s\t<%03.2f,%03.2f,%03.2f>\n",$details->{Name},$obj,$details->{OwnerID},@{$details->{Position}});
        }
    }
}

# -----------------------------------------------------------------
# NAME: cCREATEFROMASSET
# DESC: Command to create an object from an asset
# -----------------------------------------------------------------
$gCmdinfo->AddCommand('create','create an object from an asset and place it in the world');
$gCmdinfo->AddCommandParams('create','-a|--assetid', ' <uuid>','identifier for the asset to rez');
$gCmdinfo->AddCommandParams('create','-d|--description', ' <string>','description of the new object');
$gCmdinfo->AddCommandParams('create','-o|--objectname', ' <string>','name given to the new object');
$gCmdinfo->AddCommandParams('create','-l|--location', ' <vector>','position where the object will be placed');
$gCmdinfo->AddCommandParams('create','-v|--velocity', ' <vector>','initial velocity for the object');
$gCmdinfo->AddCommandParams('create','-r|--rotation', ' <quaternion>','initial rotation of the object');
$gCmdinfo->AddCommandParams('create','--start', ' <integer>','value provided to the scripts for startup');

sub cCREATEFROMASSET
{
    my $gName = "";
    my $gDescription = "";
    my $gAssetID;
    my @gPosition;
    my @gVelocity;
    my @gRotation;
    my $gStartParam = 0;

    $gOptions->{'a|assetid=s'} = \$gAssetID;
    $gOptions->{'d|description=s'} = \$gDescription;
    $gOptions->{'o|objectname=s'} = \$gName;
    $gOptions->{'l|location=f{3}'} = \@gPosition;
    $gOptions->{'v|velocity=f{3}'} = \@gVelocity;
    $gOptions->{'r|rotation=f{4}'} = \@gRotation;
    $gOptions->{'start=i'} = \$gStartParam;

    if (! GetOptions(%{$gOptions}))
    {
	$gCmdinfo->DumpCommands('create',"Unknown option");
    }

    $gCmdinfo->DumpCommands('create','missing required parameters; assetid') unless defined $gAssetID;

    &CheckGlobals("create");
    $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;
    @gPosition = (128.0, 128.0, 30.0) unless @gPosition;
    @gRotation = (0.0, 0.0, 0.0, 1.0) unless @gRotation;
    @gVelocity = (0.0, 0.0, 0.0) unless @gVelocity;

    my $result = $gRemoteControl->CreateObject($gAssetID,\@gPosition,\@gRotation,\@gVelocity,$gName,$gDescription,$gStartParam);
    if ($result->{_Success} <= 0)
    {
        print STDERR "Operation failed; " . $result->{_Message} . "\n";
    }
    else
    {
        print $result->{ObjectID} . "\n";
    }
}

# -----------------------------------------------------------------
# NAME: cDELETEOBJECT
# DESC: Command to set the position of an object
# -----------------------------------------------------------------
$gCmdinfo->AddCommand('delete','remove an object from the scene');
$gCmdinfo->AddCommandParams('remove','-o|--objectid', ' <uuid>','identifier for the object');

sub cDELETEOBJECT
{
    my $gObjectID;

    $gOptions->{'o|objectid=s'} = \$gObjectID;
    
    if (! GetOptions(%{$gOptions}))
    {
	$gCmdinfo->DumpCommands('remove',"Unknown option");
    }

    $gCmdinfo->DumpCommands('remove','missing required parameters; objectid') unless defined $gObjectID;

    &CheckGlobals('remove');
    $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;
    
    my $result = $gRemoteControl->DeleteObject($gObjectID);
    if ($result->{_Success} <= 0)
    {
        print STDERR "Operation failed; " . $result->{_Message} . "\n";
    }
}

# -----------------------------------------------------------------
# NAME: cDUMPOBJECT
# DESC: Command to dump the prims inside an object
# -----------------------------------------------------------------
$gCmdinfo->AddCommand('dump','dump the prims that make up an object');
$gCmdinfo->AddCommandParams('dump','-o|--objectid', ' <uuid>','identifier for the object');

sub cDUMPOBJECT
{
    my $gObjectID;

    $gOptions->{'o|objectid=s'} = \$gObjectID;
    
    if (! GetOptions(%{$gOptions}))
    {
	$gCmdinfo->DumpCommands('dump',"Unknown option");
    }

    $gCmdinfo->DumpCommands('dump','missing required parameters; objectid') unless defined $gObjectID;

    &CheckGlobals('dump');

    $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;
    
    my $result = $gRemoteControl->GetObjectParts($gObjectID);
    if ($result->{_Success} <= 0)
    {
        print STDERR "Operation failed; " . $result->{_Message} . "\n";
    }

    print STDERR to_json($result,{pretty => 1}) . "\n";

    $result = $gRemoteControl->GetObjectInventory($gObjectID);
    if ($result->{_Success} <= 0)
    {
        print STDERR "Operation failed; " . $result->{_Message} . "\n";
    }

    print STDERR to_json($result,{pretty => 1}) . "\n";
}

# -----------------------------------------------------------------
# NAME: cSETOBJECTPOSITION
# DESC: Command to set the position of an object
# -----------------------------------------------------------------
$gCmdinfo->AddCommand('setpos','set the position of an object');
$gCmdinfo->AddCommandParams('setpos','-o|--objectid', ' <uuid>','identifier for the object');
$gCmdinfo->AddCommandParams('setpos','-l|--location', ' <vector>','position where the object will be placed');

sub cSETOBJECTPOSITION
{
    my $gObjectID;
    my @gPosition;

    $gOptions->{'o|objectid=s'} = \$gObjectID;
    $gOptions->{'l|location=f{3}'} = \@gPosition;
    
    if (! GetOptions(%{$gOptions}))
    {
	$gCmdinfo->DumpCommands('setpos',"Unknown option");
    }

    $gCmdinfo->DumpCommands('setpos','missing required parameters; objectid') unless defined $gObjectID;

    &CheckGlobals('setpos');
    $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;
    @gPosition = (128.0, 128.0, 30.0) unless @gPosition;
    
    my $result = $gRemoteControl->SetObjectPosition($gObjectID,\@gPosition);
    if ($result->{_Success} <= 0)
    {
        print STDERR "Operation failed; " . $result->{_Message} . "\n";
    }
}

# -----------------------------------------------------------------
# NAME: cGETOBJECTPOSITION
# DESC: Command to get the position of an object
# -----------------------------------------------------------------
$gCmdinfo->AddCommand('getpos','get the position of an object');
$gCmdinfo->AddCommandParams('getpos','-o|--objectid', ' <uuid>','identifier for the object');

sub cGETOBJECTPOSITION
{
    my $gObjectID;

    $gOptions->{'o|objectid=s'} = \$gObjectID;
    
    if (! GetOptions(%{$gOptions}))
    {
	$gCmdinfo->DumpCommands('getpos',"Unknown option");
    }

    $gCmdinfo->DumpCommands('getpos','missing required parameters; objectid') unless defined $gObjectID;

    &CheckGlobals('getpos');
    $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;
    
    my $result = $gRemoteControl->GetObjectPosition($gObjectID);
    if ($result->{_Success} <= 0)
    {
        print STDERR "Operation failed; " . $result->{_Message} . "\n";
    }

    print "<" . join(',',@{$result->{Position}}) . ">\n";
}

# -----------------------------------------------------------------
# NAME: cMESSAGEOBJECT
# DESC: Command to send a message to a specific object. The message
# will appear as a dataserver event
# -----------------------------------------------------------------
$gCmdinfo->AddCommand('messageobj','send a message to an object');
$gCmdinfo->AddCommandParams('messageobj','-o|--objectid', ' <uuid>','identifier for the object');
$gCmdinfo->AddCommandParams('messageobj','-m|--message', ' <string>','the message to send to the object');

sub cMESSAGEOBJECT
{
    my $gObjectID;
    my @gMessage;

    $gOptions->{'o|objectid=s'} = \$gObjectID;
    $gOptions->{'m|message=s'} = \$gMessage;
    
    if (! GetOptions(%{$gOptions}))
    {
	$gCmdinfo->DumpCommands('setpos',"Unknown option");
    }

    $gCmdinfo->DumpCommands('messageobj','missing required parameters; objectid') unless defined $gObjectID;
    $gCmdinfo->DumpCommands('messageobj','missing required parameters; message') unless defined $gMessage;

    &CheckGlobals('messageobj');
    $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;
    
    my $result = $gRemoteControl->MessageObject($gObjectID,$gMessage);
    if ($result->{_Success} <= 0)
    {
        print STDERR "Operation failed; " . $result->{_Message} . "\n";
    }
}

# -----------------------------------------------------------------
# NAME: cSETPROP
# -----------------------------------------------------------------
sub cSETPROP
{
    my $gObjectID;
    my $gProperty = "position";
    my @gVector = ();
    my $gFloat = 0.0;
    my @gRotation = ();
    my $gInteger = 0;

    $gOptions->{'o|objectid=s'} = \$gObjectID;
    $gOptions->{'property=s'} = \$gProperty;
    $gOptions->{'vector=f{3}'} = \@gVector;
    $gOptions->{'float=f'} = \$gFloat;
    $gOptions->{'rotation=f{4}'} = \@gRotation;
    $gOptions->{'integer=i'} = \$gInteger;

    if (! GetOptions(%{$gOptions}))
    {
	$gCmdinfo->DumpCommands('test',"Unknown option");
    }

    $gCmdinfo->DumpCommands('test','missing required parameters; objectid') unless defined $gObjectID;

    &CheckGlobals('test');
    $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;
    
    my $result;

    $result = $gRemoteControl->SetPartPosition($gObjectID,$gInteger,\@gVector) if ($gProperty =~ m/^position$/i);
    $result = $gRemoteControl->SetPartRotation($gObjectID,$gInteger,\@gRotation) if ($gProperty =~ m/^rotation$/i);
    $result = $gRemoteControl->SetPartScale($gObjectID,$gInteger,\@gVector) if ($gProperty =~ m/^scale$/i);
    $result = $gRemoteControl->SetPartColor($gObjectID,$gInteger,\@gVector,$gFloat) if ($gProperty =~ m/^color$/i);

    if ($result->{_Success} <= 0)
    {
        print STDERR "Operation failed; " . $result->{_Message} . "\n";
    }
}

# -----------------------------------------------------------------
# NAME: cGETAPPEARANCE
# DESC: Command to get the avatar's appearance
# -----------------------------------------------------------------
$gCmdinfo->AddCommand('getappearance','get appearance of an avatar in the scene');

sub cGETAPPEARANCE
{
    my $gAvatarID;

    $gOptions->{'a|avatar=s'} = \$gAvatarID;
    
    if (! GetOptions(%{$gOptions}))
    {
	$gCmdinfo->DumpCommands('getappearance',"Unknown option");
    }

    &CheckGlobals('getappearance');
    $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;
    
    my $result = $gRemoteControl->GetAvatarAppearance($gAvatarID);
    if ($result->{_Success} <= 0)
    {
        print STDERR "Operation failed; " . $result->{_Message} . "\n";
    }

    print $result->{SerializedAppearance} . "\n";
}

# -----------------------------------------------------------------
# NAME: cSETAPPEARANCE
# DESC: Command to get the avatar's appearance
# -----------------------------------------------------------------
$gCmdinfo->AddCommand('setappearance','set appearance of an avatar in the scene');

sub cSETAPPEARANCE
{
    my $gAvatarID;
    my $gAppearance;

    $gOptions->{'a|avatar=s'} = \$gAvatarID;
    $gOptions->{'appearance=s'} = \$gAppearance;

    if (! GetOptions(%{$gOptions}))
    {
	$gCmdinfo->DumpCommands('getappearance',"Unknown option");
    }

    $gCmdinfo->DumpCommands('getappearance','missing required parameters; appearance') unless defined $gAppearance;
    my $gAppString = SlurpFile($gAppearance);

    &CheckGlobals('getappearance');
    $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;
    
    my $result = $gRemoteControl->SetAvatarAppearance($gAppString,$gAvatarID);
    if ($result->{_Success} <= 0)
    {
        print STDERR "Operation failed; " . $result->{_Message} . "\n";
    }
}


# -----------------------------------------------------------------
# NAME: Main
# -----------------------------------------------------------------
sub Main
{
    my $paramCmd = ($#ARGV >= 0) ? shift @ARGV : "HELP";
    
    &cAUTHENTICATE, exit	if ($paramCmd =~ m/^auth$/i);
    &cRENEWAUTH, exit		if ($paramCmd =~ m/^renew$/i);
    &cINFO, exit		if ($paramCmd =~ m/^info$/i);
    &cMESSAGEFORMAT, exit	if ($paramCmd =~ m/^message$/i);
    
    &cREMOTECHAT, exit		if ($paramCmd =~ m/^chat$/i);

    &cFINDOBJECTS, exit		if ($paramCmd =~ m/^find$/i);
    &cCREATEFROMASSET, exit	if ($paramCmd =~ m/^create$/i);
    &cDELETEOBJECT, exit	if ($paramCmd =~ m/^delete$/i);
    &cDUMPOBJECT, exit		if ($paramCmd =~ m/^dump$/i);
    &cSETOBJECTPOSITION, exit	if ($paramCmd =~ m/^setpos$/i);
    &cGETOBJECTPOSITION, exit	if ($paramCmd =~ m/^getpos$/i);
    &cMESSAGEOBJECT, exit	if ($paramCmd =~ m/^msgobj$/i);

    &cSETPROP, exit		if ($paramCmd =~ m/^setprop$/i);

    &cGETAPPEARANCE, exit	if ($paramCmd =~ m/^getappearance$/i);
    &cSETAPPEARANCE, exit	if ($paramCmd =~ m/^setappearance$/i);
    
    &cHELP;
}

&Main;


