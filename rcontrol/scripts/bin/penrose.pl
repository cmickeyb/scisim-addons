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
use Time::HiRes qw(usleep);
use Math::Trig;

use FileHandle;
use Getopt::Long;
use Term::ReadKey;

use RemoteControl;
use Helper::CommandInfo;

my $gTileCount = 0;
my $gRemoteControl;

my $gAssetID;
my $gInputFile;
my $gBasePosition = ();
my $gSceneName = 'Scratch 10';
my $gEndPointURL = 'http://m6.virtualportland.org:7060/Dispatcher';
my $gXRange = 10;
my $gYRange = 10;

my $gSymmetry = 5;
my $gMaxMax = 30;
my $gScale = 1.0;
my $gUseAsync = 0;

my $gOptions = {
    'a|asset=s'		=> \$gAssetID,
    'f|file=s'		=> \$gInputFile,
    'l|location=f{3}'	=> \@gBasePosition,
    's|scene=s'		=> \$gSceneName,
    'u|url=s' 		=> \$gEndPointURL,
    'x|xrange=i'	=> \$gXRange,
    'y|yrange=i'	=> \$gYRange,

    'scale=f'           => \$gScale,
    'sym=i'		=> \$gSymmetry,
    'dim=i'		=> \$gMaxMax,
    'async!'		=> \$gUseAsync
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

    $gSynchEP = $ENV{'OS_REMOTECONTROL_URL'} unless defined $gSynchEP;
    $gAsyncEP = $ENV{'OS_REMOTECONTROL_UDP'} unless defined $gAsyncEP;
    $gSceneName = $ENV{'OS_REMOTECONTROL_SCENE'} unless defined $gSceneName;

    # choose whether to run UDP or HTTP protocols
    $gRemoteControl = RemoteControlStream->new(URL => $gSynchEP, SCENE => $gSceneName, REQUESTTYPE => ($gUseAsync ? 'async' : 'sync'));
    ## $gRemoteControl = RemoteControlPacket->new(ENDPOINT => $gAsyncEP, SCENE => $gSceneName);
    $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;
}
    
# -----------------------------------------------------------------
# NAME: Euler2Rot
# DESC: Convert an Eulerian orientation into a quaternion
# -----------------------------------------------------------------
sub Euler2Rot
{
    my ($x, $y, $z) = @_;

    my $c1 = cos($x * 0.5);
    my $c2 = cos($y * 0.5);
    my $c3 = cos($z * 0.5);
    my $s1 = sin($x * 0.5);
    my $s2 = sin($y * 0.5);
    my $s3 = sin($z * 0.5);

    $x = $s1 * $c2 * $c3 + $c1 * $s2 * $s3;
    $y = $c1 * $s2 * $c3 - $s1 * $c2 * $s3;
    $z = $s1 * $s2 * $c3 + $c1 * $c2 * $s3;
    $s = $c1 * $c2 * $c3 - $s1 * $s2 * $s3;

    my @quat = ($x, $y, $z, $s);
    return @quat;
}

# -----------------------------------------------------------------
# NAME: InsideBoundaryP
# DESC: Determine if a tile falls inside the region boundary
# -----------------------------------------------------------------
sub InsideBoundaryP
{
    my $point = shift;
    for (my $i = 0; $i < 4; $i++)
    {
        return 0 if abs($point->{points}->[$i]->{x}) > $gXRange;
        return 0 if abs($point->{points}->[$i]->{y}) > $gYRange;
    }

    return 1;
}

# -----------------------------------------------------------------
# NAME: CreateTile
# DESC: Create a tile if it lies within the region boundary
# -----------------------------------------------------------------
sub CreateTile
{
    my ($tile) = @_;

    # Scale the point
    for (my $i = 0; $i < 4; $i++)
    {
        $tile->{points}->[$i]->{x} = 0 unless defined $tile->{points}->[$i]->{x};
        $tile->{points}->[$i]->{y} = 0 unless defined $tile->{points}->[$i]->{y};

        $tile->{points}->[$i]->{x} = $gScale * $tile->{points}->[$i]->{x};
        $tile->{points}->[$i]->{y} = $gScale * $tile->{points}->[$i]->{y};
    }

    return unless &InsideBoundaryP($tile);

    # compute the position
    my $xoff = $tile->{points}->[0]->{x};
    my $yoff = $tile->{points}->[0]->{y};
    my @gPosition = ($gBasePosition[0] + $xoff, $gBasePosition[1] + $yoff, $gBasePosition[2]);

    # compute the rotation
    my $rx = $tile->{points}->[2]->{x} - $tile->{points}->[0]->{x};
    my $ry = $tile->{points}->[2]->{y} - $tile->{points}->[0]->{y};
    my @gRotation = Euler2Rot(0,0,atan2($ry,$rx));

    my $gName = sprintf("Penrose Tile %d",$gTileCount++);
    my $gDesc = "";
    my @gVelocity = (0.0, 0.0, 0.0);
    my $gParam = to_json($tile);

    my $result = $gRemoteControl->CreateObject($gAssetID,\@gPosition,\@gRotation,\@gVelocity,$gName,$gDesc,$gParam);
    unless (defined $result)
    {
        print STDERR "Operation failed; no response\n";
        return undef;
    }
    if ($result->{_Success} <= 0)
    {
        print STDERR "Operation failed; " . $result->{_Message} . "\n";
        return undef;
    }

    return $result->{ObjectID};
}

# -----------------------------------------------------------------
# NAME: ComputeQuasi
# DESC: Compute N-symmetry quasi-tiling pattern
# This code was adapted from an application written by Eric Weeks called quasi.c.
# Here is the link to this program:
# http://www.physics.emory.edu/~weeks/software/quasi.html
# email: weeks@physics.emory.edu
#
# This program is public domain, but please leave Eric's name, email, and web link.
# -----------------------------------------------------------------
sub ComputeQuasi
{
    my @index = (0) x $gSymmetry;
    my @vx = (0.0) x $gSymmetry;
    my @vy = (0.0) x $gSymmetry;
    my @mm = (0.0) x $gSymmetry;
    my @b = ();

    my $halfmax = $gMaxMax/2.0;
    my $themax = ($gMaxMax - 1);
    my $themin = $themax / 2;

    # initialize the control structures
    for (my $t = 0; $t < $gSymmetry; $t++)
    {
        my $phi = ($t * 2.0) / (1.0 * $gSymmetry) * pi;
        $vx[$t] = cos($phi);
        $vy[$t] = sin($phi);
        $mm[$t] = $vy[$t] / $vx[$t];

        $b[$t] = ();
        for (my $r = 0; $r < $gMaxMax; $r++)
        {
            my $y1 = $vy[$t] * ($t * 0.1132) - $vx[$t] * ($r - $halfmax); # /* offset */
            my $x1 = $vx[$t] * ($t * 0.2137) + $vy[$t] * ($r - $halfmax);
            $b[$t][$r] = $y1 - $mm[$t] * $x1;
        }
    }

    # /* t is 1st direction, r is 2nd.  look for intersection between pairs
    #  * of lines in these two directions. (will be x0,y0) */

    for (my $minmin = 0.0; $minmin <= $themax; $minmin += 0.4)
    {
        my $rad1 = $minmin * $minmin;
        my $rad2 = ($minmin + 0.4) * ($minmin + 0.4);

        for (my $n = 1; $n < $themax; $n++)
        {
            for (my $m = 1; $m < $themax; $m++)
            {
                my $rad = (($n - $themin) * ($n - $themin) + ($m - $themin) * ($m - $themin));
                if (($rad >= $rad1) && ($rad < $rad2))
                {
                    for (my $t = 0; $t < ($gSymmetry - 1); $t++)
                    {
                        for (my $r = $t + 1; $r < $gSymmetry; $r++)
                        {
                            my $x0 = ($b[$t][$n] - $b[$r][$m])/($mm[$r]-$mm[$t]);
                            my $y0 = $mm[$t]*$x0 + $b[$t][$n];
                            my $flag = 0;

                            for (my $i = 0; $i < $gSymmetry; $i++)
                            {
                                if (($i != $t) && ($i != $r))
                                {
                                    my $dx = -$x0 * $vy[$i] + ($y0 - $b[$i][0]) * $vx[$i];
                                    $index[$i] = int(-$dx);
                                    if (($index[$i] > ($gMaxMax-3)) || ($index[$i] < 1))
                                    {
                                        $flag = 1;
                                    }
                                }
                            }

                            if ($flag == 0)
                            {
                                my $tile = {};

                                $index[$t] = $n-1;
                                $index[$r] = $m-1;
                                $x0 = 0.0;
                                $y0 = 0.0;
                                for (my $i = 0; $i < $gSymmetry; $i++)
                                {
                                    $x0 += $vx[$i]*$index[$i];
                                    $y0 += $vy[$i]*$index[$i];
                                }

                                # /* ---------- COLOR ---------- */
                                my $color = 0.0;
                                for (my $i = 0; $i < $gSymmetry; $i++)
                                {
                                    $color += $index[$i];
                                }
                                while ($color > (($gSymmetry - 1.0) / 2.0))  
                                {
                                    $color -= (($gSymmetry - 1.0) / 2.0);
                                }
                                $color = $color / (($gSymmetry-1.0) / 2.0) * 0.8 + 0.1;
                                $color += abs($vx[$t] * $vx[$r] + $vy[$t] * $vy[$r]);
                                if ($color > 1.0)
                                {
                                    $color -= 1.0;
                                }

                                $tile->{color} = $color;

                                # /* ---------- COLOR ---------- */
                                $tile->{points} = [];
                                    
                                $x0 += $vx[$t];
                                $y0 += $vy[$t];
                                $tile->{distance} = sqrt($x0*$x0 + $y0*$y0);
                                push(@{$tile->{points}},{ 'x' => $x0, 'y' => $y0 });

                                $x0 += $vx[$r];
                                $y0 += $vy[$r];
                                push(@{$tile->{points}},{ 'x' => $x0, 'y' => $y0 });
                                    
                                $x0 -= $vx[$t];
                                $y0 -= $vy[$t];
                                push(@{$tile->{points}},{ 'x' => $x0, 'y' => $y0 });
                                    
                                $x0 -= $vx[$r];
                                $y0 -= $vy[$r];
                                push(@{$tile->{points}},{ 'x' => $x0, 'y' => $y0 });

                                &CreateTile($tile);
                            }
                        }
                    }
                }
            }
        }
    }
}

# -----------------------------------------------------------------
# NAME: ProcessFile
# DESC: Read a list of points from a file
# -----------------------------------------------------------------
sub ProcessFile
{
    my $fh = FileHandle->new("<$gInputFile");
    die "Unable to open $gInputFile; $!\n" unless defined $fh;

    while (<$fh>)
    {
        my $tile = decode_json($_);
        &CreateTile($tile);
    }

    $fh->close();
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

    if (defined $gInputFile)
    {
        &ProcessFile; 
    }
    else
    {
        &ComputeQuasi;
    }
}

&Main;
