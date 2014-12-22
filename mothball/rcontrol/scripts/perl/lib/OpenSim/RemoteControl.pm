### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
###
### PACKAGE: OpenSim::RemoteControl
###
### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
package OpenSim::RemoteControl;

use 5.010001;
use strict;
use warnings;

=head1 NAME

OpenSim::RemoteControl - A package for building clients that access and managing scenes in the OpenSimulator 3D application platform

=head1 VERSION

Version 0.04

=cut

our $VERSION = '0.4';

=head1 SYNOPSIS

OpenSim::RemoteControl provides a procedural interface for sending and
receiving OpenSim Dispatcher messages. These messages provide a means of
interacting with an OpenSim scene including the objects in the scene,
avatars, terrain, and others.

    $rc = OpenSim::RemoteControl::Stream->new(URL => 'http://127.0.0.1:700', SCENE => 'My Region');
    $rc->CAPABILITY = $rc->AuthenticateAvatarByName("Sam Spade", "mypass", 3600);

=head1 EXPORT

A list of functions that can be exported.  You can delete this section
if you don't export anything, such as for a purely object-oriented module.

=head1 SUBROUTINES/METHODS

=cut

use Carp;
use JSON;

use Digest::MD5 qw(md5_hex);
use MIME::Base64;

my @gDomainList = qw/Dispatcher RemoteControl/;

# -----------------------------------------------------------------
=head2 AuthenticateAvatarByUUID
=cut
# -----------------------------------------------------------------
sub AuthenticateAvatarByUUID
{
    my $self = shift;
    my ($uuid, $pass, $lifespan) = @_;

    my $params = {};
    $params->{'userid'} = $uuid;
    $params->{'hashedpasswd'} = '$1$' . md5_hex($pass);
    $params->{'lifespan'} = $lifespan if defined $lifespan;
    $params->{'domainlist'} = $self->{DOMAINLIST};

    return $self->_PostRequest('Dispatcher','Dispatcher.Messages.CreateCapabilityRequest',$params);
}

# -----------------------------------------------------------------
=head2 AuthenticateAvatarByName
=cut
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
    $params->{'domainlist'} = $self->{DOMAINLIST};

    return $self->_PostRequest('Dispatcher','Dispatcher.Messages.CreateCapabilityRequest',$params);
}

# -----------------------------------------------------------------
=head2 AuthenticateAvatarByEmail
=cut
# -----------------------------------------------------------------
sub AuthenticateAvatarByEmail
{
    my $self = shift;
    my ($email, $pass, $lifespan) = @_;

    my $params = {};
    $params->{'emailaddress'} = $email;
    $params->{'hashedpasswd'} = '$1$' . md5_hex($pass);
    $params->{'lifespan'} = $lifespan if defined $lifespan;
    $params->{'domainlist'} = $self->{DOMAINLIST};

    return $self->_PostRequest('Dispatcher','Dispatcher.Messages.CreateCapabilityRequest',$params);
}

# -----------------------------------------------------------------
=head2 RenewCapability
=cut
# -----------------------------------------------------------------
sub RenewCapability
{
    my $self = shift;
    my ($lifespan) = @_;

    my $params = {};
    $params->{'lifespan'} = $lifespan if defined $lifespan;

    return $self->_PostRequest('Dispatcher','Dispatcher.Messages.RenewCapabilityRequest',$params);
}

# -----------------------------------------------------------------
=head2 Info
=cut
# -----------------------------------------------------------------
sub Info
{
    my $self = shift;
    return $self->_PostRequest('Dispatcher','Dispatcher.Messages.InfoRequest',{});
}

# -----------------------------------------------------------------
=head2 MessageFormatRequest
=cut
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
=head2 CreateEndPoint
=cut
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
=head2 RenewEndPoint
=cut
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
=head2 CloseEndPoint
=cut
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
=head2 SendChatMessage
=cut
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
=head2 GetAvatarAppearance
=cut
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
=head2 SetAvatarAppearance
=cut
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
=head2 FindObjects
=cut
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
=head2 CreateObject
=cut
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
    $params->{'StartParameter'} = defined $parm ? $parm : "{}";

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.CreateObjectRequest',$params);
}

# -----------------------------------------------------------------
=head2 DeleteObject
=cut
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
=head2 DeleteAllObject
=cut
# -----------------------------------------------------------------
sub DeleteAllObjects
{
    my $self = shift;

    my $params = {};
    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.DeleteAllObjectsRequest',$params);
}

# -----------------------------------------------------------------
=head2 GetObjectParts
=cut
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
=head2 GetObjectInventory
=cut
# -----------------------------------------------------------------
sub GetObjectInventory
{
    my $self = shift;
    my ($object) = @_;

    my $params = {};
    $params->{'ObjectID'} = $object;

    return $self->_PostRequest('RemoteControl','RemoteControl.Messages.GetObjectInventoryRequest',$params);
}

# -----------------------------------------------------------------
=head2 GetObjectData
=cut
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
=head2 GetObjectPosition
=cut
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
=head2 SetObjectPosition
=cut
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
=head2 GetObjectRotation
=cut
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
=head2 SetObjectRotation
=cut
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
=head2 MessageObject
=cut
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
=head2 SetPartPosition
=cut
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
=head2 SetPartRotation
=cut
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
=head2 SetPartScale
=cut
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
=head2 SetPartColor
=cut
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
=head2 RegisterTouchCallback
=cut
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
=head2 UnregisterTouchCallback
=cut
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
=head2 TestAsset
=cut
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
=head2 GetAsset
=cut
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
=head2 GetAssetFromObject
=cut
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
=head2 GetDependentAssets
=cut
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
=head2 AddAsset
=cut
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
=head2 new
Constructor for the object, attributes listed in gAutoFields an be initialized here.
=cut
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

=head1 AUTHOR

Mic Bowman, C<< <cmickeyb at gmail.com> >>

=head1 BUGS

Please report any bugs or feature requests to C<bug-opensim-remotecontrol at rt.cpan.org>, or through
the web interface at L<http://rt.cpan.org/NoAuth/ReportBug.html?Queue=OpenSim-RemoteControl>.  I will be notified, and then you'll
automatically be notified of progress on your bug as I make changes.

=head1 SUPPORT

You can find documentation for this module with the perldoc command.

    perldoc OpenSim::RemoteControl

=head1 ACKNOWLEDGEMENTS


=head1 LICENSE AND COPYRIGHT

Copyright (c) 2012, 2014 Intel Corporation
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above
      copyright notice, this list of conditions and the following
      disclaimer in the documentation and/or other materials provided
      with the distribution.

    * Neither the name of the Intel Corporation nor the names of its
      contributors may be used to endorse or promote products derived
      from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE INTEL OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

EXPORT LAWS: THIS LICENSE ADDS NO RESTRICTIONS TO THE EXPORT LAWS OF
YOUR JURISDICTION. It is licensee's responsibility to comply with any
export regulations applicable in licensee's jurisdiction. Under
CURRENT (May 2000) U.S. export regulations this software is eligible
for export from the U.S. and can be downloaded by or otherwise
exported or reexported worldwide EXCEPT to U.S. embargoed destinations
which include Cuba, Iraq, Libya, North Korea, Iran, Syria, Sudan,
Afghanistan and any other country to which the U.S. has embargoed
goods and services.

=cut

1;
__END__
