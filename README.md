GpxRunParser
============

A simple console application which parses GPX 1.1 files that have heart rate and possibly cadence information, and outputs statistics on the file, based on heart rate zones etc. The application is simple to run e.g. using a file system based trigger that watches a directory and then launches GpxRunParser to analyze the GPX files when a new one is found.

Current features
----------------

* Parses all GPX files in a directory and outputs statistics in simple HTML files beside each GPX file.
* Generates heart rate and pace charts, with pauses in the workout highlighted.
* Calculates time elapsed in each heart rate zone (zones specified using a command line parameter).
* Calculates time elapsed at different paces (bins specified using a command line parameter). Pace is calculated using moving averages to reduce the amount of noise in position data.
* Produces weekly and monthly statistics as well as statistics on each individual run. (Charts are only generated for individual runs.)


Planned features
----------------

* Create an index page that links each of the HTML files.
* Apply styling to the pages (they are now very bare-bones).
* Generate maps for each run, with color-coding by pace.
* Calculate elevation based on map data (GPX data probably way too noisy).
