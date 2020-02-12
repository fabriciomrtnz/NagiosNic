#!/usr/bin/env python

import sys
import pprint
import argparse
import subprocess
import json
import os

parser = argparse.ArgumentParser()

parser.add_argument('-c', '--speedtest-cli', action='store', dest='cmd', required=True,
                            help='Specify speedtest cli route.')

parser.add_argument('-s', '--server', action='store', dest='server', required=True,
                            help='Specify test server id.')

parser.add_argument('-dw', action='store', dest='dwarn', type=int, required=True,
                            help='Specify download warning.')

parser.add_argument('-dc', action='store', dest='dcritic', type=int, required=True,
                            help='Specify download critical.')

parser.add_argument('-uw', action='store', dest='uwarn', type=int, required=True,
                            help='Specify upload warning.')

parser.add_argument('-uc', action='store', dest='ucritic', type=int, required=True,
                            help='Specify upload critical.')

parser.add_argument('--version', action='version', version='%(prog)s 1.0')

args = parser.parse_args()

#status, output = commands.getstatusoutput(args.cmd + " -s " + args.server + " -f json")

cp = subprocess.Popen([args.cmd, '-s', args.server, '-f', 'json'], stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
(output, err) = cp.communicate()

data = json.loads(output)

download = int(data["download"]["bandwidth"]) / 131072
upload = int(data["upload"]["bandwidth"]) / 131072
ping = data["ping"]["latency"]
packetloss = data["packetLoss"]

if int(download) <= int(args.dcritic):
    print("DOWNLOAD CRITICAL | DOWNLOAD=" + str(download) + " UPLOAD=" + str(upload) + " PING=" + str(ping) + " PACKETLOSS=" + str(packetloss))
    exit(2)
elif int(upload) <= int(args.ucritic):
    print("UPLOAD CRITICAL | DOWNLOAD=" + str(download) + " UPLOAD=" + str(upload) + " PING=" + str(ping) + " PACKETLOSS=" + str(packetloss))
    exit(2)
elif int(download) <= int(args.dwarn):
    print("DOWNLOAD WARNING | DOWNLOAD=" + str(download) + " UPLOAD=" + str(upload) + " PING=" + str(ping) + " PACKETLOSS=" + str(packetloss))
    exit(1)
elif int(upload) <= int(args.uwarn):
    print("UPLOAD WARNING | DOWNLOAD=" + str(download) + " UPLOAD=" + str(upload) + " PING=" + str(ping) + " PACKETLOSS=" + str(packetloss))
    exit(1)
else:
    print("OK | DOWNLOAD=" + str(download) + " UPLOAD=" + str(upload) + " PING=" + str(ping) + " PACKETLOSS=" + str(packetloss))
    exit(0)
