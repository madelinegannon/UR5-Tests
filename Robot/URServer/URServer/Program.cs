using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SharpOSC;

namespace URServer
{
    class Program
    {
        public static float[] pose = { 0, 0, 0, 0, 0, 0 };
        public static float[] joints = { 0, 0, 0, 0, 0, 0 };
        public static float force = 0;
        public static float tcpForce = 0;

        public static string OSCinput = "";

        static void Main(string[] args)
        {

            // The IP address of the server (the PC on which this program is running)
            string sHostIpAddress = "128.2.102.159";
            // Standard port number
            int nPort = 21;

            Console.WriteLine("Opening IP Address: " + sHostIpAddress);
            IPAddress ipAddress = IPAddress.Parse(sHostIpAddress);        // Create the IP address
            Console.WriteLine("Starting to listen on port: " + nPort);
            TcpListener tcpListener = new TcpListener(ipAddress, nPort);  // Create the tcp Listener
            tcpListener.Start();                                          // Start listening

            // Set up OSC
            // Let the user set the IP Address and Port for sending out OSC info
            Console.WriteLine("Enter IP Address of Remote Computer receiving data: ");
            String oscIP = Console.ReadLine();
            Console.WriteLine("Enter PORT of Remote Computer receiving data: ");
            String oscPORT = Console.ReadLine();

            // offer a use a default IP / PORT
            if (oscIP.Length == 0 && oscPORT.Length == 0)
            {
                oscIP = "128.237.191.176";
                oscPORT = "55555";
                Console.WriteLine("...Set to default IP and PORT: " + oscIP + ":" + oscPORT);

            }

            // Setup OSC sender to remote computer
            var sender = new SharpOSC.UDPSender(oscIP, Int32.Parse(oscPORT));// "10.140.76.75", 55555);

            Console.WriteLine();
            Console.WriteLine("Ready to Stream!");
            Console.WriteLine("+ + + + + + + + + + + + ");

            // Setup OSC listener
            HandleOscPacket callback = delegate(OscPacket packet)
            {
                var oscMsg = (OscMessage)packet;

                String addr = oscMsg.Address;
                if (addr.Equals("/pose"))
                    OSCinput = "getTcpPose";
                else if (addr.Equals("/target"))
                    OSCinput = "getTargetPose";
                else if (addr.Equals("/joints"))
                    OSCinput = "getJointPose";
                else if (addr.Equals("/force"))
                    OSCinput = "getForce";
                else if (addr.Equals("/tcpForce"))
                    OSCinput = "getTcpForce";
                else if (addr.Equals("/movej"))
                {
                    OSCinput = "movej";
                    // print out the incoming osc message
                    //Console.WriteLine(addr);
                    for (int i = 0; i < oscMsg.Arguments.Count; i++)
                    {
                        pose[i] = float.Parse(oscMsg.Arguments[i].ToString());
                    //    Console.WriteLine("     " + pose[i]);
                    }
                    //Console.WriteLine("");
                }

            };

            var listener = new UDPListener(12000, callback);


            // Keep on listening forever
            while (true)
            {
                TcpClient tcpClient = tcpListener.AcceptTcpClient();        // Accept the client
                Console.WriteLine("Accepted new client");
                NetworkStream stream = tcpClient.GetStream();               // Open the network stream
                while (tcpClient.Client.Connected)
                {

                    // Console.Write("Send msg to Robot OVER OSC: ");
                    // string input = Console.ReadLine();

                    // if we've gotten an OSC message
                    if (!OSCinput.Equals(""))
                    {
                        sendMsg(stream, OSCinput);

                        string msg = "";
                        while (msg.Equals(""))
                        {
                            msg = receiveMsg(stream, tcpClient, sender);
                        }

                        // if we are sending an additional command
                        if (OSCinput.Equals("movej"))
                        {
                           
                            string poseMsg = "(";
                            for (int i = 0; i < 6; i++)
                            {
                                poseMsg += pose[i];
                                if (i < 5)
                                    poseMsg += ", ";
                                else
                                    poseMsg += ")";
                            }
                            Console.WriteLine(poseMsg);
                            sendMsg(stream, poseMsg);
                        }

                        // reset the OSC input
                        OSCinput = "";
                    }


                    




                }
            }

            //listener.Close();
        }

        public static void sendMsg(NetworkStream stream, string msg)
        {
            Console.WriteLine("Sending message to robot: " + msg);
            // Convert the point into a byte array
            byte[] arrayBytesAnswer = ASCIIEncoding.ASCII.GetBytes(msg);
            // Send the byte array to the client
            stream.Write(arrayBytesAnswer, 0, arrayBytesAnswer.Length);
        }

        public static string receiveMsg(NetworkStream stream, TcpClient tcpClient, UDPSender sender)
        {
            string msgReceived = "";

            // Create a byte array for the available bytes
            byte[] arrayBytesRequest = new byte[tcpClient.Available];
            // Read the bytes from the stream
            int nRead = stream.Read(arrayBytesRequest, 0, arrayBytesRequest.Length);

            if (nRead > 0)
            {
                // Convert the byte array into a string
                string sMsgRequest = ASCIIEncoding.ASCII.GetString(arrayBytesRequest);
                Console.WriteLine("Msg from Robot: " + sMsgRequest);
                string sMsgAnswer = string.Empty;

                // set the key for the key/value pairs
                string key = "";
                if (sMsgRequest.StartsWith("world"))
                    key = "hello";
                else if (sMsgRequest.StartsWith("p"))
                    key = "target";
                else if (sMsgRequest.StartsWith("["))
                    key = "joints";
                else if (sMsgRequest.StartsWith("SET force"))
                    key = "force";
                else if (sMsgRequest.StartsWith("SET tcp force"))
                    key = "tcp force";

                // set the value for the key/value pairs
                if (key.Equals("hello"))
                {
                    Console.WriteLine("Going to home position: hello " + sMsgRequest + "!");
                    // go to home position

                    msgReceived = "going home";
                }
                else if (key.Equals("target"))
                {
                    int start = 2;
                    msgReceived = sMsgRequest.Substring(2, sMsgRequest.Length - 1 - start);

                    string[] coords = msgReceived.Split(',');
                    if (coords.Length == 6)
                    {
                        float x = float.Parse(coords[0]);
                        float y = float.Parse(coords[1]);
                        float z = float.Parse(coords[2]);
                        float rx = float.Parse(coords[3]);
                        float ry = float.Parse(coords[4]);
                        float rz = float.Parse(coords[5]);

                        pose[0] = x;
                        pose[1] = y;
                        pose[2] = z;
                        pose[3] = rx;
                        pose[4] = ry;
                        pose[5] = rz;

                        // send pose over OSC
                        String[] msg = new String[6];
                        msg[0] = "x: " + pose[0];
                        msg[1] = "y: " + pose[1];
                        msg[2] = "z: " + pose[2];
                        msg[3] = "rx: " + pose[3];
                        msg[4] = "ry: " + pose[4];
                        msg[5] = "rz: " + pose[5];
                        var message = new SharpOSC.OscMessage("/pose", msg);
                        sender.Send(message);

                        Console.WriteLine("Robot Pose:  [" + x + ", " + y + ", " + z + ", " + rx + ", " + ry + ", " + rz + "]");
                    }
                    else
                    {
                        msgReceived = "WTF: " + sMsgRequest;
                    }
                }
                else if (key.Equals("joints"))
                {
                    Console.WriteLine("Parsing joint pose: " + sMsgRequest);
                    int start = 1;
                    msgReceived = sMsgRequest.Substring(start, sMsgRequest.Length - 1 - start);

                    string[] coords = msgReceived.Split(',');
                    if (coords.Length == 6)
                    {
                        float x = float.Parse(coords[0]);
                        float y = float.Parse(coords[1]);
                        float z = float.Parse(coords[2]);
                        float rx = float.Parse(coords[3]);
                        float ry = float.Parse(coords[4]);
                        float rz = float.Parse(coords[5]);

                        joints[0] = x;
                        joints[1] = y;
                        joints[2] = z;
                        joints[3] = rx;
                        joints[4] = ry;
                        joints[5] = rz;

                        // send joints over OSC
                        String[] msg = new String[6];
                        msg[0] = "base: " + pose[0];
                        msg[1] = "shoulder: " + pose[1];
                        msg[2] = "elbow: " + pose[2];
                        msg[3] = "wrist 1: " + pose[3];
                        msg[4] = "wrist 2: " + pose[4];
                        msg[5] = "wrist 3: " + pose[5];
                        var message = new SharpOSC.OscMessage("/joints", msg);
                        sender.Send(message);

                        Console.WriteLine("Robot Joint Positions: [" + x + ", " + y + ", " + z + ", " + rx + ", " + ry + ", " + rz + "]");
                    }
                    else
                    {
                        msgReceived = "WTF: " + sMsgRequest;
                    }
                }
                else if (key.Equals("force"))
                {
                    Console.WriteLine("Parsing pose: " + sMsgRequest);
                    int start = key.Length + 5;
                    msgReceived = sMsgRequest.Substring(start, sMsgRequest.Length - 1 - start);

                    force = float.Parse(msgReceived);

                    // send force over OSC
                    var message = new SharpOSC.OscMessage("/force", force);
                    sender.Send(message);

                    Console.WriteLine("Robot Force: {" + force + "}");
                }
                else if (key.Equals("tcp force"))
                {
                    Console.WriteLine("Parsing pose: " + sMsgRequest);
                    int start = key.Length + 5;
                    msgReceived = sMsgRequest.Substring(start, sMsgRequest.Length - 1 - start);

                    tcpForce = float.Parse(msgReceived);

                    // send TCP force over OSC
                    var message = new SharpOSC.OscMessage("/TCPforce", tcpForce);
                    sender.Send(message);

                    Console.WriteLine("Robot TCP Force: {" + tcpForce + "}");
                }
                else
                {
                    // unkown command
                    msgReceived = sMsgRequest;
                    ///sendMsg(stream, "unknown command sent to robot: " + sMsgRequest);
                }


            }
            else if (tcpClient.Available == 0)
            {
                Console.WriteLine("Client closed the connection.");
                // No bytes read, and no bytes available, the client is closed.
                stream.Close();
            }

            return msgReceived;
        }



    }



}
