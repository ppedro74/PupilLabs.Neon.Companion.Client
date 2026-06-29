namespace PupilLabs.Neon.Companion.Client.ConsoleApp
{
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using UnityEngine;

    public static class NetworkHelper
    {
        public static bool IsIpAddress(string ip)
        {
            return !string.IsNullOrWhiteSpace(ip) && IPAddress.TryParse(ip, out _);
        }

        /// <summary>
        /// Resolves a hostname to an IP address. If the input is already an IP address, returns it as-is.
        /// </summary>
        public static async Task<NeonResult<string>> ResolveHostToIpAsync(string hostOrIp)
        {
            if (string.IsNullOrWhiteSpace(hostOrIp))
            {
                throw new ArgumentException("The host or IP address target string cannot be null or whitespace.", nameof(hostOrIp));
            }

            // Check if it's already an IP address
            if (IPAddress.TryParse(hostOrIp, out var ipAddress))
            {
                return NeonResult<string>.Success(ipAddress.ToString(), HttpStatusCode.OK);
            }

            // It's a hostname, resolve it asynchronously
            try
            {
                var hostEntry = await Dns.GetHostEntryAsync(hostOrIp);

                // Prefer IPv4 addresses
                var ipv4Address = hostEntry.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                if (ipv4Address is not null)
                {
                    return NeonResult<string>.Success(ipv4Address.ToString(), HttpStatusCode.OK);
                }

                // Fall back to any available address (like IPv6)
                if (hostEntry.AddressList != null && hostEntry.AddressList.Length > 0)
                {
                    return NeonResult<string>.Success(hostEntry.AddressList[0].ToString(), HttpStatusCode.OK);
                }

                return NeonResult<string>.Failure($"DNS resolution yielded empty result registers for hostname '{hostOrIp}'");
            }
            catch (SocketException ex)
            {
                return NeonResult<string>.Failure($"Network socket pipeline failed to resolve hostname '{hostOrIp}': {ex.Message}");
            }
            catch (Exception ex)
            {
                return NeonResult<string>.Failure($"An unexpected internal diagnostic exception halted DNS resolution: {ex.Message}");
            }
        }

        public static IPAddress[] GetLocalIpAddresses()
        {
            var addresses = new List<IPAddress>();
            Debug.Log($"[{nameof(NetworkHelper)}] getting local IP address");
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                Debug.Log($"[{nameof(NetworkHelper)}] iterating over network interface of type: {ni.NetworkInterfaceType} and status {ni.OperationalStatus}");
                if (ni.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        Debug.Log($"[{nameof(NetworkHelper)}] iterating over unicast ip of address family: {ip.Address.AddressFamily}");
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork && IPAddress.IsLoopback(ip.Address) == false && ip.IsDnsEligible)
                        {
                            Debug.Log($"[{nameof(NetworkHelper)}] adding local IP address: {ip.Address}");
                            addresses.Add(ip.Address);
                        }
                    }
                }
            }

            return addresses.ToArray();
        }
    }
}