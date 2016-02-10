///////////////////////////////////////////////////////////////////////////////
//
// Copyright (C) OMG Plc 2009.
// All rights reserved.  This software is protected by copyright
// law and international treaties.  No part of this software / document
// may be reproduced or distributed in any form or by any means,
// whether transiently or incidentally to some other use of this software,
// without the written permission of the copyright owner.
//
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;
using ViconDataStreamSDK.DotNET;
using System.Net;
using System.Net.Sockets;
using SharpOSC;


namespace CSharpClient
{
    class Program
    {
        static string Adapt(Direction i_Direction)
        {
            switch (i_Direction)
            {
                case Direction.Forward:
                    return "Forward";
                case Direction.Backward:
                    return "Backward";
                case Direction.Left:
                    return "Left";
                case Direction.Right:
                    return "Right";
                case Direction.Up:
                    return "Up";
                case Direction.Down:
                    return "Down";
                default:
                    return "Unknown";
            }
        }

        static string Adapt(DeviceType i_DeviceType)
        {
            switch (i_DeviceType)
            {
                case DeviceType.ForcePlate:
                    return "ForcePlate";
                case DeviceType.Unknown:
                default:
                    return "Unknown";
            }
        }

        static string Adapt(Unit i_Unit)
        {
            switch (i_Unit)
            {
                case Unit.Meter:
                    return "Meter";
                case Unit.Volt:
                    return "Volt";
                case Unit.NewtonMeter:
                    return "NewtonMeter";
                case Unit.Newton:
                    return "Newton";
                case Unit.Kilogram:
                    return "Kilogram";
                case Unit.Second:
                    return "Second";
                case Unit.Ampere:
                    return "Ampere";
                case Unit.Kelvin:
                    return "Kelvin";
                case Unit.Mole:
                    return "Mole";
                case Unit.Candela:
                    return "Candela";
                case Unit.Radian:
                    return "Radian";
                case Unit.Steradian:
                    return "Steradian";
                case Unit.MeterSquared:
                    return "MeterSquared";
                case Unit.MeterCubed:
                    return "MeterCubed";
                case Unit.MeterPerSecond:
                    return "MeterPerSecond";
                case Unit.MeterPerSecondSquared:
                    return "MeterPerSecondSquared";
                case Unit.RadianPerSecond:
                    return "RadianPerSecond";
                case Unit.RadianPerSecondSquared:
                    return "RadianPerSecondSquared";
                case Unit.Hertz:
                    return "Hertz";
                case Unit.Joule:
                    return "Joule";
                case Unit.Watt:
                    return "Watt";
                case Unit.Pascal:
                    return "Pascal";
                case Unit.Lumen:
                    return "Lumen";
                case Unit.Lux:
                    return "Lux";
                case Unit.Coulomb:
                    return "Coulomb";
                case Unit.Ohm:
                    return "Ohm";
                case Unit.Farad:
                    return "Farad";
                case Unit.Weber:
                    return "Weber";
                case Unit.Tesla:
                    return "Tesla";
                case Unit.Henry:
                    return "Henry";
                case Unit.Siemens:
                    return "Siemens";
                case Unit.Becquerel:
                    return "Becquerel";
                case Unit.Gray:
                    return "Gray";
                case Unit.Sievert:
                    return "Sievert";
                case Unit.Katal:
                    return "Katal";

                case Unit.Unknown:
                default:
                    return "Unknown";
            }
        }


        static void Main(string[] args)
        {
            // Program options
            bool TransmitMulticast = false;
            bool EnableHapticTest = false;
            bool bReadCentroids = false;
            List<String> HapticOnList = new List<String>();
            int Counter = 1;

            
            string HostName = "localhost:801"; // connect to Vicon system locally
           // string HostName = "192.168.1.1:801";
            if (args.Length > 0)
            {
                HostName = args[0];
            }

            // parsing all the haptic arguments
            for (int j = 1; j < args.Length; ++j)
            {
                string HapticArg = "--enable_haptic_test";
                if (String.Compare(args[j], HapticArg) == 0)
                {
                    EnableHapticTest = true;
                    ++j;
                    while (j < args.Length && String.Compare(args[j], 0, "--", 0, 2) != 0)
                    {
                        HapticOnList.Add(args[j]);
                        ++j;
                    }
                }

                string CentroidsArg = "--centroids";
                if (String.Compare(args[j], CentroidsArg) == 0)
                {
                    bReadCentroids = true;
                }
            }
            // Make a new client
            ViconDataStreamSDK.DotNET.Client MyClient = new ViconDataStreamSDK.DotNET.Client();

            // Connect to a server
            Console.Write("Connecting to {0} ...", HostName);
            while (!MyClient.IsConnected().Connected)
            {
                // Direct connection
                MyClient.Connect(HostName);

                // Multicast connection
                // MyClient.ConnectToMulticast( HostName, "224.0.0.0" );

                System.Threading.Thread.Sleep(200);
                Console.Write(".");
            }
            Console.WriteLine();

            // Enable some different data types
            MyClient.EnableSegmentData();
            MyClient.EnableMarkerData();
            MyClient.EnableUnlabeledMarkerData();
            MyClient.EnableDeviceData();
            if (bReadCentroids)
            {
                MyClient.EnableCentroidData();
            }

            Console.WriteLine("Segment Data Enabled: {0}", MyClient.IsSegmentDataEnabled().Enabled);
            Console.WriteLine("Marker Data Enabled: {0}", MyClient.IsMarkerDataEnabled().Enabled);
            Console.WriteLine("Unlabeled Marker Data Enabled: {0}", MyClient.IsUnlabeledMarkerDataEnabled().Enabled);
            Console.WriteLine("Device Data Enabled: {0}", MyClient.IsDeviceDataEnabled().Enabled);
            Console.WriteLine("Centroid Data Enabled: {0}", MyClient.IsCentroidDataEnabled().Enabled);

            // Set the streaming mode
            MyClient.SetStreamMode(ViconDataStreamSDK.DotNET.StreamMode.ClientPull);
            // MyClient.SetStreamMode( ViconDataStreamSDK.DotNET.StreamMode.ClientPullPreFetch );
            // MyClient.SetStreamMode( ViconDataStreamSDK.DotNET.StreamMode.ServerPush );

            // Set the global up axis
            MyClient.SetAxisMapping(ViconDataStreamSDK.DotNET.Direction.Forward,
                                     ViconDataStreamSDK.DotNET.Direction.Left,
                                     ViconDataStreamSDK.DotNET.Direction.Up); // Z-up
            // MyClient.SetAxisMapping( ViconDataStreamSDK.DotNET.Direction.Forward, 
            //                          ViconDataStreamSDK.DotNET.Direction.Up, 
            //                          ViconDataStreamSDK.DotNET.Direction.Right ); // Y-up

            Output_GetAxisMapping _Output_GetAxisMapping = MyClient.GetAxisMapping();
            Console.WriteLine("Axis Mapping: X-{0} Y-{1} Z-{2}", Adapt(_Output_GetAxisMapping.XAxis),
                                                                  Adapt(_Output_GetAxisMapping.YAxis),
                                                                  Adapt(_Output_GetAxisMapping.ZAxis));

            // Discover the version number
            Output_GetVersion _Output_GetVersion = MyClient.GetVersion();
            Console.WriteLine("Version: {0}.{1}.{2}", _Output_GetVersion.Major,
                                                       _Output_GetVersion.Minor,
                                                       _Output_GetVersion.Point);


            if (TransmitMulticast)
            {
                MyClient.StartTransmittingMulticast("localhost", "224.0.0.0");
            }


            // Let the user set the IP Address and Port for sending out OSC info
            Console.WriteLine("Enter IP Address of Remote Computer receiving data: ");
            String oscIP = Console.ReadLine();
            Console.WriteLine("Enter PORT of Remote Computer receiving data: ");
            String oscPORT = Console.ReadLine();

            // offer a use a default IP / PORT
            if (oscIP.Length == 0 && oscPORT.Length == 0)
            {
                oscIP = "192.168.125.20";
                oscPORT = "55555";
                Console.WriteLine("...Set to default IP and PORT: "+oscIP+":"+oscPORT);
                
            }

            // Setup OSC sender to remote computer
            var sender = new SharpOSC.UDPSender(oscIP, Int32.Parse(oscPORT));// "10.140.76.75", 55555);

            Console.WriteLine();
            Console.WriteLine("Ready to Stream!");
            Console.WriteLine("+ + + + + + + + + + + + ");

            // Stream Mocap Data
            while (true)
            {
                ++Counter;
                // Console.KeyAvailable throws an exception if stdin is a pipe (e.g.
                // with TrackerDssdkTests.py), so we use try..catch:
                try { 
                    if (Console.KeyAvailable){
                        break;
                    }
                }
                catch (InvalidOperationException){}

                // Get a frame
                //Console.Write("Waiting for new frame...");
                while (MyClient.GetFrame().Result != ViconDataStreamSDK.DotNET.Result.Success){
                    System.Threading.Thread.Sleep(200);
                    //Console.Write(".");
                }
               


                // stream out data to remote location via OSC
                sendRigidBodies(MyClient, sender);
                sendLocalRotationQuaternion(MyClient, sender);
                sendLabledMarkers(MyClient, sender);
                sendUnlabeledMarkers(MyClient,sender);
                          
            }


            // shut down and disconnect

            if (TransmitMulticast){
                MyClient.StopTransmittingMulticast();
            }

            // Disconnect and dispose
            MyClient.Disconnect();
            MyClient = null;
        }

        private static void sendLocalRotationQuaternion(Client MyClient, UDPSender sender)
        {
            // Count the number of subjects
            uint SubjectCount = MyClient.GetSubjectCount().SubjectCount;
            for (uint SubjectIndex = 0; SubjectIndex < SubjectCount; ++SubjectIndex)
            {

                // Get the subject name
                string SubjectName = MyClient.GetSubjectName(SubjectIndex).SubjectName;
                Console.WriteLine("    Name: {0}", SubjectName);

                // Get the root segment
                string RootSegment = MyClient.GetSubjectRootSegmentName(SubjectName).SegmentName;
                Console.WriteLine("    Root Segment: {0}", RootSegment);

                //Get the static segment translation
                Output_GetSegmentLocalRotationQuaternion Output =
                MyClient.GetSegmentLocalRotationQuaternion(SubjectName, RootSegment);

                Console.WriteLine("        LOCAL Rotation Quaternion: ({0},{1},{2},{3})",
                                   Output.Rotation[0],
                                   Output.Rotation[1],
                                   Output.Rotation[2],
                                   Output.Rotation[3]);


                String[] msg = new String[5];
                msg[0] = "RigidBody Name: " + SubjectName;
                msg[1] = "q.x: " + Output.Rotation[0].ToString();
                msg[2] = "q.y: " + Output.Rotation[1].ToString();
                msg[3] = "q.z: " + Output.Rotation[2].ToString();
                msg[4] = "q.w: " + Output.Rotation[3].ToString();

                var message = new SharpOSC.OscMessage("/localQuat", msg);
                sender.Send(message);

            }
        }
         


        /*
            Sends rigid body data out over OSC.
            Address Pattern: \rigidBody
            Format: String[4]
                RigidBodyName
                GlobalPosition.x
                GlobalPosition.y
                GlobalPosition.z
                GlobalOrientation.qx
                GlobalOrientation.qy
                GlobalOrientation.qz
                GlobalOrientation.qw
        */
        private static void sendRigidBodies(Client MyClient, UDPSender sender)
        {
            // Count the number of subjects
            uint SubjectCount = MyClient.GetSubjectCount().SubjectCount;
            for (uint SubjectIndex = 0; SubjectIndex < SubjectCount; ++SubjectIndex)
            {
               
                // Get the subject name
                string SubjectName = MyClient.GetSubjectName(SubjectIndex).SubjectName;
                Console.WriteLine("    Name: {0}", SubjectName);

                // Get the root segment
                string RootSegment = MyClient.GetSubjectRootSegmentName(SubjectName).SegmentName;
                Console.WriteLine("    Root Segment: {0}", RootSegment);

                //Get the static segment translation
                Output_GetSegmentGlobalTranslation _Output_GetSegmentGlobalTranslation =
                MyClient.GetSegmentGlobalTranslation(SubjectName, RootSegment);
                Console.WriteLine("        Global Translation: ({0},{1},{2}) {3}",
                                   _Output_GetSegmentGlobalTranslation.Translation[0],
                                   _Output_GetSegmentGlobalTranslation.Translation[1],
                                   _Output_GetSegmentGlobalTranslation.Translation[2],
                                   _Output_GetSegmentGlobalTranslation.Occluded);

                // Get the global segment rotation in quaternion co-ordinates
                Output_GetSegmentGlobalRotationQuaternion _Output_GetSegmentGlobalRotationQuaternion =
                MyClient.GetSegmentGlobalRotationQuaternion(SubjectName, RootSegment);
                Console.WriteLine("        Global Rotation Quaternion: ({0},{1},{2},{3}) {4}",
                                   _Output_GetSegmentGlobalRotationQuaternion.Rotation[0],
                                   _Output_GetSegmentGlobalRotationQuaternion.Rotation[1],
                                   _Output_GetSegmentGlobalRotationQuaternion.Rotation[2],
                                   _Output_GetSegmentGlobalRotationQuaternion.Rotation[3],
                                   _Output_GetSegmentGlobalRotationQuaternion.Occluded);



                String[] msg = new String[8];
                msg[0] = "RigidBody Name: "+ SubjectName;
                msg[1] = "pos.x: " + _Output_GetSegmentGlobalTranslation.Translation[0].ToString();
                msg[2] = "pos.y: " + _Output_GetSegmentGlobalTranslation.Translation[1].ToString();
                msg[3] = "pos.z: " + _Output_GetSegmentGlobalTranslation.Translation[2].ToString();
                msg[4] = "q.x: " + _Output_GetSegmentGlobalRotationQuaternion.Rotation[0].ToString();
                msg[5] = "q.y: " + _Output_GetSegmentGlobalRotationQuaternion.Rotation[1].ToString();
                msg[6] = "q.z: " + _Output_GetSegmentGlobalRotationQuaternion.Rotation[2].ToString();
                msg[7] = "q.w: " + _Output_GetSegmentGlobalRotationQuaternion.Rotation[3].ToString();

                // ignore dropped tracking frames
                if (_Output_GetSegmentGlobalTranslation.Translation[0] != 0 &&
                    _Output_GetSegmentGlobalTranslation.Translation[1] != 0 &&
                    _Output_GetSegmentGlobalTranslation.Translation[2] != 0 ) {
                    var message = new SharpOSC.OscMessage("/rigidBody", msg);
                    sender.Send(message);
                }

            }
        }

        /*
            Sends any labled markers out over OSC.
            Address Pattern: \labledMarker
            Format: String[5]
                MarkerID
                MarkerName
                GlobalMarkerPosition.x
                GlobalMarkerPosition.y
                GlobalMarkerPosition.z
        */
        private static void sendLabledMarkers(Client MyClient, UDPSender sender)
        {


            // For each subject in the scene
            uint SubjectCount = MyClient.GetSubjectCount().SubjectCount;
            for (uint SubjectIndex = 0; SubjectIndex < SubjectCount; ++SubjectIndex)
            {        
                // Get the subject name
                string SubjectName = MyClient.GetSubjectName(SubjectIndex).SubjectName;

                // Count the number of markers
                uint MarkerCount = MyClient.GetMarkerCount(SubjectName).MarkerCount;
                
                // for each marker in subject
                for (uint MarkerIndex = 0; MarkerIndex < MarkerCount; ++MarkerIndex)
                {
                    // Get the marker name
                    string MarkerName = MyClient.GetMarkerName(SubjectName, MarkerIndex).MarkerName;

                    // Get the global marker translation
                    Output_GetMarkerGlobalTranslation _Output_GetMarkerGlobalTranslation =
                      MyClient.GetMarkerGlobalTranslation(SubjectName, MarkerName);



                    String[] msg = new String[5];
                    msg[0] = "RigidBody Name: "+MarkerName;
                    msg[1] = "MarkerID: " + MarkerIndex;
                    msg[2] = "pos.x: " + _Output_GetMarkerGlobalTranslation.Translation[0].ToString();
                    msg[3] = "pos.y: " + _Output_GetMarkerGlobalTranslation.Translation[1].ToString();
                    msg[4] = "pos.z: " + _Output_GetMarkerGlobalTranslation.Translation[2].ToString();

                    // ignore dropped tracking locations
                    if (_Output_GetMarkerGlobalTranslation.Translation[0] != 0 &&
                        _Output_GetMarkerGlobalTranslation.Translation[1] != 0 &&
                        _Output_GetMarkerGlobalTranslation.Translation[2] != 0)
                    {
                        var message = new SharpOSC.OscMessage("/labledMarker", msg);
                        sender.Send(message);
                    }

                    Console.WriteLine("      Marker #{0}: {1} ({2}, {3}, {4}) {5}",
                                       MarkerIndex,
                                       MarkerName,
                                       _Output_GetMarkerGlobalTranslation.Translation[0],
                                       _Output_GetMarkerGlobalTranslation.Translation[1],
                                       _Output_GetMarkerGlobalTranslation.Translation[2],
                                       _Output_GetMarkerGlobalTranslation.Occluded);
               } 
        }


    }
        /*
            Sends any unlabled markers out over OSC.
            Address Pattern: \unlabledMarker
            Format: String[4]
                MarkerID
                GlobalMarkerPosition.x
                GlobalMarkerPosition.y
                GlobalMarkerPosition.z
        */
        private static void sendUnlabeledMarkers(Client MyClient, UDPSender sender)
        {
            // Get the unlabeled markers
            uint UnlabeledMarkerCount = MyClient.GetUnlabeledMarkerCount().MarkerCount;
            Console.WriteLine("    Unlabeled Markers ({0}):", UnlabeledMarkerCount);
            for (uint UnlabeledMarkerIndex = 0; UnlabeledMarkerIndex < UnlabeledMarkerCount; ++UnlabeledMarkerIndex)
            {
                // Get the global marker translation
                Output_GetUnlabeledMarkerGlobalTranslation _Output_GetUnlabeledMarkerGlobalTranslation
                  = MyClient.GetUnlabeledMarkerGlobalTranslation(UnlabeledMarkerIndex);



                String[] msg = new String[4];
                msg[0] = "UnlabledMarkerID: " + UnlabeledMarkerIndex + "";
                msg[1] = "pos.x: " + _Output_GetUnlabeledMarkerGlobalTranslation.Translation[0].ToString();
                msg[2] = "pos.y: " + _Output_GetUnlabeledMarkerGlobalTranslation.Translation[1].ToString();
                msg[3] = "pos.z: " + _Output_GetUnlabeledMarkerGlobalTranslation.Translation[2].ToString();

                var message = new SharpOSC.OscMessage("/unlabledMarker", msg);
                sender.Send(message);


                Console.WriteLine("      Marker #{0}: ({1}, {2}, {3})",
                                   UnlabeledMarkerIndex,
                                   _Output_GetUnlabeledMarkerGlobalTranslation.Translation[0],
                                   _Output_GetUnlabeledMarkerGlobalTranslation.Translation[1],
                                   _Output_GetUnlabeledMarkerGlobalTranslation.Translation[2]);
            }
        }



    }
}


////////////////////////////////////////////////////////
////////// REFERENCE ///////////////////////////////////
////////////////////////////////////////////////////////


// Get the frame number
//Output_GetFrameNumber _Output_GetFrameNumber = MyClient.GetFrameNumber();
//Console.WriteLine("Frame Number: {0}", _Output_GetFrameNumber.FrameNumber);

//Output_GetFrameRate _Output_GetFrameRate = MyClient.GetFrameRate();
//Console.WriteLine("Frame rate: {0}", _Output_GetFrameRate.FrameRateHz);




//// Count the number of subjects
//uint SubjectCount = MyClient.GetSubjectCount().SubjectCount;
//Console.WriteLine("Subjects ({0}):", SubjectCount);
//for (uint SubjectIndex = 0; SubjectIndex < SubjectCount; ++SubjectIndex)
//{
//    Console.WriteLine("  Subject #{0}", SubjectIndex);

//    // Get the subject name
//    string SubjectName = MyClient.GetSubjectName(SubjectIndex).SubjectName;
//    Console.WriteLine("    Name: {0}", SubjectName);

//    // Get the root segment
//    string RootSegment = MyClient.GetSubjectRootSegmentName(SubjectName).SegmentName;
//    Console.WriteLine("    Root Segment: {0}", RootSegment);

//    // Count the number of segments
//    uint SegmentCount = MyClient.GetSegmentCount(SubjectName).SegmentCount;
//    Console.WriteLine("    Segments ({0}):", SegmentCount);
//    for (uint SegmentIndex = 0; SegmentIndex < SegmentCount; ++SegmentIndex)
//    {
//        Console.WriteLine("      Segment #{0}", SegmentIndex);

//        // Get the segment name
//        string SegmentName = MyClient.GetSegmentName(SubjectName, SegmentIndex).SegmentName;
//        Console.WriteLine("        Name: {0}", SegmentName);

//        // Get the segment parent
//        string SegmentParentName = MyClient.GetSegmentParentName(SubjectName, SegmentName).SegmentName;
//        Console.WriteLine("        Parent: {0}", SegmentParentName);

//        // Get the segment's children
//        uint ChildCount = MyClient.GetSegmentChildCount(SubjectName, SegmentName).SegmentCount;
//        Console.WriteLine("     Children ({0}):", ChildCount);
//        for (uint ChildIndex = 0; ChildIndex < ChildCount; ++ChildIndex)
//        {
//            string ChildName = MyClient.GetSegmentChildName(SubjectName, SegmentName, ChildIndex).SegmentName;
//            Console.WriteLine("       {0}", ChildName);
//        }

//        // Get the static segment translation
//        Output_GetSegmentStaticTranslation _Output_GetSegmentStaticTranslation =
//          MyClient.GetSegmentStaticTranslation(SubjectName, SegmentName);
//        Console.WriteLine("        Static Translation: ({0},{1},{2})",
//                           _Output_GetSegmentStaticTranslation.Translation[0],
//                           _Output_GetSegmentStaticTranslation.Translation[1],
//                           _Output_GetSegmentStaticTranslation.Translation[2]);

//        // Get the static segment rotation in helical co-ordinates
//        Output_GetSegmentStaticRotationHelical _Output_GetSegmentStaticRotationHelical =
//          MyClient.GetSegmentStaticRotationHelical(SubjectName, SegmentName);
//        Console.WriteLine("        Static Rotation Helical: ({0},{1},{2})",
//                           _Output_GetSegmentStaticRotationHelical.Rotation[0],
//                           _Output_GetSegmentStaticRotationHelical.Rotation[1],
//                           _Output_GetSegmentStaticRotationHelical.Rotation[2]);

//        // Get the static segment rotation as a matrix
//        Output_GetSegmentStaticRotationMatrix _Output_GetSegmentStaticRotationMatrix =
//          MyClient.GetSegmentStaticRotationMatrix(SubjectName, SegmentName);
//        Console.WriteLine("        Static Rotation Matrix: ({0},{1},{2},{3},{4},{5},{6},{7},{8})",
//                           _Output_GetSegmentStaticRotationMatrix.Rotation[0],
//                           _Output_GetSegmentStaticRotationMatrix.Rotation[1],
//                           _Output_GetSegmentStaticRotationMatrix.Rotation[2],
//                           _Output_GetSegmentStaticRotationMatrix.Rotation[3],
//                           _Output_GetSegmentStaticRotationMatrix.Rotation[4],
//                           _Output_GetSegmentStaticRotationMatrix.Rotation[5],
//                           _Output_GetSegmentStaticRotationMatrix.Rotation[6],
//                           _Output_GetSegmentStaticRotationMatrix.Rotation[7],
//                           _Output_GetSegmentStaticRotationMatrix.Rotation[8]);

//        // Get the static segment rotation in quaternion co-ordinates
//        Output_GetSegmentStaticRotationQuaternion _Output_GetSegmentStaticRotationQuaternion =
//          MyClient.GetSegmentStaticRotationQuaternion(SubjectName, SegmentName);
//        Console.WriteLine("        Static Rotation Quaternion: ({0},{1},{2},{3})",
//                           _Output_GetSegmentStaticRotationQuaternion.Rotation[0],
//                           _Output_GetSegmentStaticRotationQuaternion.Rotation[1],
//                           _Output_GetSegmentStaticRotationQuaternion.Rotation[2],
//                           _Output_GetSegmentStaticRotationQuaternion.Rotation[3]);

//        // Get the static segment rotation in EulerXYZ co-ordinates
//        Output_GetSegmentStaticRotationEulerXYZ _Output_GetSegmentStaticRotationEulerXYZ =
//          MyClient.GetSegmentStaticRotationEulerXYZ(SubjectName, SegmentName);
//        Console.WriteLine("        Static Rotation EulerXYZ: ({0},{1},{2})",
//                           _Output_GetSegmentStaticRotationEulerXYZ.Rotation[0],
//                           _Output_GetSegmentStaticRotationEulerXYZ.Rotation[1],
//                           _Output_GetSegmentStaticRotationEulerXYZ.Rotation[2]);

//        // Get the global segment translation
//        Output_GetSegmentGlobalTranslation _Output_GetSegmentGlobalTranslation =
//          MyClient.GetSegmentGlobalTranslation(SubjectName, SegmentName);
//        Console.WriteLine("        Global Translation: ({0},{1},{2}) {3}",
//                           _Output_GetSegmentGlobalTranslation.Translation[0],
//                           _Output_GetSegmentGlobalTranslation.Translation[1],
//                           _Output_GetSegmentGlobalTranslation.Translation[2],
//                           _Output_GetSegmentGlobalTranslation.Occluded);

//        // Get the global segment rotation in helical co-ordinates
//        Output_GetSegmentGlobalRotationHelical _Output_GetSegmentGlobalRotationHelical =
//          MyClient.GetSegmentGlobalRotationHelical(SubjectName, SegmentName);
//        Console.WriteLine("        Global Rotation Helical: ({0},{1},{2}) {3}",
//                           _Output_GetSegmentGlobalRotationHelical.Rotation[0],
//                           _Output_GetSegmentGlobalRotationHelical.Rotation[1],
//                           _Output_GetSegmentGlobalRotationHelical.Rotation[2],
//                           _Output_GetSegmentGlobalRotationHelical.Occluded);

//        // Get the global segment rotation as a matrix
//        Output_GetSegmentGlobalRotationMatrix _Output_GetSegmentGlobalRotationMatrix =
//          MyClient.GetSegmentGlobalRotationMatrix(SubjectName, SegmentName);
//        Console.WriteLine("        Global Rotation Matrix: ({0},{1},{2},{3},{4},{5},{6},{7},{8}) {9}",
//                           _Output_GetSegmentGlobalRotationMatrix.Rotation[0],
//                           _Output_GetSegmentGlobalRotationMatrix.Rotation[1],
//                           _Output_GetSegmentGlobalRotationMatrix.Rotation[2],
//                           _Output_GetSegmentGlobalRotationMatrix.Rotation[3],
//                           _Output_GetSegmentGlobalRotationMatrix.Rotation[4],
//                           _Output_GetSegmentGlobalRotationMatrix.Rotation[5],
//                           _Output_GetSegmentGlobalRotationMatrix.Rotation[6],
//                           _Output_GetSegmentGlobalRotationMatrix.Rotation[7],
//                           _Output_GetSegmentGlobalRotationMatrix.Rotation[8],
//                           _Output_GetSegmentGlobalRotationMatrix.Occluded);

//        // Get the global segment rotation in quaternion co-ordinates
//        Output_GetSegmentGlobalRotationQuaternion _Output_GetSegmentGlobalRotationQuaternion =
//          MyClient.GetSegmentGlobalRotationQuaternion(SubjectName, SegmentName);
//        Console.WriteLine("        Global Rotation Quaternion: ({0},{1},{2},{3}) {4}",
//                           _Output_GetSegmentGlobalRotationQuaternion.Rotation[0],
//                           _Output_GetSegmentGlobalRotationQuaternion.Rotation[1],
//                           _Output_GetSegmentGlobalRotationQuaternion.Rotation[2],
//                           _Output_GetSegmentGlobalRotationQuaternion.Rotation[3],
//                           _Output_GetSegmentGlobalRotationQuaternion.Occluded);

//        // Get the global segment rotation in EulerXYZ co-ordinates
//        Output_GetSegmentGlobalRotationEulerXYZ _Output_GetSegmentGlobalRotationEulerXYZ =
//          MyClient.GetSegmentGlobalRotationEulerXYZ(SubjectName, SegmentName);
//        Console.WriteLine("        Global Rotation EulerXYZ: ({0},{1},{2}) {3}",
//                           _Output_GetSegmentGlobalRotationEulerXYZ.Rotation[0],
//                           _Output_GetSegmentGlobalRotationEulerXYZ.Rotation[1],
//                           _Output_GetSegmentGlobalRotationEulerXYZ.Rotation[2],
//                           _Output_GetSegmentGlobalRotationEulerXYZ.Occluded);

//        // Get the local segment translation
//        Output_GetSegmentLocalTranslation _Output_GetSegmentLocalTranslation =
//          MyClient.GetSegmentLocalTranslation(SubjectName, SegmentName);
//        Console.WriteLine("        Local Translation: ({0},{1},{2}) {3}",
//                           _Output_GetSegmentLocalTranslation.Translation[0],
//                           _Output_GetSegmentLocalTranslation.Translation[1],
//                           _Output_GetSegmentLocalTranslation.Translation[2],
//                           _Output_GetSegmentLocalTranslation.Occluded);

//        // Get the local segment rotation in helical co-ordinates
//        Output_GetSegmentLocalRotationHelical _Output_GetSegmentLocalRotationHelical =
//          MyClient.GetSegmentLocalRotationHelical(SubjectName, SegmentName);
//        Console.WriteLine("        Local Rotation Helical: ({0},{1},{2}) {3}",
//                           _Output_GetSegmentLocalRotationHelical.Rotation[0],
//                           _Output_GetSegmentLocalRotationHelical.Rotation[1],
//                           _Output_GetSegmentLocalRotationHelical.Rotation[2],
//                           _Output_GetSegmentLocalRotationHelical.Occluded);

//        // Get the local segment rotation as a matrix
//        Output_GetSegmentLocalRotationMatrix _Output_GetSegmentLocalRotationMatrix =
//          MyClient.GetSegmentLocalRotationMatrix(SubjectName, SegmentName);
//        Console.WriteLine("        Local Rotation Matrix: ({0},{1},{2},{3},{4},{5},{6},{7},{8}) {9}",
//                           _Output_GetSegmentLocalRotationMatrix.Rotation[0],
//                           _Output_GetSegmentLocalRotationMatrix.Rotation[1],
//                           _Output_GetSegmentLocalRotationMatrix.Rotation[2],
//                           _Output_GetSegmentLocalRotationMatrix.Rotation[3],
//                           _Output_GetSegmentLocalRotationMatrix.Rotation[4],
//                           _Output_GetSegmentLocalRotationMatrix.Rotation[5],
//                           _Output_GetSegmentLocalRotationMatrix.Rotation[6],
//                           _Output_GetSegmentLocalRotationMatrix.Rotation[7],
//                           _Output_GetSegmentLocalRotationMatrix.Rotation[8],
//                           _Output_GetSegmentLocalRotationMatrix.Occluded);

//        // Get the local segment rotation in quaternion co-ordinates
//        Output_GetSegmentLocalRotationQuaternion _Output_GetSegmentLocalRotationQuaternion =
//          MyClient.GetSegmentLocalRotationQuaternion(SubjectName, SegmentName);
//        Console.WriteLine("        Local Rotation Quaternion: ({0},{1},{2},{3}) {4}",
//                           _Output_GetSegmentLocalRotationQuaternion.Rotation[0],
//                           _Output_GetSegmentLocalRotationQuaternion.Rotation[1],
//                           _Output_GetSegmentLocalRotationQuaternion.Rotation[2],
//                           _Output_GetSegmentLocalRotationQuaternion.Rotation[3],
//                           _Output_GetSegmentLocalRotationQuaternion.Occluded);

//        // Get the local segment rotation in EulerXYZ co-ordinates
//        Output_GetSegmentLocalRotationEulerXYZ _Output_GetSegmentLocalRotationEulerXYZ =
//          MyClient.GetSegmentLocalRotationEulerXYZ(SubjectName, SegmentName);
//        Console.WriteLine("        Local Rotation EulerXYZ: ({0},{1},{2}) {3}",
//                           _Output_GetSegmentLocalRotationEulerXYZ.Rotation[0],
//                           _Output_GetSegmentLocalRotationEulerXYZ.Rotation[1],
//                           _Output_GetSegmentLocalRotationEulerXYZ.Rotation[2],
//                           _Output_GetSegmentLocalRotationEulerXYZ.Occluded);
//    }