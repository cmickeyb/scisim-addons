use 5.006;
use strict;
use warnings FATAL => 'all';
use ExtUtils::MakeMaker;

WriteMakefile(
    NAME             => 'OpenSim::RemoteControl',
    AUTHOR           => q{Mic Bowman <cmickeyb@gmail.com>},
    VERSION_FROM     => 'lib/OpenSim/RemoteControl.pm',
    ABSTRACT_FROM    => 'lib/OpenSim/RemoteControl.pm',
    PL_FILES         => {},
    MIN_PERL_VERSION => 5.006,
    CONFIGURE_REQUIRES => {
        'ExtUtils::MakeMaker' => 0,
    },
    EXE_FILES => [
        'tools/osremote.pl', 'tools/osinventory.pl'
    ],
    BUILD_REQUIRES => {
        'Test::More' => 0,
    },
    PREREQ_PM => {
        'Carp'			=> 0,
        'JSON'			=> 0,
        'LWP::UserAgent'	=> 0,
        'LWP::ConnCache'	=> 0,
        'HTTP::Request'		=> 0,
        'IO::Socket'		=> 0,
    },
    dist  => { COMPRESS => 'gzip -9f', SUFFIX => 'gz', },
    clean => { FILES => 'OpenSim-RemoteControl-*' },
);
