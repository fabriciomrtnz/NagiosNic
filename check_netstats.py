#!/usr/bin/env python
# /proc/net/dev parsers
# data structure
#{'pppoe0': {'recv_bytes': '17415041073',
#            'recv_compressed': '0',
#            'recv_drop': '0',
#            'recv_errs': '0',
#            'recv_fifo': '0',
#            'recv_frame': '0',
#            'recv_multicast': '0',
#            'recv_packets': '16141596',
#            'trans_bytes': '9749029007',
#            'trans_carrier': '0',
#            'trans_colls': '0',
#            'trans_compressed': '0',
#            'trans_drop': '0',
#            'trans_errs': '0',
#            'trans_fifo': '0',
#            'trans_packets': '16306397'}}
import sys
import argparse

parser = argparse.ArgumentParser()

parser.add_argument('-i', action='store', dest='iface',
                    help='Specify network interface.')

parser.add_argument('-p', action='store', dest='iparam',
                    help='Specify network interface parameter to track.')

parser.add_argument('-w', action='store', dest='pwarn', type=int,
                    help='Specify parameter warning value.')

parser.add_argument('-c', action='store', dest='pcrit', type=int,
                    help='Specify parameter critical value.')

parser.add_argument('--version', action='version', version='%(prog)s 1.0')

args = parser.parse_args()

if not args.iface or not args.iparam or not args.pwarn or not args.pcrit:
    print(parser.print_help())
    exit(3)

lines = open("/proc/net/dev", "r").readlines()

columnLine = lines[1]
_, receiveCols , transmitCols = columnLine.split("|")
receiveCols = map(lambda a:"recv_"+a, receiveCols.split())
transmitCols = map(lambda a:"trans_"+a, transmitCols.split())

cols = receiveCols+transmitCols

faces = {}
for line in lines[2:]:
    if line.find(":") < 0: continue
    face, data = line.split(":")
    face = face.strip()
    faceData = dict(zip(cols, data.split()))
    faces[face] = faceData

if args.iface not in faces:
    print(parser.print_help())
    exit(3)
elif args.iparam not in faces[args.iface]:
    print(parser.print_help())
    exit(3)

if int(faces[args.iface][args.iparam]) >= int(args.pcrit):
    print(args.iface + " " + args.iparam + " CRITICAL | [" + args.iface + "][" + args.iparam + "]=" + faces[args.iface][args.iparam])
    exit(2)
elif int(faces[args.iface][args.iparam]) >= int(args.pwarn):
    print(args.iface + " " + args.iparam + " WARNING | [" + args.iface + "][" + args.iparam + "]=" + faces[args.iface][args.iparam])
    exit(1)
else:
    print(args.iface + " " + args.iparam + " OK | [" + args.iface + "][" + args.iparam + "]=" + faces[args.iface][args.iparam])
    exit(0)