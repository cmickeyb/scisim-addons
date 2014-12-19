### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
###
### PACKAGE: OpenSim::RemoteControl::Stream
###
### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
package OpenSim::RemoteControl::Stream;

use 5.006;
use strict;
use warnings FATAL => 'all';

=head1 NAME

OpenSim::RemoteControl::Stream - A package for sending stream-based remote control messages to an OpenSim scene

=head1 SYNOPSIS

An extension for the OpenSim::RemoteControl class for sending stream-based
messages to the Dispatcher interface on an OpenSim simulator. The stream is
implemented through HTTP requests.

    $rc = OpenSim::RemoteControl::Stream->new(URL => 'http://127.0.0.1:7000/Dispatcher/');

=head1 EXPORT

Nothing exported

=head1 SUBROUTINES/METHODS

=cut

use Carp;
use JSON;

use LWP::UserAgent;
use LWP::ConnCache;
require HTTP::Request;

use base 'OpenSim::RemoteControl';

# -----------------------------------------------------------------
=head2 new

Class constructor extends the constructor from the OpenSim::RemoteControl
class to include an URL  property. The URL identifies the network connection
where the OpenSim Dispatcher listens.

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

    # print STDERR "CONTENT: $content\n";
    # print STDERR "URL: " . $self->{URL} . "\n";

    my $request = HTTP::Request->new;
    $request->method('POST');
    $request->uri($self->{URL});
    $request->header('Content-Type' => 'application/json');
    $request->header('Content-Length' => length $content);
    $request->header('Connection' => 'keep-alive');
    $request->content($content);

    my $result = $self->{_ua}->request($request);
    if (! $result->is_success)
    {
        croak "Message transmission failed; " . $result->status_line . "\n";
    }

    # print STDERR "RESULT: " . $result->content . "\n";

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
