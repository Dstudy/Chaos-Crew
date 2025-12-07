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
                        // Skip loopback and link-local addresses
                        string ipString = ip.Address.ToString();
                        if (!ipString.StartsWith("127.") && !ipString.StartsWith("169.254."))
                        {
                            return ipString;
                        }
                    }
                }
            }
        }
        return "No local IPv4 address found.";
    }

    public void LogAllIPv4Addresses()
    {
        Debug.Log("=== ALL NETWORK INTERFACES ===");
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus == OperationalStatus.Up)
            {
                Debug.Log($"Interface: {ni.Name} ({ni.NetworkInterfaceType})");
                foreach (IPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        Debug.Log($"  - {ip.Address}");
                    }
                }
            }
        }
    }

    void Start()
    {
        Debug.Log("Local IP Address: " + GetLocalIPv4Address());
    }
}