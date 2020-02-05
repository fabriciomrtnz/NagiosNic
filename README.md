NagiosNic
======
I could not find a simple nagios plugin that monitorized all NICs without the use of SNMP. So made this plugin using C# and mono.

Also, created (using code from stack overflow) python script that captures data from /proc/net/dev (same as ifconfig) and runs checks on it.
