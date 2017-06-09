﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkynetAndroid.Base;
using System.Net;
using System.Net.Sockets;
using SharpToxAndroid.Core;
using System.Threading;
using Newtonsoft.Json;
using System.IO;
using SkynetAndroid.Utils;
using System.Reflection;

namespace SharpLinkAndroid
{
    public static class TaskExtension
    {
        public static void ForgetOrThrow(this Task task)
        {
            task.ContinueWith((t) =>
            {
                Console.WriteLine(t.Exception);
                Utils.Log(t.Exception.ToString());
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }

    public class Connection
    {
        bool runningFlag = true;
        public bool IsConnected
        {
            get
            {
                if (mSkynet == null)
                    return false;
                if (!mSkynet.tox.IsConnected)
                    return false;
                if (targetToxId == "")
                    return false;
                try
                {
                    var toxkey = new ToxId(targetToxId).PublicKey;
                    int friendNum = mSkynet.tox.GetFriendByPublicKey(toxkey);
                    if (mSkynet.tox.GetFriendConnectionStatus(friendNum) == ToxConnectionStatus.None)
                    {
                        return false;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                }
                return false;
            }
        }
        SkynetAndroid.Base.SkynetAndroid mSkynet = null;
        private string targetToxId = "";
        private int targetPort = 0;
        private int localPort = 0;
        private string targetIP = "";


        public void Stop()
        {
            runningFlag = false;
        }

        public Connection()
        {
            mSkynet = new SkynetAndroid.Base.SkynetAndroid();
        }

        public void Connect(string[] args)
        {
            if (args.Length != 4 && args.Length != 0)
            {
                Console.WriteLine("usage: SharpLink [local_port] [target_tox_id] [target_ip] [target_port]");
                return;
            }

            // 线程监控程序
            Task.Run(() =>
            {
                while (runningFlag)
                {
                    int workerThreads, completionPortThreads;
                    ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
                    int workerThreadsMax, completionPortThreadsMax;
                    ThreadPool.GetMaxThreads(out workerThreadsMax, out completionPortThreadsMax);
                    int workerThreadsMin, completionPortThreadsMin;
                    ThreadPool.GetMinThreads(out workerThreadsMin, out completionPortThreadsMin);
                    ThreadPool.SetMinThreads(workerThreadsMax - workerThreads + 40, workerThreadsMax - workerThreads + 40);
                    Utils.Log("CurrentThreads: " + (workerThreadsMax - workerThreads), true);
                    Thread.Sleep(2000);
                }
            });

            if (args.Length == 4)
            {

                if (localPort == Convert.ToInt32(args[0]) && targetToxId == args[1] && targetIP == args[2] && targetPort == Convert.ToInt32(args[3]))
                {
                    // already started
                    return;
                }

                localPort = Convert.ToInt32(args[0]);
                targetToxId = args[1];
                targetIP = args[2];
                targetPort = Convert.ToInt32(args[3]);

                if (!ToxId.IsValid(targetToxId))
                {
                    Console.WriteLine("not a valid id");
                    Console.WriteLine("usage: SharpLink [local_port] [target_tox_id] [target_ip] [target_port]");
                    return;
                }

                // start check connection task
                Task.Run(() =>
                {
                    while (runningFlag)
                    {
                        mSkynet.HandShake(new ToxId(targetToxId), 10).GetAwaiter().GetResult();
                        Thread.Sleep(200 * 1000);
                    }
                });

                // create local socket server
                IPAddress ip = IPAddress.Parse("0.0.0.0");
                var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(new IPEndPoint(ip, localPort));
                serverSocket.Listen(1000);
                Task.Factory.StartNew(() =>
                {
                    while (runningFlag)
                    {
                        Utils.Log("Event: Waiting socket");
                        List<byte> tempData = new List<byte>();
                        Socket clientSocket = serverSocket.Accept();
                        Task.Factory.StartNew(() =>
                        {
                            bool closeFlag = false;
                            LinkClient mlink = null;
                            string tempConnectId = Guid.NewGuid().ToString();
                            Task.Factory.StartNew(() =>
                            {
                                while (runningFlag)
                                {
                                    byte[] buf = new byte[1024 * 512];
                                    try
                                    {
                                        int size = 0;
                                        if (clientSocket != null && clientSocket.Connected)
                                            size = clientSocket.Receive(buf);
                                        else
                                            break;
                                        if (mlink == null)
                                        {
                                            tempData.AddRange(buf.Take(size));
                                        }
                                        if (size == 0)
                                        {
                                            // socket closed
                                            if (mlink != null)
                                            {
                                                mlink.CloseRemote();
                                                mlink.Close();
                                            }

                                            if (!closeFlag && clientSocket.Connected)
                                            {
                                                closeFlag = true;
                                                try
                                                {
                                                    clientSocket.Shutdown(SocketShutdown.Both);
                                                }
                                                catch (SocketException ex)
                                                {
                                                    Utils.Log("Event ERROR: " + ex.Message);
                                                }

                                                clientSocket.Close();
                                                if (mlink != null)
                                                    Utils.Log("Event: Close Connection, ClientId: " + mlink.clientId + ", ConnectId: " + tempConnectId);
                                                else
                                                    Utils.Log("Event: Close Connection, ClinetId: null" + ", ConnectId: " + tempConnectId);
                                            }
                                            break;
                                        }
                                        if (mlink != null)
                                        {
                                            var res = mlink.Send(buf, size);
                                            if (!res && !closeFlag && clientSocket.Connected)
                                            {
                                                closeFlag = true;
                                                try
                                                {
                                                    clientSocket.Shutdown(SocketShutdown.Both);
                                                }
                                                catch (SocketException ex)
                                                {
                                                    Utils.Log("Event ERROR: " + ex.Message);
                                                }
                                                clientSocket.Close();
                                                mlink.CloseRemote();
                                                mlink.Close();
                                                Utils.Log("Event: Tox send message failed, ClientId: " + mlink.clientId + ", ConnectId: " + tempConnectId);
                                                Utils.Log("Event: Close Connection, ClientId: " + mlink.clientId + ", ConnectId: " + tempConnectId);
                                                break;
                                            }
                                        }

                                    }
                                    catch (Exception e)
                                    {
                                        Utils.Log("Event: ERROR " + e.Message);
                                        Utils.Log(e.StackTrace);
                                        if (mlink != null)
                                        {
                                            mlink.CloseRemote();
                                            mlink.Close();
                                        }

                                        if (!closeFlag && clientSocket.Connected)
                                        {
                                            closeFlag = true;
                                            try
                                            {
                                                clientSocket.Shutdown(SocketShutdown.Both);
                                            }
                                            catch (SocketException ex)
                                            {
                                                Utils.Log("Event ERROR: " + ex.Message);
                                            }
                                            clientSocket.Close();
                                            if (mlink != null)
                                                Utils.Log("Event: Close Connection, ClientId: " + mlink.clientId + ", ConnectId: " + tempConnectId);
                                            else
                                                Utils.Log("Event: Close Connection, ClinetId: null" + ", ConnectId: " + tempConnectId);
                                        }
                                        break;
                                    }

                                }
                            }, TaskCreationOptions.LongRunning).ForgetOrThrow();
                            mlink = LinkClient.Connect(mSkynet, targetToxId, IPAddress.Parse(targetIP), targetPort,
                                // message handler
                                (msg) =>
                                {
                                    try
                                    {
                                        if (clientSocket != null && clientSocket.Connected)
                                            clientSocket.Send(msg, SocketFlags.None);
                                    }
                                    catch (Exception e)
                                    {
                                        Utils.Log("ERROR " + e.Message);
                                        Utils.Log(e.StackTrace);
                                        mlink.CloseRemote();
                                        mlink.Close();
                                        if (!closeFlag && clientSocket.Connected)
                                        {
                                            closeFlag = true;
                                            try
                                            {
                                                clientSocket.Shutdown(SocketShutdown.Both);
                                            }
                                            catch (SocketException ex)
                                            {
                                                Utils.Log("Event ERROR: " + ex.Message);
                                            }
                                            clientSocket.Close();
                                            Utils.Log("Event: Close Connection, ClientId: " + mlink.clientId + ", ConnectId: " + tempConnectId);
                                        }
                                    }
                                },
                                // close handler
                                () =>
                                {
                                    if (!closeFlag && clientSocket.Connected)
                                    {
                                        closeFlag = true;
                                        try
                                        {
                                            clientSocket.Shutdown(SocketShutdown.Both);
                                        }
                                        catch (SocketException ex)
                                        {
                                            Utils.Log("Event ERROR: " + ex.Message);
                                        }
                                        clientSocket.Close();
                                        Utils.Log("Event: Close Connection, ClientId: " + mlink.clientId + ", ConnectId: " + tempConnectId);
                                    }
                                }
                            );
                            if (mlink == null)
                            {
                                // connected failed
                                Utils.Log("Event: Connected failed, ClientId: null" + ", ConnectId: " + tempConnectId);
                                if (!closeFlag && clientSocket.Connected)
                                {
                                    closeFlag = true;
                                    try
                                    {
                                        clientSocket.Shutdown(SocketShutdown.Both);
                                    }
                                    catch (SocketException ex)
                                    {
                                        Utils.Log("Event ERROR: " + ex.Message);
                                    }
                                    clientSocket.Close();
                                    Utils.Log("Event: Close Connection, ClientId: null" + ", ConnectId: " + tempConnectId);
                                }
                                return;
                            }
                            if (tempData.Count != 0)
                                mlink.Send(tempData.ToArray(), tempData.Count);
                            // check if socket has closed
                            if (closeFlag)
                            {
                                // socket has closed
                                Utils.Log("Event: Close Remote, ClientId: " + mlink.clientId + ", ConnectId: " + tempConnectId);
                                mlink.CloseRemote();
                                mlink.Close();
                            }
                        }, TaskCreationOptions.LongRunning).ForgetOrThrow();
                    }
                }, TaskCreationOptions.LongRunning).ForgetOrThrow();
            }

            mSkynet.addNewReqListener("", (req) =>
            {
                // handle 
                if (req.toNodeId == "" && req.url == "/connect")
                {
                    Utils.Log("Event: Task Connect to " + req.fromNodeId + ", MessageId: " + req.uuid);
                    Task.Factory.StartNew(() =>
                    {
                        // connect to server received, create sockets
                        Utils.Log("Event: Task Started Connect to " + req.fromNodeId);
                        try
                        {
                            string reqStr = Encoding.UTF8.GetString(req.content);
                            string ipstr = reqStr.Split('\n')[0];
                            string port = reqStr.Split('\n')[1];
                            Utils.Log("Event: Connect to " + ipstr + " " + port + " " + req.fromNodeId);
                            IPAddress targetIp = IPAddress.Parse(ipstr);
                            Socket mClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            bool closeFlag = false;
                            mClientSocket.Connect(new IPEndPoint(targetIp, Convert.ToInt32(port)));
                            Utils.Log("Event: Connect to " + ipstr + " " + port + " Success " + req.fromNodeId);
                            var mlink = LinkClient.Connect(mSkynet, req.fromToxId, req.fromNodeId);
                            req.toNodeId = mlink.clientId;
                            Utils.Log("Event: Connect to " + ipstr + " " + port + " Success " + req.fromNodeId + " , mLinkID: " + mlink.clientId);

                            mlink.OnMessage((msg) =>
                            {
                                try
                                {
                                    Utils.Log("Event: Start Write Message, mLinkID: " + mlink.clientId);
                                    if (mClientSocket != null && mClientSocket.Connected)
                                        mClientSocket.Send(msg, SocketFlags.None);
                                    Utils.Log("Event: Write Message Success, mLinkID: " + mlink.clientId);
                                }
                                catch (Exception e)
                                {
                                    Utils.Log("Event: ERROR " + e.Message);
                                    Utils.Log(e.StackTrace);
                                    mlink.CloseRemote();
                                    mlink.Close();
                                    if (!closeFlag && mClientSocket.Connected)
                                    {
                                        closeFlag = true;
                                        try
                                        {
                                            mClientSocket.Shutdown(SocketShutdown.Both);
                                        }
                                        catch (SocketException ex)
                                        {
                                            Utils.Log("Event: " + ex.Message);
                                        }

                                        mClientSocket.Close();
                                        Utils.Log("Event: Close Socket" + ipstr + " " + port + " mLinkID " + mlink.clientId);
                                    }
                                }
                            });
                            mlink.OnClose(() =>
                            {
                                if (!closeFlag && mClientSocket.Connected)
                                {
                                    closeFlag = true;
                                    try
                                    {
                                        mClientSocket.Shutdown(SocketShutdown.Both);
                                    }
                                    catch (SocketException ex)
                                    {
                                        Utils.Log("Event: " + ex.Message);
                                    }
                                    mClientSocket.Close();
                                    Utils.Log("Event: Close Socket" + ipstr + " " + port + " mLinkID " + mlink.clientId);
                                }
                            });
                            // send response after all handler has been set
                            mSkynet.sendResponse(req.createResponse(Encoding.UTF8.GetBytes("OK")), new ToxId(req.fromToxId));
                            Task.Factory.StartNew(() =>
                            {
                                while (runningFlag)
                                {
                                    byte[] buf = new byte[1024 * 512];
                                    try
                                    {
                                        Utils.Log("Event: Start Read Data, Clientid: " + mlink.clientId);
                                        int size = 0;
                                        if (mClientSocket != null && mClientSocket.Connected)
                                            size = mClientSocket.Receive(buf);
                                        else
                                        {
                                            Utils.Log("Event: Socket already closed" + ipstr + " " + port + " mLinkID " + mlink.clientId);
                                            break;
                                        }

                                        if (size == 0)
                                        {
                                            if (!closeFlag && mClientSocket.Connected)
                                            {
                                                Utils.Log("Event: Close Connection, Clientid: " + mlink.clientId);
                                                closeFlag = true;
                                                try
                                                {
                                                    mClientSocket.Shutdown(SocketShutdown.Both);
                                                }
                                                catch (SocketException ex)
                                                {
                                                    Utils.Log("Event: " + ex.Message);
                                                }
                                                mClientSocket.Close();
                                            }
                                            mlink.CloseRemote();
                                            mlink.Close();
                                            break;
                                        }
                                        else
                                        {
                                            Utils.Log("Event: Read Data " + size + ", Clientid: " + mlink.clientId);
                                        }
                                        var res = mlink.Send(buf, size);
                                        if (!res)
                                        {
                                            // send failed
                                            if (!closeFlag && mClientSocket.Connected)
                                            {
                                                closeFlag = true;
                                                try
                                                {
                                                    mClientSocket.Shutdown(SocketShutdown.Both);
                                                }
                                                catch (SocketException ex)
                                                {
                                                    Utils.Log("Event: " + ex.Message);
                                                }
                                                mClientSocket.Close();
                                                mlink.Close();
                                                Utils.Log("Event: Tox send message failed, Clientid: " + mlink.clientId);
                                                break;
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        /*if (e.ErrorCode != 10004) // this is not an error
                                        {
                                            Console.WriteLine("Time: " + Utils.UnixTimeNow() + " Event: ERROR " + e.Message);
                                            Console.WriteLine(e.StackTrace);
                                        }*/
                                        Utils.Log("Event: ERROR " + e.Message);
                                        Utils.Log(e.StackTrace);
                                        mlink.CloseRemote();
                                        mlink.Close();
                                        if (!closeFlag && mClientSocket.Connected)
                                        {
                                            closeFlag = true;
                                            try
                                            {
                                                mClientSocket.Shutdown(SocketShutdown.Both);
                                            }
                                            catch (SocketException ex)
                                            {
                                                Utils.Log("Event: " + ex.Message);
                                            }
                                            mClientSocket.Close();
                                            Utils.Log("Event: Close Connection, ClientId: " + mlink.clientId);
                                        }
                                        break;
                                    }
                                }
                            }, TaskCreationOptions.LongRunning).ForgetOrThrow();
                            Utils.Log("Event: Connect to " + ipstr + " " + port + " All Success " + req.fromNodeId + ", mLinkID: " + mlink.clientId);
                        }
                        catch (Exception e)
                        {
                            Utils.Log("Event: ERROR " + e.Message);
                            Utils.Log(e.StackTrace);

                            // connected failed
                            string reqStr = Encoding.UTF8.GetString(req.content);
                            string ipstr = reqStr.Split('\n')[0];
                            string port = reqStr.Split('\n')[1];
                            Utils.Log("Event: Connect to " + ipstr + " " + port + " failed");
                            var response = req.createResponse(Encoding.UTF8.GetBytes("failed"));
                            mSkynet.sendResponse(response, new ToxId(response.toToxId));
                        }
                    }, TaskCreationOptions.LongRunning).ForgetOrThrow();
                }
                else if (req.toNodeId == "" && req.url == "/handshake")
                {
                    var response = req.createResponse(Encoding.UTF8.GetBytes("OK"));
                    Utils.Log("Event: HandShake from " + response.toToxId + ", MessageID: " + req.uuid, true);
                    Utils.Log("Event: Send HandShake response " + response.uuid + ", ToxId: " + response.toToxId, true);
                    Console.WriteLine("Event: HandShake from " + response.toToxId + ", MessageID: " + req.uuid);
                    mSkynet.sendResponse(response, new ToxId(response.toToxId));
                }
            });

            while (runningFlag)
            {
                Thread.Sleep(10);
            }

            mSkynet.stop();
        }
    }
}
