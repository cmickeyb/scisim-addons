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

my $gCommand = $FindBin::Script;

use JSON;
use Getopt::Long;

my @gAttractorList = ();

my @PositionStdd = (20.0, 20.0, 20.0);

my $gAttractorData = {};
my $gEntityData = {};
my $gGlobalData = {};

my @gDefaultCenter = (128.0, 128.0, 200.0);
my @gCenter = ();
my $gRange = 100.0;
my $gDimension = 4;
my $gEntityCount = 12;
my $gAttractorCount = 3;
my $gMassMean = 5.0;
my $gMassStdd = 1.0;
my $gVelocity = 2.0;
my $gExperiment = "nbody";
my $gPrettyPrint = 0;

my $gOptions = {
    'a|attractor=i'	=> \$gAttractorCount,
    'c|center=f{3}'     => \@gCenter,
    'd|dimension=i'	=> \$gDimension,
    'e|entity=i'	=> \$gEntityCount,
    'm|mass=f'          => \$gMassMean,
    'r|range=f'         => \$gRange,
    'v|velocity=f'      => \$gVelocity,
    'x|experiment=s'	=> \$gExperiment,

    'p|pretty!'		=> \$gPrettyPrint
};

# -----------------------------------------------------------------
# NAME:
# DESC:
# -----------------------------------------------------------------
sub ClampPositionVector
{
    my $vect = shift @_;
    my $len = shift @_;

    my $mag = sqrt($vect->[0] * $vect->[0] + $vect->[1] * $vect->[1] + $vect->[2] * $vect->[2]);
    if ($mag > $len)
    {
        $vect->[0] = $vect->[0] * $len / $mag;
        $vect->[1] = $vect->[1] * $len / $mag;
        $vect->[2] = $vect->[2] * $len / $mag;
    }
}

# -----------------------------------------------------------------
# NAME:
# DESC:
# -----------------------------------------------------------------
sub GenerateGuassian
{
    my $mean = shift @_;
    my $stddev = shift @_;

    my ($s, $v1, $v2);

    do
    {
        $v1 = 2 * rand() - 1;
        $v2 = 2 * rand() - 1;
        $s = $v1 * $v1 + $v2 * $v2;
    } while ($s >= 1);

    my $ug = sqrt(-2 * log($s) / $s) * $v1;
    return $mean + $stddev * $ug;
}

# -----------------------------------------------------------------
# NAME:
# DESC:
# -----------------------------------------------------------------
sub GenerateValues
{
    my $cnt = shift @_;
    my @values;
    my $mag = 0;
    for ($i = 0; $i < $cnt; $i++)
    {
        my $value = rand();
        ## $mag += $value * $value;

        push(@values,$value);
    }

    ## $mag = sqrt($mag);
    ## @values = map { int($_ * 1000.0 / $mag) / 1000.0 } @values;

    @values = map { int($_ * 1000.0) / 1000.0 } @values;
    return \@values;
}

# -----------------------------------------------------------------
# NAME:
# DESC: Generate a vector of values that is close to an attractor
# -----------------------------------------------------------------
sub GenerateSimilarValues
{
    my $cnt = shift @_;

    if ((rand() > 0.6) || ($#gAttractorList < 0))
    {
        return &GenerateValues($cnt);
    }

    my $index = int(rand() * ($#gAttractorList + 1));
    
    my @values;
    my $mag = 0;
    for ($i = 0; $i < $cnt; $i++)
    {
        my $value = &GenerateGuassian($gAttractorList[$index]->[$i],0.05);
        $value = ($value < 0.0 ? 0.0 : ($value > 1.0 ? 1.0 : $value));
        ## $mag += $value * $value;

        push(@values,$value);
    }

    # $mag = sqrt($mag);
    # @values = map { int($_ * 1000.0 / $mag) / 1000.0 } @values;

    @values = map { int($_ * 10000.0) / 10000.0 } @values;
    return \@values;
}

# -----------------------------------------------------------------
# NAME:
# DESC:
# -----------------------------------------------------------------
sub GenerateVelocity
{
    my @velocity = (0.0, 0.0, 0.0);

    $velocity[0] = &GenerateGuassian(0,$gVelocity);
    $velocity[1] = &GenerateGuassian(0,$gVelocity);
    $velocity[2] = &GenerateGuassian(0,$gVelocity);
    
    &ClampPositionVector(\@velocity,$gVelocity);

    return sprintf("<%.2f,%.2f,%.2f>",$velocity[0],$velocity[1],$velocity[2]);
}

# -----------------------------------------------------------------
# NAME:
# DESC:
# -----------------------------------------------------------------
sub GeneratePosition
{
    my @position = (0.0, 0.0, 0.0);

    $position[0] = &GenerateGuassian(0,$gRange/2.0);
    $position[1] = &GenerateGuassian(0,$gRange/2.0);
    $position[2] = &GenerateGuassian(0,$gRange/2.0);
    
    &ClampPositionVector(\@position,$gRange/2.0);

    $position[0] += $gCenter[0];
    $position[1] += $gCenter[1];
    $position[2] += $gCenter[2];

    return sprintf("<%.2f,%.2f,%.2f>",$position[0],$position[1],$position[2]);
}

# -----------------------------------------------------------------
# NAME:
# DESC:
# -----------------------------------------------------------------
sub GeneratePositionFromValue
{
    my $value = shift(@_);
    my $mag = sqrt($value->[0] * $value->[0] + $value->[1] * $value->[1] + $value->[2] * $value->[2]);

    my @position = (0.0, 0.0, 0.0);

    $position[0] = $gCenter[0] + $value->[0] * $gRange / (2.0 * $mag);
    $position[1] = $gCenter[1] + $value->[1] * $gRange / (2.0 * $mag);
    $position[2] = $gCenter[2] + $value->[2] * $gRange / (2.0 * $mag);

    return sprintf("<%.2f,%.2f,%.2f>",$position[0],$position[1],$position[2]);
}

# -----------------------------------------------------------------
# NAME:
# DESC:
# -----------------------------------------------------------------
sub GenerateEntity
{
    my $cnt = shift @_;

    my $result = {};
    $result->{'mass'} = ($gMassMean == 0 ? 1.0 : int(&GenerateGuassian($gMassMean,$gMassStdd) * 100.0) / 100.0);
    $result->{'value'} = &GenerateSimilarValues($cnt);
    $result->{'position'} = &GeneratePosition();
    $result->{'velocity'} = &GenerateVelocity();

    return $result;
}

# -----------------------------------------------------------------
# NAME:
# DESC:
# -----------------------------------------------------------------
sub GenerateEntityList
{
    my $bcount = shift @_;
    my $vcount = shift @_;


    $gEntityData->{'EntityList'} = ();

    for (my $bodies = 0; $bodies < $bcount; $bodies++)
    {
        push(@{$gEntityData->{'EntityList'}},&GenerateEntity($vcount));
    }
    $gEntityData->{'EntityListCount'} = $bcount;
}

# -----------------------------------------------------------------
# NAME:
# DESC:
# -----------------------------------------------------------------
sub GenerateAttractor
{
    my $cnt = shift @_;

    my $result = {};
    $result->{'mass'} = ($gMassMean == 0 ? 1.0 : int(&GenerateGuassian($gMassMean * 2.0,$gMassStdd * 2.0) * 100.0) / 100.0);
    $result->{'value'} = &GenerateValues($cnt);
    $result->{'position'} = &GeneratePositionFromValue($result->{'value'});
    $result->{'velocity'} = &GenerateVelocity();

    push(@gAttractorList,$result->{'value'});

    return $result;
}

# -----------------------------------------------------------------
# NAME:
# DESC:
# -----------------------------------------------------------------
sub GenerateAttractorList
{
    my $acount = shift @_;
    my $vcount = shift @_;

    $gAttractorData->{'AttractorList'} = ();

    for (my $attractors = 0; $attractors < $acount; $attractors++)
    {
        push(@{$gAttractorData->{'AttractorList'}},&GenerateAttractor($vcount));
    }

    $gAttractorData->{'AttractorListCount'} = $acount;
}

# -----------------------------------------------------------------
# NAME:
# DESC:
# -----------------------------------------------------------------
sub Main
{
    if (! GetOptions(%{$gOptions}))
    {
        die "unable to process arguments";
    }

    # if center was not set in the parameters, copy it
    # option processing would just append to the end if we defaulted 
    @gCenter = @gDefaultCenter if $#gCenter < 0;
    

    $gGlobalData->{'Center'} = sprintf("<%.2f,%.2f,%.2f>",$gCenter[0],$gCenter[1],$gCenter[2]);
    $gGlobalData->{'Range'} = $gRange;
    $gGlobalData->{'Dimension'} = $gDimension;
    $gGlobalData->{'UseMass'} = ($gMassMean != 0 ? "yes" : "no");
    $gGlobalData->{'UseVelocity'} = ($gVelocity != 0 ? "yes" : "no");
    $gGlobalData->{'AttractorsMove'} = "yes";
    $gGlobalData->{'EntitiesAttract'} = "no";
    $gGlobalData->{'SimulationType'} = 1;

    # --------------- Create the Attractors ---------------
    $gGlobalData->{'AttractorDataCollection'} = ();

    my $acount = 0;
    while ($gAttractorCount > 0)
    {
        $gAttractorData = {};
        &GenerateAttractorList(($gAttractorCount > 500 ? 500 : $gAttractorCount),$gDimension);
        $gAttractorCount -= 500;
        $acount++;

        my $file = "$gExperiment$acount-a.txt";
        open(ED,"> $file") || die "Unable to open attractor data file";
        print ED to_json($gAttractorData, {pretty => $gPrettyPrint});
        close ED;
        push(@{$gGlobalData->{'AttractorDataCollection'}},$file);
    }
    $gGlobalData->{'AttractorDataCollectionCount'} = $acount;

    # --------------- Create the Entities ---------------
    $gGlobalData->{'EntityDataCollection'} = ();

    my $ecount = 0;
    while ($gEntityCount > 0)
    {
        $gEntityData = {};
        &GenerateEntityList(($gEntityCount > 500 ? 500 : $gEntityCount),$gDimension);
        $gEntityCount -= 500;
        $ecount++;

        my $file = "$gExperiment$ecount-e.txt";
        open(ED,"> $file") || die "Unable to open attractor data file";
        print ED to_json($gEntityData, {pretty => $gPrettyPrint});
        close ED;

        push(@{$gGlobalData->{'EntityDataCollection'}},$file);
    }
    $gGlobalData->{'EntityDataCollectionCount'} = $ecount;

    # --------------- Create the Configuration ---------------
    open(GD,"> $gExperiment-c.txt") ||
        die "Unable to open global config file";
    print GD to_json($gGlobalData, {pretty => 1});
    close GD;
}

&Main;

