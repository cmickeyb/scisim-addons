### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
###
### PACKAGE: OpenSim::RemoteControl::Packet
###
### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
package OpenSim::RemoteControl::Packet;

use 5.006;
use strict;
use warnings FATAL => 'all';

=head1 NAME

OpenSim::RemoteControl::Packet - A package for sending packet-based remote-control messages to an OpenSim scene

=head1 SYNOPSIS

An extension for the OpenSim::RemoteControl class for sending packet-based (and
therefore asyncrhonous) messages to the Dispatcher interface on an OpenSim 
simulator. 

    $rc = OpenSim::RemoteControl::Packet->new(ENDPOINT => '127.0.0.1:7000');

=head1 EXPORT

Nothing exported

=head1 SUBROUTINES/METHODS

=cut

use Carp;
use JSON;

use IO::Socket;

use base 'OpenSim::RemoteControl';

# -----------------------------------------------------------------
=head2 new

Class constructor extends the constructor from the OpenSim::RemoteControl
class to include and ENDPOINT property. The ENDPOINT is a host:port pair (e.g. 127.0.0.1:45387)
that identifies a UDP port on the OpenSim simulator where the dispatcher listens.

=cut
# -----------------------------------------------------------------
sub new
{
    my $proto = shift;
    my $parms = ($#_ == 0) ? { %{ (shift) } } : { @_ };

    my $class = ref($proto) || $proto;
    my $self = OpenSim::RemoteControl->new($parms);
    
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

