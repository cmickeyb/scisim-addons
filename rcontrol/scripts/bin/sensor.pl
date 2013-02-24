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
use lib "$FindBin::Bin";
use lib "$FindBin::Bin/../lib";
use lib "/share/opensim/lib";
use lib '/usr/local/perl/5.10.0';

my $gCommand = $FindBin::Script;

use Getopt::Long;
use Time::HiRes;

use RemoteControl;
use Helper::CommandInfo;

# -----------------------------------------------------------------
# Globals
# -----------------------------------------------------------------
my $gLogFile = "$FindBin::Bin/monitor.log";
my $gPIDFile = "$FindBin::Bin/monitor.pid";
my $gRemoteControl;

# -----------------------------------------------------------------
# Configuration variables
# -----------------------------------------------------------------
my $gBackground = 0;
my $gInterval = 30;
my $gVerbose = 1;

my $gFamilyID = "b5004e60-c7db-4785-a4ec-3e4b5b2eb0c5";

my $gSceneName;
my $gEndPointURL;


my $gOptions = {
    'background!' 	=> \$gBackground,
    'i|interval=i'	=> \$gInterval,
    'v|verbose!'	=> \$gVerbose,

    'f|family=s'	=> \$gFamilyID,

    's|scene=s'		=> \$gSceneName,
    'u|url=s' 		=> \$gEndPointURL,
};

### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
### DAEMON ROUTINES
### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

# -----------------------------------------------------------------
# NAME: LogMessage
# DESC: Writes a time stamped message to the log file.
# -----------------------------------------------------------------
sub LogMessage {
    my $msg = shift;

    my @now = gmtime(time);
    my $stamp = join(":",($now[5]-100,$now[7],$now[2],$now[1],$now[0]));

    # Use the semaphore just to make sure we get full messages
    print STDERR "$stamp: $msg\n";
}

# -----------------------------------------------------------------
# NAME: QuitDaemon
# DESC: Log message and shutdown
# -----------------------------------------------------------------
sub QuitDaemon {
    my $msg = shift;
    &LogMessage($msg);
    &LogMessage("daemon shutdown");
    unlink $gPIDFile;
    exit 0;
}

# -----------------------------------------------------------------
# NAME: GetRunningPID
# DESC: Return the process id from the pid file if it exists
# -----------------------------------------------------------------
sub GetRunningPID {
    if (-f "$gPIDFile") {
	open(PID,"<$gPIDFile") || die "Unable to open pid file; $!\n";
	my $pid = <PID>;
	close(PID);
	if (defined $pid) {
	    chomp($pid);
	    return $pid;
	}
    }
    return -1;
}

# -----------------------------------------------------------------
# NAME: SaveRunningPID
# DESC: Save the process id in the pid file
# -----------------------------------------------------------------
sub SaveRunningPID {
    my $pid = shift;

    warn "PID file already exists\n" if -f "$gPIDFile";

    # Save the pid to the PID file
    return 0 unless open(PID,">$gPIDFile");
    return 0 unless print PID "$pid\n";
    close(PID);

    # Verify that it was written correctly
    my $vpid;
    return 0 unless open(RPID,"<$gPIDFile");
    return 0 unless defined ($vpid = <RPID>);
    chomp($vpid);
    close(RPID);

    # now verify that they match and return if successful
    return 0 unless $vpid == $pid;

    # everything looks OK
    return $pid;
}

# -----------------------------------------------------------------
# NAME: DetachProcess
# DESC: Detach the process from the controling terminal so it can
# run as a background daemon
# -----------------------------------------------------------------
sub DetachProcess {
    my $pid = fork;
    die "Unable to create daemon process; $!\n" unless defined $pid;
    
    # Let the parent process exit
    exit 0 if ($pid);

    # Detach from the controlling terminal
    my $sid = POSIX::setsid();
    die "Unable to detach process; $!\n" unless $sid;

    # Keep the process from acquiring a terminal on restart
    $SIG{'HUP'} = 'IGNORE';

    $pid = fork;
    die "Unable to create daemon process; $!\n" unless defined $pid;

    # Parent process writes pid to a file
    if ($pid) {
	if (! &SaveRunningPID($pid)) {
	    print STDERR "Unable to save pid $pid, exiting\n";
	    kill 9, $pid;
	    waitpid $pid,0;
	}
	exit 0;
    }

    # Now we are the child process
    chdir $FindBin::Bin;

    # Close open file descriptors
    for (my $fd = 0; $fd < 63; $fd++) {
	POSIX::close($fd);
    }

    # Reopen stdout, stdin to /dev/null
    open(STDIN,  "+>/dev/null");
    open(STDOUT, "+>&STDIN");

    # Reopen stderr to logfile, autoflush on
    open(STDERR, "+>$gLogFile");
    my $fh = select(STDERR); $| = 1; select($fh);

    &LogMessage("daemon started");
}

### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
### SENSOR ROUTINES
### XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

# -----------------------------------------------------------------
# NAME: HandleRecord
# -----------------------------------------------------------------
sub SendSensorData
{
    my ($sensor, $values) = @_;

    &LogMessage("send: $sensor=[" . join(',',@{$values}));
    $gRemoteControl->SensorDataRequest($gFamilyID,$sensor,$values);
}

# -----------------------------------------------------------------
# NAME: GenerateNetworkStats
# DESC: Sensor to generate network statistics
# -----------------------------------------------------------------
use Linux::net::dev;
sub GenerateNetworkStats
{
    my ($sec, $msec) = Time::HiRes::gettimeofday;

    my $devs = Linux::net::dev::info();
    foreach my $dev (sort keys %$devs)
    {
        my @val = ($sec, $msec);
        push(@val,$devs->{$dev}->{rbytes});
        push(@val,$devs->{$dev}->{rdrop});
        push(@val,$devs->{$dev}->{rerrs});
        push(@val,$devs->{$dev}->{tbytes});
        push(@val,$devs->{$dev}->{tdrop});
        push(@val,$devs->{$dev}->{terrs});

        &SendSensorData("net.$dev",\@val);
    }
}

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
    if (! GetOptions(%{$gOptions}))
    {
        die "Unknown option\n";
    }

    $gEndPointURL = $ENV{'OS_REMOTECONTROL_URL'} unless defined $gEndPointURL;
    $gSceneName = $ENV{'OS_REMOTECONTROL_SCENE'} unless defined $gSceneName;

    $gRemoteControl = RemoteControlStream->new(URL => $gEndPointURL, SCENE => $gSceneName);
    $gRemoteControl->{CAPABILITY} = &AuthenticateRequest;
}

# -----------------------------------------------------------------
# NAME: START
# DESC: Command to start the daemon
# -----------------------------------------------------------------
sub START {
    &Initialize();

    # check for existance of the process
    my $pid = &GetRunningPID;
    if ($pid > 0) {
	if (-d "/proc/$pid") {
	    die "Daemon process already running; $pid\n";
	}
    }

    # in order to open the socket, we must be root
    # die "Must be started as root\n" unless ($< == 0);

    # create the shutdown handler
    $SIG{QUIT} = sub {
	&QuitDaemon("QUIT signal received");
    };

    &DetachProcess if $gBackground;

    while (1)
    {
        &GenerateNetworkStats();
	sleep $gInterval;
    }

    # never reaches here if in background
    &QuitDaemon("monitor shutdown");
}

# -----------------------------------------------------------------
# NAME: STOP
# DESC: Command to stop the daemon using the pid from the pid file
# -----------------------------------------------------------------
sub STOP {
    # check for existance of the process
    my $pid = &GetRunningPID;
    if ($pid > 0) {
	if (-d "/proc/$pid") {
	    kill 3, $pid; # QUIT
	    sleep 5; # wait for it to shutdown
	    if (-d "/proc/$pid") {
		kill 9,$pid; # now REALLY kill it
	    }
	    unlink $gPIDFile;
	    exit 0;
	}
    }
    warn "Unable to find daemon process\n";
    exit 0;
}

# -----------------------------------------------------------------
# NAME: STATUS
# DESC: Command to see if the daemon is running, check the pid in
# the pid file
# -----------------------------------------------------------------
sub STATUS {
    # check for existance of the process
    my $pid = &GetRunningPID;
    if ($pid > 0) {
	if (-d "/proc/$pid") {
	    print "Daemon process running; $pid\n";
	    exit 0;
	}
    }

    warn "Unable to find daemon process\n";
    exit 0;
}

# -----------------------------------------------------------------
# NAME: RESTART
# DESC: Combined stop/start command
# -----------------------------------------------------------------
sub RESTART {
    # check for existance of the process
    my $pid = &GetRunningPID;
    if ($pid > 0) {
	kill 3, $pid if -d "/proc/$pid"; # QUIT
	sleep 1; # wait for the shutdown
    }

    &START;
}

# -----------------------------------------------------------------
# NAME: Main
# DESC: Main controling routine.
# -----------------------------------------------------------------
sub Main {
    my $cmd = ($#ARGV >= 0) ? shift @ARGV : "HELP";

    &START, exit   if ($cmd =~ m/^start$/i);
    &STATUS, exit  if ($cmd =~ m/^status$/i);
    &STOP, exit    if ($cmd =~ m/^stop$/i);
    &RESTART, exit if ($cmd =~ m/^restart$/i);

    &Usage();
}

# -----------------------------------------------------------------
&Main;
