using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using NatNetML;

using SharpOSC;



namespace NatNetOSC
{
    class Program
    {

        // [NatNet] Our NatNet object
        private static NatNetML.NatNetClientML m_NatNet;

        // [NatNet] Our NatNet Frame of Data object
        private static NatNetML.FrameOfMocapData m_FrameOfData = new NatNetML.FrameOfMocapData();

        // [NatNet] Description of the Active Model List from the server (e.g. Motive)
        static NatNetML.ServerDescription desc = new NatNetML.ServerDescription();

        // [NatNet] Queue holding our incoming mocap frames the NatNet server (e.g. Motive)
        private static Queue<NatNetML.FrameOfMocapData> m_FrameQueue = new Queue<NatNetML.FrameOfMocapData>();

        // frame timing information
        static double m_fLastFrameTimestamp = 0.0f;
        static float m_fCurrentMocapFrameTimestamp = 0.0f;
        static float m_fFirstMocapFrameTimestamp = 0.0f;
        static QueryPerfCounter m_FramePeriodTimer = new QueryPerfCounter();
        static QueryPerfCounter m_UIUpdateTimer = new QueryPerfCounter();

        // server information
        static double m_ServerFramerate = 1.0f;
        static float m_ServerToMillimeters = 1.0f;
        static int m_UpAxis = 1;   // 0=x, 1=y, 2=z (Y default)
        static int mAnalogSamplesPerMocpaFrame = 0;
        static int mDroppedFrames = 0;
        static int mLastFrame = 0;

        private static object syncLock = new object();
        private bool needMarkerListUpdate = false;
        private bool mPaused = false;

        // rigid body stuff
        static Hashtable htRigidBodies = new Hashtable();
        static List<RigidBody> mRigidBodies = new List<RigidBody>();


        // OSC info
        private static string oscIP = "128.237.161.161";
        private static string oscPORT = "55555";
        private static SharpOSC.UDPSender sender;


        static void Main(string[] args)
        {
            Console.WriteLine("Connecting to Motive: ");

            // set up NatNet client and connect
            CreateClient(0); // 0 is MultiCast, 1 is Unicast
            Connect();

            while (true)
            {
                Update();
            }

        }

        /// <summary>
        /// Create a new NatNet client, which manages all communication with the NatNet server (e.g. Motive)
        /// </summary>
        /// <param name="iConnectionType">0 = Multicast, 1 = Unicast</param>
        /// <returns></returns>
        private static int CreateClient(int iConnectionType)
        {
            // release any previous instance
            if (m_NatNet != null)
            {
                m_NatNet.Uninitialize();
            }

            // [NatNet] create a new NatNet instance
            m_NatNet = new NatNetML.NatNetClientML(iConnectionType);

            // [NatNet] set a "Frame Ready" callback function (event handler) handler that will be
            // called by NatNet when NatNet receives a frame of data from the server application
            m_NatNet.OnFrameReady += new NatNetML.FrameReadyEventHandler(m_NatNet_OnFrameReady);


            // [NatNet] print version info
            int[] ver = new int[4];
            ver = m_NatNet.NatNetVersion();
            String strVersion = String.Format("NatNet Version : {0}.{1}.{2}.{3}", ver[0], ver[1], ver[2], ver[3]);
            Console.WriteLine(strVersion);

            return 0;
        }

        /// <summary>
        /// Connect to a NatNet server (e.g. Motive)
        /// </summary>
        private static void Connect()
        {
            // [NatNet] connect to a NatNet server
            int returnCode = 0;
            string strLocalIP  = "127.0.0.1";
            string strServerIP = "127.0.0.1";
            returnCode = m_NatNet.Initialize(strLocalIP, strServerIP);
            if (returnCode == 0)
            {
                Console.WriteLine("Initialization Succeeded.");
            }
            else
            {
                Console.WriteLine("Error Initializing.");            
            }


            // [NatNet] validate the connection
            returnCode = m_NatNet.GetServerDescription(desc);
            if (returnCode == 0)
            {
                Console.WriteLine("Connection Succeeded.");
                Console.WriteLine("   Server App Name: " + desc.HostApp);
                Console.WriteLine(String.Format("   Server App Version: {0}.{1}.{2}.{3}", desc.HostAppVersion[0], desc.HostAppVersion[1], desc.HostAppVersion[2], desc.HostAppVersion[3]));
                Console.WriteLine(String.Format("   Server NatNet Version: {0}.{1}.{2}.{3}", desc.NatNetVersion[0], desc.NatNetVersion[1], desc.NatNetVersion[2], desc.NatNetVersion[3]));
                

                // Setup OSC sender to remote computer
                sender = new SharpOSC.UDPSender(oscIP, Int32.Parse(oscPORT));
                Console.WriteLine("set up OSC sender at " + oscIP + " on port " + oscPORT);

                // Set up OSC
                // Let the user set the IP Address and Port for sending out OSC info
                Console.WriteLine("Enter IP Address of Remote Computer receiving data: ");
                String customOscIP = Console.ReadLine();
                Console.WriteLine("Enter PORT of Remote Computer receiving data: ");
                String customOscPORT = Console.ReadLine();

                // offer a use a default IP / PORT
                if (customOscIP.Length == 0 && customOscPORT.Length == 0)
                {
                    oscIP = "128.237.161.161";
                    oscPORT = "55555";
                    Console.WriteLine("...Using default IP and PORT: " + oscIP + ":" + oscPORT);
                }
                else
                {
                    oscIP = customOscIP;
                    oscPORT = customOscPORT;
                    Console.WriteLine("...Setting custom IP and PORT: " + oscIP + ":" + oscPORT);
                }

                // Tracking Tools and Motive report in meters - lets convert to millimeters
                if (desc.HostApp.Contains("TrackingTools") || desc.HostApp.Contains("Motive"))
                    m_ServerToMillimeters = 1000.0f;


                // [NatNet] [optional] Query mocap server for the current camera framerate
                int nBytes = 0;
                byte[] response = new byte[10000];
                int rc;
                rc = m_NatNet.SendMessageAndWait("FrameRate", out response, out nBytes);
                if (rc == 0)
                {
                    try
                    {
                        m_ServerFramerate = BitConverter.ToSingle(response, 0);
                        Console.WriteLine(String.Format("   Camera Framerate: {0}", m_ServerFramerate));
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                // [NatNet] [optional] Query mocap server for the current analog framerate
                rc = m_NatNet.SendMessageAndWait("AnalogSamplesPerMocapFrame", out response, out nBytes);
                if (rc == 0)
                {
                    try
                    {
                        mAnalogSamplesPerMocpaFrame = BitConverter.ToInt32(response, 0);
                        Console.WriteLine(String.Format("   Analog Samples Per Camera Frame: {0}", mAnalogSamplesPerMocpaFrame));
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }


                // [NatNet] [optional] Query mocap server for the current up axis
                rc = m_NatNet.SendMessageAndWait("UpAxis", out response, out nBytes);
                if (rc == 0)
                {
                    m_UpAxis = BitConverter.ToInt32(response, 0);
                }


                m_fCurrentMocapFrameTimestamp = 0.0f;
                m_fFirstMocapFrameTimestamp = 0.0f;
                mDroppedFrames = 0;
            }
            else
            {
                Console.WriteLine("Error Connecting.");
            }

        }

       

       static private void Update()
        {
            m_UIUpdateTimer.Stop();
            double interframeDuration = m_UIUpdateTimer.Duration();

            QueryPerfCounter uiIntraFrameTimer = new QueryPerfCounter();
            uiIntraFrameTimer.Start();

            // the frame queue is a shared resource with the FrameOfMocap delivery thread, so lock it while reading
            // note this can block the frame delivery thread.  In a production application frame queue management would be optimized.
            lock (syncLock)
            {
                while (m_FrameQueue.Count > 0)
                {
                    m_FrameOfData = m_FrameQueue.Dequeue();

                    if (m_FrameQueue.Count > 0)
                        continue;

                    if (m_FrameOfData != null)
                    {
                        // for servers that only use timestamps, not frame numbers, calculate a 
                        // frame number from the time delta between frames
                        if (desc.HostApp.Contains("TrackingTools"))
                        {
                            m_fCurrentMocapFrameTimestamp = m_FrameOfData.fLatency;
                            if (m_fCurrentMocapFrameTimestamp == m_fLastFrameTimestamp)
                            {
                                continue;
                            }
                            if (m_fFirstMocapFrameTimestamp == 0.0f)
                            {
                                m_fFirstMocapFrameTimestamp = m_fCurrentMocapFrameTimestamp;
                            }
                            m_FrameOfData.iFrame = (int)((m_fCurrentMocapFrameTimestamp - m_fFirstMocapFrameTimestamp) * m_ServerFramerate);

                        }

                        // update the mocap data
                        UpdateData();
                    }
                }

                uiIntraFrameTimer.Stop();
                double uiIntraFrameDuration = uiIntraFrameTimer.Duration();
                m_UIUpdateTimer.Start();

            }
        }


        /// <summary>
        /// Update the spreadsheet.  
        /// Note: This refresh is quite slow and provided here only as a complete example. 
        /// In a production setting this would be optimized.
        /// </summary>
        static private void UpdateData()
        {
            // update MarkerSet data
            for (int i = 0; i < m_FrameOfData.nMarkerSets; i++)
            {
                NatNetML.MarkerSetData ms = m_FrameOfData.MarkerSets[i];
                //for (int j = 0; j < ms.nMarkers; j++)
                //{
                    
                //}
            }

            // update RigidBody data
            Console.WriteLine("RigidBody count: " + m_FrameOfData.nRigidBodies);
            for (int i = 0; i < m_FrameOfData.nRigidBodies; i++)
            {
                NatNetML.RigidBodyData rb = m_FrameOfData.RigidBodies[i];

                // update RigidBody data

                bool tracked = rb.Tracked;
                if (!tracked)
                {
                    //Console.WriteLine("RigidBody not tracked in this frame.");
                }


                ////////////////////////////////////////////////////////////
                // poop 

                // send rigid body & markers over OSC           
                // may not be fast enough?

                String[] msg = new String[8];
                msg[0] = "Rigid Body: " + rb.ID;
                msg[1] = "x: " + rb.x * 1000;
                msg[2] = "y: " + rb.y * 1000;
                msg[3] = "z: " + rb.z * 1000;
                msg[4] = "qx: " + rb.qx;
                msg[5] = "qy: " + rb.qy;
                msg[6] = "qz: " + rb.qz;
                msg[7] = "qz: " + rb.qw;

                var message = new SharpOSC.OscMessage("/rigid body", msg);
                sender.Send(message);
                
                // print out to console
                for (int j = 0; j < 8; j++)
                {
                    if (j == 0)                 
                        Console.WriteLine(msg[j]+":");           
                    else 
                        Console.WriteLine("     "+msg[j]);
                }

                int count = rb.nMarkers;
                msg = new String[count];
                for (int j = 0; j < count; j++)
                {
                    if (rb.Markers[j] != null)
                        msg[j] = "Rigid Body " + rb.ID + ", Marker " + rb.Markers[j].ID + ": {" + rb.Markers[j].x + "," + rb.Markers[j].y + "," + rb.Markers[j].z + "}";
                }

                sender.Send(new SharpOSC.OscMessage("/marker", msg));

                // print out to console
                for (int j = 0; j < count; j++)
                {
                    if (j == 0)
                        Console.WriteLine(msg[j] + ":");
                    else
                        Console.WriteLine("     " + msg[j]);
                }
                

                ////////////////////////////////////////////////////////////


                // update Marker data associated with this rigid body
                for (int j = 0; j < rb.nMarkers; j++)
                {
                    if (rb.Markers[j].ID != -1)
                    {
                        RigidBody rbDef = FindRB(rb.ID);
                        if (rbDef != null)
                        {
                            String strUniqueName = rbDef.Name + "-" + rb.Markers[j].ID.ToString();
                            int keyMarker = strUniqueName.GetHashCode();

                            NatNetML.Marker m = rb.Markers[j];

                        }
                    }
                }

            }
            

            // update Skeleton data
            //for (int i = 0; i < m_FrameOfData.nSkeletons; i++)
            //{
            //    NatNetML.SkeletonData sk = m_FrameOfData.Skeletons[i];
            //    for (int j = 0; j < sk.nRigidBodies; j++)
            //    {
            //        // note : skeleton rigid body ids are of the form:
            //        // parent skeleton ID   : high word (upper 16 bits of int)
            //        // rigid body id        : low word  (lower 16 bits of int)
            //        NatNetML.RigidBodyData rb = sk.RigidBodies[j];
            //        int skeletonID = HighWord(rb.ID);
            //        int rigidBodyID = LowWord(rb.ID);
            //        int uniqueID = skeletonID * 1000 + rigidBodyID;
            //        int key = uniqueID.GetHashCode();
            //        if (htRigidBodies.ContainsKey(key))
            //        {
            //            int rowIndex = (int)htRigidBodies[key];
            //            if (rowIndex >= 0)
            //            {
            //                dataGridView1.Rows[rowIndex].Cells[1].Value = rb.x;
            //                dataGridView1.Rows[rowIndex].Cells[2].Value = rb.y;
            //                dataGridView1.Rows[rowIndex].Cells[3].Value = rb.z;

            //                // Convert quaternion to eulers.  Motive coordinate conventions: X(Pitch), Y(Yaw), Z(Roll), Relative, RHS
            //                float[] quat = new float[4] { rb.qx, rb.qy, rb.qz, rb.qw };
            //                float[] eulers = new float[3];
            //                eulers = m_NatNet.QuatToEuler(quat, (int)NATEulerOrder.NAT_XYZr);
            //                double x = RadiansToDegrees(eulers[0]);     // convert to degrees
            //                double y = RadiansToDegrees(eulers[1]);
            //                double z = RadiansToDegrees(eulers[2]);

            //                dataGridView1.Rows[rowIndex].Cells[4].Value = x;
            //                dataGridView1.Rows[rowIndex].Cells[5].Value = y;
            //                dataGridView1.Rows[rowIndex].Cells[6].Value = z;

            //                // Marker data associated with this rigid body
            //                for (int k = 0; k < rb.nMarkers; k++)
            //                {

            //                }
            //            }
            //        }
            //    }
            //}   // end skeleton update


            //// update labeled markers data
            //// remove previous dynamic marker list
            //// for testing only - this simple approach to grid updating too slow for large marker count use
            //int labeledCount = 0;
            //if (false)
            //{

            //    for (int i = 0; i < m_FrameOfData.nMarkers; i++)
            //    {
            //        NatNetML.Marker m = m_FrameOfData.LabeledMarkers[i];

            //        int modelID, markerID;
            //        m_NatNet.DecodeID(m.ID, out modelID, out markerID);
            //        string name = "Labeled Marker (ModelID: " + modelID + "  MarkerID: " + markerID + ")";
            //        if (modelID == 0)
            //            name = "UnLabeled Marker ( ID: " + markerID + ")";

            //        labeledCount++;
            //    }
            //}

        }

        private static RigidBody FindRB(int id)
        {
            foreach (RigidBody rb in mRigidBodies)
            {

                if (rb.ID == id)
                    return rb;
            }
            return null;
        }
    

        /// <summary>
        /// [NatNet] m_NatNet_OnFrameReady will be called when a frame of Mocap
        /// data has is received from the server application.
        ///
        /// Note: This callback is on the network service thread, so it is
        /// important to return from this function quickly as possible 
        /// to prevent incoming frames of data from buffering up on the
        /// network socket.
        ///
        /// Note: "data" is a reference structure to the current frame of data.
        /// NatNet re-uses this same instance for each incoming frame, so it should
        /// not be kept (the values contained in "data" will become replaced after
        /// this callback function has exited).
        /// </summary>
        /// <param name="data">The actual frame of mocap data</param>
        /// <param name="client">The NatNet client instance</param>
        static void m_NatNet_OnFrameReady(NatNetML.FrameOfMocapData data, NatNetML.NatNetClientML client)
        {
            double elapsedIntraMS = 0.0f;
            QueryPerfCounter intraTimer = new QueryPerfCounter();
            intraTimer.Start();

            // detect and report and 'measured' frame drop (as measured by client)
            m_FramePeriodTimer.Stop();
            double elapsedMS = m_FramePeriodTimer.Duration();

            ProcessFrameOfData(ref data);

            // report if we are taking too long, which blocks packet receiving, which if long enough would result in socket buffer drop
            intraTimer.Stop();
            elapsedIntraMS = intraTimer.Duration();
            if (elapsedIntraMS > 5.0f)
            {
                Console.WriteLine("Warning : Frame handler taking too long: " + elapsedIntraMS.ToString("F2"));
            }

            m_FramePeriodTimer.Start();

        }

        static void ProcessFrameOfData(ref NatNetML.FrameOfMocapData data)
        {
            // detect and reported any 'reported' frame drop (as reported by server)
            if (m_fLastFrameTimestamp != 0.0f)
            {
                double framePeriod = 1.0f / m_ServerFramerate;
                double thisPeriod = data.fTimestamp - m_fLastFrameTimestamp;
                double fudgeFactor = 0.002f; // 2 ms
                if ((thisPeriod - framePeriod) > fudgeFactor)
                {
                    //Console.WriteLine("Frame Drop: ( ThisTS: " + data.fTimestamp.ToString("F3") + "  LastTS: " + m_fLastFrameTimestamp.ToString("F3") + " )");
                    mDroppedFrames++;
                }
            }

            // check and report frame drop (frame id based)
            if (mLastFrame != 0)
            {
                if ((data.iFrame - mLastFrame) != 1)
                {
                    //Console.WriteLine("Frame Drop: ( ThisFrame: " + data.iFrame.ToString() + "  LastFrame: " + mLastFrame.ToString() + " )");
                    //mDroppedFrames++;
                }
            }



            // [NatNet] Add the incoming frame of mocap data to our frame queue,  
            // Note: the frame queue is a shared resource with the UI thread, so lock it while writing
            lock (syncLock)
            {
                // [optional] clear the frame queue before adding a new frame
                m_FrameQueue.Clear();
                FrameOfMocapData deepCopy = new FrameOfMocapData(data);
                m_FrameQueue.Enqueue(deepCopy);
            }

            mLastFrame = data.iFrame;
            m_fLastFrameTimestamp = data.fTimestamp;

        }




        // Wrapper class for the windows high performance timer QueryPerfCounter
        // ( adapted from MSDN https://msdn.microsoft.com/en-us/library/ff650674.aspx )
        public class QueryPerfCounter
        {
            [DllImport("KERNEL32")]
            private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

            [DllImport("Kernel32.dll")]
            private static extern bool QueryPerformanceFrequency(out long lpFrequency);

            private long start;
            private long stop;
            private long frequency;
            Decimal multiplier = new Decimal(1.0e9);

            public QueryPerfCounter()
            {
                if (QueryPerformanceFrequency(out frequency) == false)
                {
                    // Frequency not supported
                    throw new Win32Exception();
                }
            }

            public void Start()
            {
                QueryPerformanceCounter(out start);
            }

            public void Stop()
            {
                QueryPerformanceCounter(out stop);
            }

            // return elapsed time between start and stop, in milliseconds.
            public double Duration()
            {
                double val = ((double)(stop - start) * (double)multiplier) / (double)frequency;
                val = val / 1000000.0f;   // convert to ms
                return val;
            }
        }
    }
}
