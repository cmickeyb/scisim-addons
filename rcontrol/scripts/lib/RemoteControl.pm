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

use 5.010001;
use strict;
use warnings;

our $AUTOLOAD;
our $VERSION = sprintf "%s", '$Revision: 1.0 $ ' =~ /\$Revision:\s+([^\s]+)/;

### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
###
### PACKAGE: RemoteControl
###
### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
package RemoteControl;

use Carp;
use JSON;

use Digest::MD5 qw(md5_hex);
use MIME::Base64;

my @gDomainList = qw/Dispatcher RemoteControl RemoteSensor/;

## XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
## Dispatcher Fields
## XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

# -----------------------------------------------------------------
# NAME: AuthenticateAvatarByUUID
# -----------------------------------------------------------------
sub AuthenticateAvatarByUUID
{
    my $self = shift;
    my ($uuid, $pass, $lifespan) = @_;

    my $params = {};
    $params->{'userid'} = $uuid;
    $params->{'hashedpasswd'} = '$1$' . md5_hex($pass);
    $params->{'lifespan'} = $lifespan if defined $lifespan;
    ## $params->{'domainlist'} = \@gDomainList;
    $params->{'domainlist'} = $self->{DOMAINLIST};

    return $self->_PostRequest('Dispatcher','Dispatcher.Messages.AuthRequest',$params);
}

# -----------------------------------------------------------------
# NAME: AuthenticateAvatarByName
# -----------------------------------------------------------------
sub AuthenticateAvatarByName
{
    my $self = shift;
    my ($name, $pass, $lifespan) = @_;
    my ($fname,$lname) = split(' ',$name);

    my $params = {};
    $params->{'firstname'} = $fname;
    $params->{'lastname'} = $lname;
    $params->{'hashedpasswd'} = '$1$' . md5_hex($pass);
    $params->{'lifespan'} = $lifespan if defined $lifespan;
    ## $params->{'domainlist'} = \@gDomainList;
    $params->{'domainlist'} = $self->{DOMAINLIST};

    return $self->_PostRequest('Dispatcher','Dispatcher.Messages.AuthRequest',$params);
}

# -----------------------------------------------------------------
# NAME: AuthenticateAvatarByEmail
# -----------------------------------------------------------------
sub AuthenticateAvatarByEmail
{
    my $self = shift;
    my ($email, $pass, $lifespan) = @_;

    my $params = {};
    $params->{'emailaddress'} = $email;
    $params->{'hashedpasswd'} = '$1$' . md5_hex($pass);
    $params->{'lifespan'} = $lifespan if defined $lifespan;
    ## $params->{'domainlist'} = \@gDomainList;
    $params->{'domainlist'} = $self->{DOMAINLIST};

    return $self->_PostRequest('Dispatcher','Dispatcher.Messages.AuthRequest',$params);
}

# -----------------------------------------------------------------
# NAME: Info
# -----------------------------------------------------------------
sub Info
{
    my $self = shift;
    return $self->_PostRequest('Dispatcher','Dispatcher.Messages.InfoRequest',{});
}

# -----------------------------------------------------------------
# NAME: MessageFormatRequest
# -----------------------------------------------------------------
sub MessageFormatRequest
{
    my $self = shift;
    my ($message) = @_;

    my $params = {};
    $params->{'MessageName'} = $message;
    return $self->_PostRequest('Dispatcher','Dispatcher.Messages.MessageFormatRequest',$params);
}

# -----------------------------------------------------------------
# NAME: CreateEndPoint
# -----------------------------------------------------------------
sub CreateEndPoint
{
    my $self = shift;
    my ($host,$port,$life) = @_;

    my $params = {};
    $params->{'CallbackHost'} = $host;
    $params->{'CallbackPort'} = $port;
    $params->{'LifeSpan'} = $life if defined $life;

    return $self->_PostRequest('Dispatcher','Dispatcher.Messages.CreateEndPointRequest',$params);
}

# -----------------------------------------------------------------
# NAME: RenewEndPoint
# -----------------------------------------------------------------
sub RenewEndPoint
{
    my $self = shift;
    my ($id,$life) = @_;

    my $params = {};
    $params->{'EndPointID'} = $id;
    $params->{'LifeSpan'} = $life if defined $life;

    return $self->_PostRequest('Dispatcher','Dispatcher.Messages.RenewEndPointRequest',$params);
}

# -----------------------------------------------------------------
# NAME: CloseEndPoint
# -----------------------------------------------------------------
sub CloseEndPoint
{
    my $self = shift;
    my ($id) = @_;

    my $params = {};
    $params->{'EndPointID'} = $id;

    return $self->_PostRequest('Dispatcher','Dispatcher.Messages.CloseEndPointRequest',$params);
}

# -----------------------------------------------------------------
# NAME: SendChatMessage
# -----------------------------------------------------------------
sub SendChatMessage
{
    my $self = shift;
    my ($msg,$pos) = @_;

    my $params = {};
    $params->{'Message'} = $msg;
    $params->{'Position'} = defined $pos ? $pos : [128.0, 128.0, 128.0];

    # return $self->_PostRequest('RemoteControl.Messages.ChatRequest',$params);
    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.ChatRequest',$params);
}


# -----------------------------------------------------------------
# NAME: GetAvatarAppearance
# -----------------------------------------------------------------
sub GetAvatarAppearance
{
    my $self = shift;
    my $id = shift;

    my $params = {};
    $params->{'AvatarID'} = $id if defined $id;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.GetAvatarAppearanceRequest',$params);
}

# -----------------------------------------------------------------
# NAME: SetAvatarAppearance
# -----------------------------------------------------------------
sub SetAvatarAppearance
{
    my $self = shift;
    my ($app, $id) = @_;

    my $params = {};
    $params->{'SerializedAppearance'} = $app;
    $params->{'AvatarID'} = $id if defined $id;
    
    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.SetAvatarAppearanceRequest',$params);
}

# -----------------------------------------------------------------
# NAME: FindObjects
# -----------------------------------------------------------------
sub FindObjects
{
    my $self = shift;
    my ($coord1, $coord2, $pattern, $owner) = @_;

    my $params = {};
    $params->{'CoordinateA'} = $coord1 if defined $coord1 && @{$coord1};
    $params->{'CoordinateB'} = $coord2 if defined $coord2 && @{$coord2};
    $params->{'Pattern'} = $pattern if defined $pattern;
    $params->{'OwnerID'} = $owner if defined $owner;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.FindObjectsRequest',$params);
}

# -----------------------------------------------------------------
# NAME: CreateObject
# -----------------------------------------------------------------
sub CreateObject
{
    my $self = shift;
    my ($asset, $pos, $rot, $vel, $name, $desc, $parm) = @_;

    my $params = {};
    $params->{'AssetID'} = $asset;
    $params->{'Name'} = $name if defined $name;
    $params->{'Description'} = $desc if defined $desc;
    $params->{'Position'} = defined $pos ? $pos : [128.0, 128.0, 50.0];
    $params->{'Rotation'} = defined $rot ? $rot : [0.0, 0.0, 0.0, 1.0];
    $params->{'Velocity'} = defined $vel ? $vel : [0.0, 0.0, 0.0];
    $params->{'StartParameter'} = $parm;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.CreateObjectRequest',$params);
}

# -----------------------------------------------------------------
# NAME: DeleteObject
# -----------------------------------------------------------------
sub DeleteObject
{
    my $self = shift;
    my ($object) = @_;

    my $params = {};
    $params->{'ObjectID'} = $object;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.DeleteObjectRequest',$params);
}

# -----------------------------------------------------------------
# NAME: DeleteAllObject
# -----------------------------------------------------------------
sub DeleteAllObjects
{
    my $self = shift;

    my $params = {};
    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.DeleteAllObjectsRequest',$params);
}

# -----------------------------------------------------------------
# NAME: GetObjectParts
# -----------------------------------------------------------------
sub GetObjectParts
{
    my $self = shift;
    my ($object) = @_;

    my $params = {};
    $params->{'ObjectID'} = $object;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.GetObjectPartsRequest',$params);
}

# -----------------------------------------------------------------
# NAME: GetObjectData
# -----------------------------------------------------------------
sub GetObjectData
{
    my $self = shift;
    my ($object) = @_;

    my $params = {};
    $params->{'ObjectID'} = $object;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.GetObjectDataRequest',$params);
}

# -----------------------------------------------------------------
# NAME: GetObjectPosition
# -----------------------------------------------------------------
sub GetObjectPosition
{
    my $self = shift;
    my ($object) = @_;

    my $params = {};
    $params->{'ObjectID'} = $object;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.GetObjectPositionRequest',$params);
}

# -----------------------------------------------------------------
# NAME: SetObjectPosition
# -----------------------------------------------------------------
sub SetObjectPosition
{
    my $self = shift;
    my ($object, $pos) = @_;

    my $params = {};
    $params->{'ObjectID'} = $object;
    $params->{'Position'} = defined $pos ? $pos : [128.0, 128.0, 50.0];

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.SetObjectPositionRequest',$params);
}

# -----------------------------------------------------------------
# NAME: GetObjectRotation
# -----------------------------------------------------------------
sub GetObjectRotation
{
    my $self = shift;
    my ($object) = @_;

    my $params = {};
    $params->{'ObjectID'} = $object;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.GetObjectRotationRequest',$params);
}

# -----------------------------------------------------------------
# NAME: SetObjectRotation
# -----------------------------------------------------------------
sub SetObjectRotation
{
    my $self = shift;
    my ($object, $rot) = @_;

    my $params = {};
    $params->{'ObjectID'} = $object;
    $params->{'Rotation'} = defined $rot ? $rot : [0.0, 0.0, 0.0, 1.0];

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.SetObjectRotationRequest',$params);
}

# -----------------------------------------------------------------
# NAME: MessageObject
# -----------------------------------------------------------------
sub MessageObject
{
    my $self = shift;
    my ($object, $msg) = @_;

    my $params = {};
    $params->{'ObjectID'} = $object;
    $params->{'Message'} = $msg;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.MessageObjectRequest',$params);
}

# -----------------------------------------------------------------
# NAME: SetPartPosition
# -----------------------------------------------------------------
sub SetPartPosition
{
    my $self = shift;
    my ($object, $part, $pos) = @_;

    my $params = {};
    $params->{'ObjectID'} = $object;
    $params->{'LinkNum'} = $part;
    $params->{'Position'} = defined $pos ? $pos : [0.0, 0.0, 0.0];

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.SetPartPositionRequest',$params);
}

# -----------------------------------------------------------------
# NAME: SetPartRotation
# -----------------------------------------------------------------
sub SetPartRotation
{
    my $self = shift;
    my ($object, $part, $rot) = @_;

    my $params = {};
    $params->{'ObjectID'} = $object;
    $params->{'LinkNum'} = $part;
    $params->{'Rotation'} = defined $rot ? $rot : [0.0, 0.0, 0.0, 1.0];

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.SetPartRotationRequest',$params);
}

# -----------------------------------------------------------------
# NAME: SetPartScale
# -----------------------------------------------------------------
sub SetPartScale
{
    my $self = shift;
    my ($object, $part, $scale) = @_;

    my $params = {};
    $params->{'ObjectID'} = $object;
    $params->{'LinkNum'} = $part;
    $params->{'Scale'} = defined $scale ? $scale : [1.0, 1.0, 1.0];

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.SetPartScaleRequest',$params);
}

# -----------------------------------------------------------------
# NAME: SetPartColor
# -----------------------------------------------------------------
sub SetPartColor
{
    my $self = shift;
    my ($object, $part, $color, $alpha) = @_;

    my $params = {};
    $params->{'ObjectID'} = $object;
    $params->{'LinkNum'} = $part;
    $params->{'Color'} = defined $color ? $color : [0.0, 0.0, 0.0];
    $params->{'Alpha'} = defined $alpha ? $alpha : 0.0;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.SetPartColorRequest',$params);
}

# -----------------------------------------------------------------
# NAME: RegisterTouchCallback
# -----------------------------------------------------------------
sub RegisterTouchCallback
{
    my $self = shift;
    my ($object, $endpoint) = @_;

    my $params = {};
    $params->{'ObjectID'} = $object;
    $params->{'EndPointID'} = $endpoint;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.RegisterTouchCallbackRequest',$params);
}

# -----------------------------------------------------------------
# NAME: UnregisterTouchCallback
# -----------------------------------------------------------------
sub UnregisterTouchCallback
{
    my $self = shift;
    my ($object, $request) = @_;

    my $params = {};
    $params->{'ObjectID'} = $object;
    $params->{'RequestID'} = $request;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.UnregisterTouchCallbackRequest',$params);
}



# -----------------------------------------------------------------
# NAME: TestAsset
# -----------------------------------------------------------------
sub TestAsset
{
    my $self = shift;
    my ($asset) = @_;

    my $params = {};
    $params->{'AssetID'} = $asset;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.TestAssetRequest',$params);
}

# -----------------------------------------------------------------
# NAME: GetAsset
# -----------------------------------------------------------------
sub GetAsset
{
    my $self = shift;
    my ($asset) = @_;

    my $params = {};
    $params->{'AssetID'} = $asset;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.GetAssetRequest',$params);
}

# -----------------------------------------------------------------
# NAME: GetAssetFromObject
# -----------------------------------------------------------------
sub GetAssetFromObject
{
    my $self = shift;
    my ($id) = @_;

    my $params = {};
    $params->{'ObjectID'} = $id;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.GetAssetFromObjectRequest',$params);
}

# -----------------------------------------------------------------
# NAME: GetDependentAssets
# -----------------------------------------------------------------
sub GetDependentAssets
{
    my $self = shift;
    my ($asset) = @_;

    my $params = {};
    $params->{'AssetID'} = $asset;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.GetDependentAssetsRequest',$params);
}

# -----------------------------------------------------------------
# NAME: AddAsset
# -----------------------------------------------------------------
sub AddAsset
{
    my $self = shift;
    my ($asset) = @_;

    my $params = {};
    $params->{'Asset'} = $asset;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.AddAsset',$params);
}

# -----------------------------------------------------------------
# NAME: SensorDataRequest
# -----------------------------------------------------------------
sub SensorDataRequest
{
    my $self = shift;
    my ($family, $sensor, $values) = @_;

    my $params = {};
    $params->{'SensorFamily'} = $family;
    $params->{'SensorID'} = $sensor;
    $params->{'SensorData'} = $values;

    return $self->_PostRequest('RemoteSensor','RemoteSensor.Messages.SensorDataRequest',$params);
}

# -----------------------------------------------------------------
# NAME: new
# DESC: Constructor for the object, attributes listed in gAutoFields
# can be initialized here.
# -----------------------------------------------------------------
sub new
{
    my $proto = shift;
    my $parms = ($#_ == 0) ? { %{ (shift) } } : { @_ };

    my $class = ref($proto) || $proto;
    my $self = { };

    bless $self, $class;

    # Copy the parameters into the object
    my %gAutoFields = ( REQUESTTYPE => 'sync', CAPABILITY => undef, SCENE => undef, DOMAINLIST => \@gDomainList );
    $self->{_permitted} = \%gAutoFields;

    # Set the initial values for all the parameters
    foreach my $key (keys %{$self->{_permitted}}) {
        $self->{$key} = $parms->{$key} || $self->{_permitted}->{$key};
    }

    return $self;
}

### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
###
### PACKAGE: RemoteControlStream
###
### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
package RemoteControlStream;

use Carp;
use JSON;

use LWP::UserAgent;
use LWP::ConnCache;
require HTTP::Request;

use base 'RemoteControl';

# -----------------------------------------------------------------
# NAME: new
# -----------------------------------------------------------------
sub new
{
    my $proto = shift;
    my $parms = ($#_ == 0) ? { %{ (shift) } } : { @_ };

    my $class = ref($proto) || $proto;
    my $self = RemoteControl->new($parms);

    bless $self, $class;

    # Copy the parameters into the object
    my %gAutoFields = ( URL => undef );
    $self->{_permitted} = \%gAutoFields;

    # Set the initial values for all the parameters
    foreach my $key (keys %{$self->{_permitted}}) {
        $self->{$key} = $parms->{$key} || $self->{_permitted}->{$key};
    }

    $self->{_ua} = LWP::UserAgent->new;
    $self->{_ua}->agent("OpenSimRemoteControl/0.1 ");
    $self->{_ua}->timeout(30);
    $self->{_ua}->conn_cache(LWP::ConnCache->new()); # opensim seems to barf if this isn't here

    return $self;
}

# -----------------------------------------------------------------
# NAME: _PostRequest
# -----------------------------------------------------------------
sub _PostRequest()
{
    my $self = shift;
    my $domain = shift;
    my $operation = shift;
    my ($params) = @_;

    croak 'Synchronous enpoint undefined' unless defined $self->{URL};

    # Create a request
    $params->{'$type'} = $operation;
    $params->{'_capability'} = $self->{CAPABILITY} if defined $self->{CAPABILITY};
    $params->{'_scene'} = $self->{SCENE} if defined $self->{SCENE};
    $params->{'_domain'} = $domain;
    $params->{'_asyncrequest'} = ($self->{REQUESTTYPE} eq 'async' ? 1 : 0);

    my $content = to_json($params,{canonical => 1});

    ## print STDERR "CONTENT: $content\n";
    ## print STDERR "URL: " . $self->{URL} . "\n";

    my $request = HTTP::Request->new;
    $request->method('POST');
    $request->uri($self->{URL});
    $request->header('Content-Type' => 'application/json');
    $request->header('Content-Length' => length $content);
    $request->content($content);

    my $result = $self->{_ua}->request($request);
    if (! $result->is_success)
    {
        croak "Message transmission failed; " . $result->status_line . "\n";
    }

    ## print STDERR "RESULT: " . $result->content . "\n";

    my $results;
    eval { $results = decode_json($result->content); };
    if ($@)
    {
        croak "JSON decode of string <" . $result->content . "> failed\n";
    }

    # if ($results->{"_Success"} <= 0)
    # {
    #     my $msg = $results->{"_Message"} || "unknown error";
    #     carp "Operation failed; $msg\n";
    # }

    return $results;
}

### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
###
### PACKAGE: RemoteControlPacket
###
### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
package RemoteControlPacket;

use Carp;
use JSON;

use IO::Socket;

use base 'RemoteControl';

# -----------------------------------------------------------------
# NAME: new 
# -----------------------------------------------------------------
sub new
{
    my $proto = shift;
    my $parms = ($#_ == 0) ? { %{ (shift) } } : { @_ };

    my $class = ref($proto) || $proto;
    my $self = RemoteControl->new($parms);
    
    bless $self, $class;

    # Copy the parameters into the object
    my %gAutoFields = ( ENDPOINT => undef );
    $self->{_permitted} = \%gAutoFields;

    # Set the initial values for all the parameters
    foreach my $key (keys %{$self->{_permitted}}) {
        $self->{$key} = $parms->{$key} || $self->{_permitted}->{$key};
    }

    return $self;
}

# -----------------------------------------------------------------
# NAME: _PostRequest
# -----------------------------------------------------------------
sub _PostRequest()
{
    my $self = shift;
    my $domain = shift;
    my $operation = shift;
    my ($params) = @_;

    croak 'Asynchronous enpoint undefined' unless defined $self->{ENDPOINT};

    # Create a request
    $params->{'$type'} = $operation;
    $params->{'_capability'} = $self->{CAPABILITY} if defined $self->{CAPABILITY};
    $params->{'_scene'} = $self->{SCENE} if defined $self->{SCENE};
    $params->{'_domain'} = $domain;
    $params->{'_asyncrequest'} = 1;

    my $content = to_json($params,{canonical => 1});
    ## print STDERR "CONTENT:\n$content\n";

    my $socket = new IO::Socket::INET( PeerAddr => $self->{ENDPOINT}, Proto => 'udp' );
    eval { $socket->send($content); $socket->close(); };
    if ($@)
    {
        croak "Message transmission failed; $!\n";
    }

    my $results = { _Success => 2 };
    return $results;
}


1;
__END__
