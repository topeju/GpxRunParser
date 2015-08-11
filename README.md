GpxRunParser
============

A simple console application which parses GPX 1.1 files that have heart rate and possibly cadence information, and outputs statistics on the file, based on heart rate zones etc. The application is simple to run e.g. using a file system based trigger that watches a directory and then launches GpxRunParser to analyze the GPX files when a new one is found.

Features
--------

* Parses all GPX files in a directory and outputs statistics in simple HTML files beside each GPX file.
  * Analysis is only performed on new/modified files
  * Analysis results of earlier runs are cached so they can be used in weekly/monthly statistics.
  * Report files from already-analyzed files will not be touched.
* Calculates time elapsed in each heart rate zone (zones specified using a command line parameter).
* Calculates time elapsed at different paces (bins specified using a command line parameter). Pace is calculated using moving averages to reduce the amount of noise in position data.
* Produces weekly and monthly statistics as well as statistics on each individual run. (Charts are only generated for individual runs.)
* Creates an index page that links each of the HTML files.
* Generates heart rate, pace, cadence, elevation and cumulative climb charts, with pauses in the workout highlighted.
  * The charts are generated using Javascript, and require an internet connection when viewing.
* Shows a Google Map of the run route.
  * The weekly and monthly reports show maps with routes sampled at approximately one-minute intervals (by default).


Planned features can be found on the Issues page on [GitHub](https://github.com/topeju/GpxRunParser)
