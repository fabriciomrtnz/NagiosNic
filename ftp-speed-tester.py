#!/usr/bin/env python

import argparse, sys, os
import ftplib
import datetime as dt
import time
import serial
from multiprocessing import Process, Value, Queue

class Ftp:
    def __init__(self, size, timeout, host, port, user, passwd, directory, filename, mode):
        self.size = size
        self.host = host
        self.port = port
        self.user = user
        self.passwd = passwd
        self.timeout = timeout
        self.directory = directory
        self.filename = filename
        self.mode = mode
        self.ftpclient = ftplib.FTP()

    def ProcessChunk(self, chunk):
        self.size.value += len(chunk)

    def RunTest(self):
        try:
            self.ftpclient.connect(self.host, self.port, self.timeout)
        except Exception as e:
            print("Could not connect to {0}, port {1}, timeout {2} {3}\n".format(self.host, self.port, self.timeout, e))
            sys.exit(-1)
        try:
            self.ftpclient.login(self.user, self.passwd)
        except Exception as e:
            print("Could not log into ftp with user {0}, passwd {1} {2}\n".format(self.user, self.passwd, e))
            sys.exit(-1)

        if self.mode == "Upload":
            self.ftpclient.cwd(self.directory)

        self.ftpclient.set_pasv(True)
        start = dt.datetime.now()

        try:
            if self.mode == "Upload":
                self.ftpclient.storbinary('STOR ' + self.filename, open(self.filename, 'rb'), blocksize=1024, callback=self.ProcessChunk)
            elif self.mode == "Download":
                self.ftpclient.retrbinary('RETR ' + self.directory + "/" + self.filename, callback=self.ProcessChunk) # blocksize=1024*16
        except Exception as e:
            print("Could not {0} file {1} in {2}. {3}\n".format(self.mode, self.filename, self.directory, e))

        stop = dt.datetime.now()
        diff = stop-start

        self.ftpclient.quit

        self.throughput = self.size / diff.total_seconds()
        self.throughput = ((self.throughput/1e6) * 8) #Mbit/s
        print("Throughput: %.2f Mbit/s" % self.throughput)

        #print("Total bytes %s: %d" % (mode, size.value))
        #print("Total duration: %.2f [s]" % (diff.total_seconds()))
        #throughput_Bs = size.value / diff.total_seconds()
        #print("Throughput: %.2f Mbit/s" % ((throughput_Bs/1e6) * 8))



def main():
    parser = argparse.ArgumentParser(description='FTP bandwidth measurements.')
    # parser.add_argument('-l','--log', type=str, default='DEBUG', help='Set logging level (DEBUG|INFO|WARN|CRIT)')
    parser.add_argument('-u','--upload', type=str, help='Activate uplink and specific directory to copy things')
    parser.add_argument('-i','--interval', type=float, default=1.0, help='Measurement interval duration')
    parser.add_argument('-t','--timeout', type=int, default=10, help='FTP timeout in seconds')
    parser.add_argument('-f','--filename', type=str, default='ftp_data.txt', help='File to write output')
    parser.add_argument('-d','--data', type=str, default='/mirror/ubuntu-cdimage/13.10/ubuntu-13.10-server-amd64+mac.iso', help='Data to download/upload to create traffic')
    parser.add_argument('--host',type=str, default='mirror.switch.ch',  help='Ftp host')
    parser.add_argument('-n','--netrc', type=str, default='sample.netrc', help='Location of netrc file to use')
    parser.add_argument('-s','--serial', type=str, help='Serial port to use')
    parser.add_argument('-p','--processes', type=int, default=1, help='Number of processes to start')
    args = parser.parse_args()

    ftp = Ftp(size,args.interval,args.timeout,args.host,args.netrc,args.data,args.upload)

    # if not isinstance(getattr(logging,args.log.upper()), int):
    #     raise ValueError('Invalid log level: %s' % args.log)

    # # Setup logging: see http://docs.python.org/dev/howto/logging.html#logging-basic-tutorial
    # logging.basicConfig(level=args.log.upper())

    # Shared counter for the size calculation
    size = Value('i', 0)
    triggerCounter = Value('i', 0)
    # Shared queue for throughput measurements
    q = Queue()
    # Run stuff
    p = []
    for k in range(0,args.processes):
        print('Setup FTP download %d' % k)
        ftp = Ftp(size,args.interval,args.timeout,args.host,args.netrc,args.data,args.upload)
        p.append(Process(target=ftp.Run, args=(q,args.filename)))
        p[k].start()
        # p1 = Process(target=ftp.Run, args=(q,args.filename))
        # p1.start()
    if args.serial:
        p2 = Process(target=triggerCount, args=(triggerCounter,args.serial))
        p2.start()

    try:
        ftp.printSize(q,size,triggerCounter,args.filename,args.upload,args.processes)
    except KeyboardInterrupt as e:
        pass

    #p2 = Process(target=ftp.printSize, args=(q,))
    #p2.start()

    #try:
        #p1.join()
        #p2.join()
    #except KeyboardInterrupt as e:
        # sys.stderr.write('Let\'s stop\n')
    #    pass

if __name__ == "__main__":
    main()