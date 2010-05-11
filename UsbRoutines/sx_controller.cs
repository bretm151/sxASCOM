﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using WinUsbDemo;

namespace sx
{
    // This class defines the the functions to access the resources which exist on the 
    // USB controller board.  If there are two cameras conneted (a main camera and a guide
    // camera), there is still only one of these devices present.
    //
    // Locking:
    //    The controller object is used as the lock.  It is necessary to lock the interface when
    // a transaction is occuring.  There are three types of transactions:
    // - simple command writes that return no data
    // - operations that require a write to begin, and which return data that is collected with read
    // - operations that require a write to begin and another write to end (STAR2K guiding is the only one)

    public class Controller
        : sxBase
    {
        // Variables
        private USBInterface iface;
        private SX_CCD_PARAMS ccdParms;

        // Properties

        public bool Connected
        {
            get;
            private set;
        }

        public UInt32 firmwareVersion
        {
            get;
            private set;
        }

        public Byte numSerialPorts
        {
            get {return ccdParms.num_serial_ports;}
        }

        public Controller()
        {
            Connected = false;

            try
            {
                iface = new USBInterface();

                reset();
                firmwareVersion = getVersion();
                getParams(ref ccdParms);
                Connected = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal void buildCommandBlock(out SX_CMD_BLOCK block, Byte cmd_type, Byte cmd, UInt16 cmd_value, UInt16 index, UInt16 cmd_length)
        {
            block.cmd_type = cmd_type;
            block.cmd = cmd;
            block.cmd_value = cmd_value;
            block.index = index;
            block.cmd_length = cmd_length;
            Log.Write(String.Format("buildCommandBlock(): type=0x{0:x2} cmd=0x{1:x2} cmd_value=0x{2:x4} index=0x{3:x4} cmd_length=0x{4:x4}\n", cmd_type, cmd, cmd_value, index, cmd_length));
        }

        
        internal void Write(SX_CMD_BLOCK block, Object data, out Int32 numBytesWritten)
        {
            lock (this)
            {
                Log.Write("Write has locked\n");
                iface.Write(block, data, out numBytesWritten);
            }
            Log.Write("Write has unlocked\n");
        }
           
        internal void Write(SX_CMD_BLOCK block, out Int32 numBytesWritten)
        {
            Write(block, null, out numBytesWritten);
        }

        internal object Read(Type returnType, Int32 numBytesToRead, out Int32 numBytesRead)
        {
            object oReturn;

            lock (this)
            {
                Log.Write("Read has locked\n");
                oReturn = iface.Read(returnType, numBytesToRead, out numBytesRead);
            }
            Log.Write("Read has unlocked\n"); 
            return oReturn;
        }

        internal void Read(out byte[] bytes, Int32 numBytes, out Int32 numBytesRead)
        {
            bytes = (byte[])Read(typeof(System.Byte[]), numBytes, out numBytesRead);
        }

        internal void Read(out string s, Int32 numBytesToRead, out Int32 numBytesRead)
        {
            s = (string)Read(typeof(System.String), numBytesToRead, out numBytesRead);
        }

        internal object Read(Type returnType, out Int32 numBytesRead)
        {
            return Read(returnType, Marshal.SizeOf(returnType), out numBytesRead);
        }

        public void echo(string s)
        {
            SX_CMD_BLOCK cmdBlock;
            Int32 numBytesWritten, numBytesRead;
            string s2;

            buildCommandBlock(out cmdBlock, SX_CMD_TYPE_PARMS, SX_CMD_ECHO, 0, 0, (UInt16)s.Length);

            lock (this)
            {

                Write(cmdBlock, s, out numBytesWritten);

                Read(out s2, s.Length, out numBytesRead);
            }

            if (s2 != s)
            {
                throw new System.IO.IOException(String.Format("Echo: s2 != s ({0} != {1})", s2, s));
            }
        }

         public void reset()
        {
            SX_CMD_BLOCK cmdBlock;
            Int32 numBytesWritten;

            buildCommandBlock(out cmdBlock, SX_CMD_TYPE_PARMS, SX_CMD_RESET, 0, 0, 0);

            Write(cmdBlock, out numBytesWritten);
        }

        public uint getVersion()
        {
            SX_CMD_BLOCK cmdBlock;
            Int32 numBytesWritten, numBytesRead;
            byte[] bytes;
            UInt32 ver = 0;

            buildCommandBlock(out cmdBlock, SX_CMD_TYPE_PARMS, SX_CMD_GET_FIRMWARE_VERSION, 0, 0, 0);

            lock (this)
            {
                Log.Write("getVersion has locked\n");
                Write(cmdBlock, out numBytesWritten);

                Read(out bytes, 4, out numBytesRead);
            }
            Log.Write("getVersion has unlocked\n");

            ver = System.BitConverter.ToUInt32(bytes, 0);

            return ver;
        }

        void getParams(ref SX_CCD_PARAMS parms)
        {
            SX_CMD_BLOCK cmdBlock;
            Int32 numBytesWritten, numBytesRead;

            buildCommandBlock(out cmdBlock, SX_CMD_TYPE_READ, SX_CMD_GET_CCD_PARMS, 0, 0, 0);

            Write(cmdBlock, out numBytesWritten);

            parms = (SX_CCD_PARAMS)Read(typeof(SX_CCD_PARAMS), out numBytesRead);
        }

        public int getTimer()
        {
            SX_CMD_BLOCK cmdBlock;
            Int32 numBytesWritten, numBytesRead;
            byte[] bytes;
            Int32 ms = 0;

            buildCommandBlock(out cmdBlock, SX_CMD_TYPE_PARMS, SX_CMD_GET_TIMER, 0, 0, 0);

            lock (this)
            {
                Log.Write("getTimer has locked\n");
                Write(cmdBlock, out numBytesWritten);

                Read(out bytes, 4, out numBytesRead);
            }
            Log.Write("getTimer has unlocked\n");

            ms = System.BitConverter.ToInt32(bytes, 0);

            Log.Write("Timer 0 = " + ms + "\n");

            return ms;
        }

        public void setTimer(UInt32 ms)
        {
            SX_CMD_BLOCK cmdBlock;
            Int32 numBytesWritten;

            buildCommandBlock(out cmdBlock, SX_CMD_TYPE_PARMS, SX_CMD_SET_TIMER, 0, 0, (UInt16)Marshal.SizeOf(ms));
            Write(cmdBlock, ms, out numBytesWritten);
        }

        public Boolean hasGuideCamera
        {
            get { return (ccdParms.extra_capabilities & INTEGRATED_GUIDER_CCD) == INTEGRATED_GUIDER_CCD; }
        }

        public Boolean hasGuidePort
        {
            get { return (ccdParms.extra_capabilities & STAR2000_PORT) == STAR2000_PORT; }
        }

        public void guide(UInt16 direction, int durationMS)
        {
            SX_CMD_BLOCK cmdBlock;
            Int32 numBytesWritten;
            DateTime guideStart = DateTime.Now;

            if (!hasGuidePort)
            {
                throw new System.Exception("Guide request but no guide port");
            }

            buildCommandBlock(out cmdBlock, SX_CMD_TYPE_PARMS, SX_CMD_SET_STAR2K, direction, 0, 0);

            sx.Log.Write(String.Format("guide({0}, {1}) begins\n", direction, durationMS));

            lock (this)
            {
                try
                {
                    TimeSpan desiredGuideDuration = TimeSpan.FromMilliseconds(durationMS);
                    DateTime guideEnd = guideStart + desiredGuideDuration;

                    Write(cmdBlock, out numBytesWritten);

                    // We sleep for most of the guide time, then spin for the last little bit
                    // because this helps us end closer to the right time

                    sx.Log.Write("guide(): about to begin loop\n");

                    for (TimeSpan remainingGuideTime = desiredGuideDuration;
                        remainingGuideTime.TotalMilliseconds > 0;
                        remainingGuideTime = guideEnd - DateTime.Now)
                    {
                        if (remainingGuideTime.TotalMilliseconds > 75)
                        {
                            // sleep in small chunks so that we are responsive to abort and stop requests
                            //sx.Log.Write("Before sleep, remaining exposure=" + remainingGuideTime.TotalSeconds + "\n");
                            Thread.Sleep(50);
                        }
                    }
                }
                finally
                {
                    buildCommandBlock(out cmdBlock, SX_CMD_TYPE_PARMS, SX_CMD_SET_STAR2K, SX_STAR2K_STOP, 0, 0);
                    Write(cmdBlock, out numBytesWritten);
                }
            }

            sx.Log.Write(String.Format("guide(): delay ends, actualExposureLength={0:F4}\n", (DateTime.Now - guideStart).TotalMilliseconds));
        }
    }
}
