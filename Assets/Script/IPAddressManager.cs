using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

public class IPAddressManager : MonoBehaviour
{
    public static IPAddressManager instance;

    private void Awake()
    {
        instance = this;
    }

    public string GetLocalIPv4Address()
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus == OperationalStatus.Up &&
                (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                 ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
            {
                foreach (IPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.Address.ToString();
                    }
                }
            }
        }
        return "No local IPv4 address found.";
    }

    void Start()
    {
        Debug.Log("Local IP Address: " + GetLocalIPv4Address());
    }
}