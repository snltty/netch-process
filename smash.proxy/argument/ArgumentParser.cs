﻿using common.libs;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace smash.proxy.argument
{
    public static class ArgumentParser
    {
        public static Dictionary<string, string> Parse(string[] args, out string error)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].IndexOf("--") == 0)
                {
                    if (i + 1 < args.Length && args[i + 1].IndexOf("--") == -1)
                    {
                        dic.Add(args[i].Substring(2), args[i + 1]);
                        i++;
                    }
                    else
                    {
                        dic.Add(args[i].Substring(2), string.Empty);
                    }
                }
            }

            Validate(dic, out error);

            return dic;
        }
        static bool Validate(Dictionary<string, string> dic, out string error)
        {
            error = string.Empty;

            return ValidateMode(dic) &&
             ValidateServer(dic, out error) &&
             ValidateFake(dic, out error) &&
             ValidatePort(dic) &&
             ValidateKey(dic, out error) &&
             ValidateBuff(dic) && ValidateGateWay(dic);

            //ip route show default | awk '{print $3}'
        }
        static bool ValidateMode(Dictionary<string, string> dic)
        {
            //模式
            if (dic.ContainsKey("mode") == false || dic["mode"] != "client" && dic["mode"] != "server")
            {
                dic["mode"] = "server";
            }
            return true;
        }
        static bool ValidateServer(Dictionary<string, string> dic, out string error)
        {
            error = string.Empty;
            //服务器地址
            if (dic["mode"] == "client")
            {
                if (dic.ContainsKey("server") == false || string.IsNullOrWhiteSpace(dic["server"]))
                {
#if DEBUG
                    dic["server"] = "127.0.0.1:5413";
#else
                    error = $"client mode need server endpoint";
                    return false;
#endif
                }
            }
            return true;
        }
        static bool ValidateFake(Dictionary<string, string> dic, out string error)
        {
            error = string.Empty;
            if (dic["mode"] == "server")
            {
                //伪装地址
                if (dic.ContainsKey("fake") == false || string.IsNullOrWhiteSpace(dic["fake"]))
                {
#if DEBUG
                    dic["fake"] = "127.0.0.1:8100";
#else
                    error = $"server mode need fake endpoint";
                    return false;
#endif

                }
            }

            return true;
        }

        static bool ValidatePort(Dictionary<string, string> dic)
        {
            //监听端口
            if (dic.ContainsKey("port") == false || ushort.TryParse(dic["port"], out ushort port) == false)
            {
#if DEBUG
                dic["port"] = dic["mode"] == "client" ? "5414" : "5413";
#else
                dic["port"] = "5413";
#endif
            }
            return true;
        }
        static bool ValidateKey(Dictionary<string, string> dic, out string error)
        {
            error = string.Empty;
            //密钥
            if (dic.ContainsKey("key") == false || string.IsNullOrWhiteSpace(dic["key"]))
            {
#if DEBUG
                dic["key"] = "SNLTTY";
#else
                if (dic["mode"] == "client")
                {
                    error = $"client mode need server key";
                    return false;
                }
                else
                {
                    dic["key"] = Helper.RandomPasswordString(8);
                }
#endif
            }
            return true;
        }
        static bool ValidateBuff(Dictionary<string, string> dic)
        {
            //连接缓冲区大小(buffsize)
            if (dic.ContainsKey("buff") == false || string.IsNullOrWhiteSpace(dic["buff"]) || byte.TryParse(dic["buff"], out byte buff) == false)
            {
                dic["buff"] = "3";
            }
            return true;
        }

        static bool ValidateGateWay(Dictionary<string, string> dic)
        {
            //连接缓冲区大小(buffsize)
            if (dic.ContainsKey("gateway") == false && dic["mode"] == "server")
            {
                try
                {
                    dic["gateway"] = GetGateWay();
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error($"get dateway error，maybe need --gateway param : {ex}");
                    return false;
                }
            }
            return true;
        }

        static string GetGateWay()
        {
            if (OperatingSystem.IsWindows())
            {
                return GetGateWayWindows();
            }
            else if (OperatingSystem.IsLinux())
            {
                return GetGateWayLinux();
            }
            return string.Empty;
        }

        static string GetGateWayWindows()
        {
            IPAddress ip = Dns.GetHostEntry("www.baidu.com").AddressList[0];
            var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(ip, 80));
            string ipStr = (socket.LocalEndPoint as IPEndPoint).Address.ToString();

            string[] res = CommandHelper.Windows(string.Empty, new string[] { "ipconfig" }).Split('\n');
            for (int i = 0; i < res.Length; i++)
            {
                if (res[i].IndexOf(ipStr) >= 0)
                {
                    string _ip = res[i + 2].Split(':')[1];
                    return _ip.Substring(1, _ip.Length - 2);
                }
            }
            return string.Empty;
        }
        static string GetGateWayLinux()
        {
            string res = CommandHelper.Linux(string.Empty, new string[] { "ip route show default | awk '{print $3}'" });
            Console.WriteLine(res);
            Console.WriteLine($"--{res.Split(new char[] { '\r', '\n' })[0]}--");
            return res.Split(new char[] { '\r', '\n' })[0];
        }
    }
}
