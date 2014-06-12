GpxRunParser
============

A simple console application which parses GPX 1.1 files that have heart rate and possibly cadence information, and outputs statistics on the file, based on heart rate zones etc.

Current features
----------------

* Parses a single GPX file and outputs statistics in a simple HTML file beside the GPX file.
* Calculates time elapsed in each heart rate zone (zones specified using a command line parameter).
* Calculates time elapsed at different paces (bins specified using a command line parameter). Note that the pace is calculated using two consecutive track points, which isn't very high quality due to GPS accuracy issues.


Planned features
----------------

* Calculate statistics on all GPX files in a directory at once
* Produce weekly and monthly statistics
* Calculate pace using more reasonable averaging
