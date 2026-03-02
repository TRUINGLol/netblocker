using System.Runtime.InteropServices;
using InternetBlocker.Core.Interfaces;
using InternetBlocker.Core.Models;
using System.Diagnostics;
using System.Net;

namespace InternetBlocker.Infrastructure.Windows;

public class WindowsMonitorService : IMonitorService
{
    private const int AfInet = 2; // IPv4
    private const int AfInet6 = 23; // IPv6
    private const int TcpTableOwnerPidAll = 5;

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion, int tblClass, uint reserved = 0);

    public IEnumerable<ConnectionInfo> GetActiveConnections()
    {
        var connections = new List<ConnectionInfo>();
        connections.AddRange(GetTcpConnections(AfInet));
        connections.AddRange(GetTcpConnections(AfInet6));
        return connections;
    }

    private List<ConnectionInfo> GetTcpConnections(int ipVersion)
    {
        var connections = new List<ConnectionInfo>();
        int bufferSize = 0;
        GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, false, ipVersion, TcpTableOwnerPidAll);

        if (bufferSize == 0) return connections;

        IntPtr tablePtr = Marshal.AllocHGlobal(bufferSize);
        try
        {
            if (GetExtendedTcpTable(tablePtr, ref bufferSize, false, ipVersion, TcpTableOwnerPidAll) == 0)
            {
                int rowCount = Marshal.ReadInt32(tablePtr);
                IntPtr rowPtr = (IntPtr)((long)tablePtr + 4);

                for (int i = 0; i < rowCount; i++)
                {
                    ConnectionInfo? conn = null;
                    if (ipVersion == AfInet)
                    {
                        var row = Marshal.PtrToStructure<TcpRow>(rowPtr);
                        conn = CreateConnectionInfo(row.dwOwningPid, new IPAddress(row.dwLocalAddr), 
                            BitConverter.ToUInt16(new byte[] { row.dwLocalPort[1], row.dwLocalPort[0] }, 0),
                            new IPAddress(row.dwRemoteAddr),
                            BitConverter.ToUInt16(new byte[] { row.dwRemotePort[1], row.dwRemotePort[0] }, 0),
                            row.dwState);
                        rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf<TcpRow>());
                    }
                    else
                    {
                        var row = Marshal.PtrToStructure<Tcp6Row>(rowPtr);
                        conn = CreateConnectionInfo(row.dwOwningPid, new IPAddress(row.ucLocalAddr),
                            BitConverter.ToUInt16(new byte[] { row.dwLocalPort[1], row.dwLocalPort[0] }, 0),
                            new IPAddress(row.ucRemoteAddr),
                            BitConverter.ToUInt16(new byte[] { row.dwRemotePort[1], row.dwRemotePort[0] }, 0),
                            row.dwState);
                        rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf<Tcp6Row>());
                    }

                    if (conn != null) connections.Add(conn);
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(tablePtr);
        }

        return connections;
    }

    private ConnectionInfo CreateConnectionInfo(uint pid, IPAddress localAddr, ushort localPort, IPAddress remoteAddr, ushort remotePort, uint state)
    {
        string processName = "Unknown";
        string processPath = "Unknown";
        try
        {
            using var process = Process.GetProcessById((int)pid);
            processName = process.ProcessName;
            processPath = process.MainModule?.FileName ?? "Unknown";
        }
        catch { }

        return new ConnectionInfo(
            (int)pid,
            processName,
            processPath,
            localAddr.ToString(),
            localPort,
            remoteAddr.ToString(),
            remotePort,
            NetworkProtocol.Tcp,
            MapStatus(state)
        );
    }

    public Task<IEnumerable<ConnectionInfo>> GetActiveConnectionsAsync() => Task.FromResult(GetActiveConnections());

    private ConnectionStatus MapStatus(uint state) => state switch
    {
        2 => ConnectionStatus.Listen,
        5 => ConnectionStatus.Active,
        _ => ConnectionStatus.Closed
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct TcpRow
    {
        public uint dwState;
        public uint dwLocalAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] dwLocalPort;
        public uint dwRemoteAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] dwRemotePort;
        public uint dwOwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Tcp6Row
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] ucLocalAddr;
        public uint dwLocalScopeId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] dwLocalPort;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] ucRemoteAddr;
        public uint dwRemoteScopeId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] dwRemotePort;
        public uint dwState;
        public uint dwOwningPid;
    }
}
