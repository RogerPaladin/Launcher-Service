﻿    using System;
    using System.Text;
    using System.Security;
    using System.Management;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

namespace Black_Roger_Service
{
        public class Win32API
        {
            #region WMI Constants

            private const String cstrScope = "root\\CIMV2";
            private const String cstrLoggenInUser = "SELECT * FROM Win32_ComputerSystem";

            #endregion

            #region Win32 API routines

            [StructLayout(LayoutKind.Sequential)]
            struct SECURITY_ATTRIBUTES
            {
                public Int32 Length;
                public IntPtr lpSecurityDescriptor;
                public Boolean bInheritHandle;
            }

            enum TOKEN_TYPE
            {
                TokenPrimary = 1,
                TokenImpersonation = 2
            }

            enum TOKEN_INFORMATION_CLASS
            {
                TokenUser = 1,
                TokenGroups,
                TokenPrivileges,
                TokenOwner,
                TokenPrimaryGroup,
                TokenDefaultDacl,
                TokenSource,
                TokenType,
                TokenImpersonationLevel,
                TokenStatistics,
                TokenRestrictedSids,
                TokenSessionId,
                TokenGroupsAndPrivileges,
                TokenSessionReference,
                TokenSandBoxInert,
                TokenAuditPolicy,
                TokenOrigin,
                MaxTokenInfoClass  // MaxTokenInfoClass should always be the last enum
            }

            [StructLayout(LayoutKind.Sequential)]
            struct STARTUPINFO
            {
                public Int32 cb;
                public String lpReserved;
                public String lpDesktop;
                public String lpTitle;
                public UInt32 dwX;
                public UInt32 dwY;
                public UInt32 dwXSize;
                public UInt32 dwYSize;
                public UInt32 dwXCountChars;
                public UInt32 dwYCountChars;
                public UInt32 dwFillAttribute;
                public UInt32 dwFlags;
                public short wShowWindow;
                public short cbReserved2;
                public IntPtr lpReserved2;
                public IntPtr hStdInput;
                public IntPtr hStdOutput;
                public IntPtr hStdError;
            }

            [StructLayout(LayoutKind.Sequential)]
            struct PROCESS_INFORMATION
            {
                public IntPtr hProcess;
                public IntPtr hThread;
                public UInt32 dwProcessId;
                public UInt32 dwThreadId;
            }

            enum SECURITY_IMPERSONATION_LEVEL
            {
                SecurityAnonymous = 0,
                SecurityIdentification = 1,
                SecurityImpersonation = 2,
                SecurityDelegation = 3,
            }

            [StructLayout(LayoutKind.Sequential)]
            struct LUID
            {
                public Int32 LowPart;
                public Int32 HighPart;
            }

            [StructLayout(LayoutKind.Sequential)]
            struct LUID_AND_ATRIBUTES
            {
                LUID Luid;
                Int32 Attributes;
            }

            [StructLayout(LayoutKind.Sequential)]
            struct TOKEN_PRIVILEGES
            {
                public Int32 PrivilegeCount;
                //LUID_AND_ATRIBUTES
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
                public Int32[] Privileges;
            }

            const Int32 READ_CONTROL = 0x00020000;

            const Int32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;

            const Int32 STANDARD_RIGHTS_READ = READ_CONTROL;
            const Int32 STANDARD_RIGHTS_WRITE = READ_CONTROL;
            const Int32 STANDARD_RIGHTS_EXECUTE = READ_CONTROL;

            const Int32 STANDARD_RIGHTS_ALL = 0x001F0000;

            const Int32 SPECIFIC_RIGHTS_ALL = 0x0000FFFF;

            const Int32 TOKEN_ASSIGN_PRIMARY = 0x0001;
            const Int32 TOKEN_DUPLICATE = 0x0002;
            const Int32 TOKEN_IMPERSONATE = 0x0004;
            const Int32 TOKEN_QUERY = 0x0008;
            const Int32 TOKEN_QUERY_SOURCE = 0x0010;
            const Int32 TOKEN_ADJUST_PRIVILEGES = 0x0020;
            const Int32 TOKEN_ADJUST_GROUPS = 0x0040;
            const Int32 TOKEN_ADJUST_DEFAULT = 0x0080;
            const Int32 TOKEN_ADJUST_SESSIONID = 0x0100;

            const Int32 TOKEN_ALL_ACCESS_P = (
                STANDARD_RIGHTS_REQUIRED |
                TOKEN_ASSIGN_PRIMARY |
                TOKEN_DUPLICATE |
                TOKEN_IMPERSONATE |
                TOKEN_QUERY |
                TOKEN_QUERY_SOURCE |
                TOKEN_ADJUST_PRIVILEGES |
                TOKEN_ADJUST_GROUPS |
                TOKEN_ADJUST_DEFAULT);

            const Int32 TOKEN_ALL_ACCESS = TOKEN_ALL_ACCESS_P | TOKEN_ADJUST_SESSIONID;

            const Int32 TOKEN_READ = STANDARD_RIGHTS_READ | TOKEN_QUERY;


            const Int32 TOKEN_WRITE = STANDARD_RIGHTS_WRITE |
                                          TOKEN_ADJUST_PRIVILEGES |
                                          TOKEN_ADJUST_GROUPS |
                                          TOKEN_ADJUST_DEFAULT;

            const Int32 TOKEN_EXECUTE = STANDARD_RIGHTS_EXECUTE;

            const UInt32 MAXIMUM_ALLOWED = 0x2000000;

            const Int32 CREATE_NEW_PROCESS_GROUP = 0x00000200;
            const Int32 CREATE_UNICODE_ENVIRONMENT = 0x00000400;

            const Int32 IDLE_PRIORITY_CLASS = 0x40;
            const Int32 NORMAL_PRIORITY_CLASS = 0x20;
            const Int32 HIGH_PRIORITY_CLASS = 0x80;
            const Int32 REALTIME_PRIORITY_CLASS = 0x100;

            const Int32 CREATE_NEW_CONSOLE = 0x00000010;

            const string SE_DEBUG_NAME = "SeDebugPrivilege";
            const string SE_RESTORE_NAME = "SeRestorePrivilege";
            const string SE_BACKUP_NAME = "SeBackupPrivilege";

            const Int32 SE_PRIVILEGE_ENABLED = 0x0002;

            const Int32 ERROR_NOT_ALL_ASSIGNED = 1300;

            [StructLayout(LayoutKind.Sequential)]
            struct PROCESSENTRY32
            {
                UInt32 dwSize;
                UInt32 cntUsage;
                UInt32 th32ProcessID;
                IntPtr th32DefaultHeapID;
                UInt32 th32ModuleID;
                UInt32 cntThreads;
                UInt32 th32ParentProcessID;
                Int32 pcPriClassBase;
                UInt32 dwFlags;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                string szExeFile;
            }

            const UInt32 TH32CS_SNAPPROCESS = 0x00000002;

            const Int32 INVALID_HANDLE_VALUE = -1;

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern Boolean CloseHandle(IntPtr hSnapshot);

            [DllImport("kernel32.dll")]
            public static extern UInt32 WTSGetActiveConsoleSessionId();

            [DllImport("Wtsapi32.dll")]
            static extern UInt32 WTSQueryUserToken(UInt32 SessionId, ref IntPtr phToken);

            [DllImport("advapi32.dll", SetLastError = true)]
            static extern Boolean LookupPrivilegeValue(IntPtr lpSystemName, string lpname, [MarshalAs(UnmanagedType.Struct)] ref LUID lpLuid);

            [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            extern static Boolean CreateProcessAsUser(IntPtr hToken, String lpApplicationName, String lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
                ref SECURITY_ATTRIBUTES lpThreadAttributes, Boolean bInheritHandle, Int32 dwCreationFlags, IntPtr lpEnvironment,
                String lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

            [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
            extern static Boolean DuplicateTokenEx(IntPtr ExistingTokenHandle, UInt32 dwDesiredAccess,
                ref SECURITY_ATTRIBUTES lpThreadAttributes, Int32 TokenType,
                Int32 ImpersonationLevel, ref IntPtr DuplicateTokenHandle);

            [DllImport("kernel32.dll")]
            static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Boolean bInheritHandle, UInt32 dwProcessId);

            [DllImport("advapi32", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
            static extern Boolean OpenProcessToken(IntPtr ProcessHandle, // handle to process
                                                Int32 DesiredAccess, // desired access to process
                                                ref IntPtr TokenHandle); // handle to open access token

            [DllImport("advapi32.dll", SetLastError = true)]
            static extern Boolean AdjustTokenPrivileges(IntPtr TokenHandle, Boolean DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, Int32 BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

            [DllImport("advapi32.dll", SetLastError = true)]
            static extern Boolean SetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, ref UInt32 TokenInformation, UInt32 TokenInformationLength);

            [DllImport("userenv.dll", SetLastError = true)]
            static extern Boolean CreateEnvironmentBlock(ref IntPtr lpEnvironment, IntPtr hToken, Boolean bInherit);

            #endregion

            #region Methods

            /// <summary>
            /// Method returns name of the user that logged in on workstation
            /// </summary>
            public static String GetLoggedInUserName()
            {
                String userName = String.Empty;

                try
                {
                    ManagementObjectSearcher searcher =
                        new ManagementObjectSearcher(cstrScope, cstrLoggenInUser);

                    foreach (ManagementObject queryObj in searcher.Get())
                    {
                        userName = queryObj["UserName"].ToString();
                        break;
                    }
                }
                catch
                {
                    userName = String.Empty;
                }

                return userName;
            }

            /// <summary>
            /// Creates the process in the interactive desktop with credentials of the logged in user.
            /// </summary>
            public static Boolean CreateProcessAsUser(String commandLine,
                String workingDirectory,
                String userAppName,
                out StringBuilder output)
            {
                Boolean processStarted = false;
                output = new StringBuilder();

                try
                {
                    UInt32 dwSessionId = WTSGetActiveConsoleSessionId();
                    output.AppendLine(String.Format("Active console session Id: {0}", dwSessionId));

                    IntPtr hUserToken = IntPtr.Zero;
                    WTSQueryUserToken(dwSessionId, ref hUserToken);

                    if (hUserToken != IntPtr.Zero)
                    {
                        output.AppendLine(String.Format("WTSQueryUserToken() OK (hUserToken:{0})", hUserToken));

                        Process[] processes = Process.GetProcessesByName(userAppName);

                        if (processes.Length == 0)
                        {
                            output.AppendLine(String.Format("Application '{0}' can not be found in the running processes", userAppName));
                            return false;
                        }

                        Int32 userAppProcessId = -1;

                        for (Int32 k = 0; k < processes.Length; k++)
                        {
                            output.AppendLine(String.Format("Process: '{0}', PID: {1}, Handle: {2}, Session: {3}",
                                processes[k].ProcessName, processes[k].Id, processes[k].Handle, processes[k].SessionId));

                            if ((UInt32)processes[k].SessionId == dwSessionId)
                            {
                                userAppProcessId = processes[k].Id;
                            }
                        }

                        if (userAppProcessId == -1)
                        {
                            output.AppendLine(String.Format("Application '{0}' is not found in the processes of the current session", userAppName));
                            return false;
                        }

                        IntPtr hProcess = OpenProcess((Int32)MAXIMUM_ALLOWED, false, (UInt32)userAppProcessId);

                        IntPtr hPToken = IntPtr.Zero;

                        OpenProcessToken(hProcess,
                            TOKEN_ADJUST_PRIVILEGES
                            | TOKEN_QUERY
                            | TOKEN_DUPLICATE
                            | TOKEN_ASSIGN_PRIMARY
                            | TOKEN_ADJUST_SESSIONID
                            | TOKEN_READ
                            | TOKEN_WRITE,
                            ref hPToken);

                        if (hPToken != IntPtr.Zero)
                        {
                            output.AppendLine(String.Format("OpenProcessToken() OK (Token: {0})", hPToken));

                            LUID luid = new LUID();

                            if (LookupPrivilegeValue(IntPtr.Zero, SE_DEBUG_NAME, ref luid))
                            {
                                output.AppendLine(String.Format("LookupPrivilegeValue() OK (High: {0}, Low: {1})", luid.HighPart, luid.LowPart));

                                SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
                                sa.Length = Marshal.SizeOf(sa);

                                IntPtr hUserTokenDup = IntPtr.Zero;
                                DuplicateTokenEx(hPToken,
                                    (Int32)MAXIMUM_ALLOWED,
                                    ref sa,
                                    (Int32)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                                    (Int32)TOKEN_TYPE.TokenPrimary,
                                    ref hUserTokenDup);

                                if (hUserTokenDup != IntPtr.Zero)
                                {
                                    output.AppendLine(String.Format("DuplicateTokenEx() OK (hToken: {0})", hUserTokenDup));

                                    TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
                                    {
                                        PrivilegeCount = 1,
                                        Privileges = new Int32[3]
                                    };

                                    tp.Privileges[1] = luid.HighPart;
                                    tp.Privileges[0] = luid.LowPart;
                                    tp.Privileges[2] = SE_PRIVILEGE_ENABLED;

                                    //Adjust Token privilege
                                    if (SetTokenInformation(hUserTokenDup,
                                        TOKEN_INFORMATION_CLASS.TokenSessionId,
                                        ref dwSessionId,
                                        (UInt32)IntPtr.Size))
                                    {
                                        output.AppendLine(String.Format("SetTokenInformation() OK"));

                                        if (AdjustTokenPrivileges(hUserTokenDup,
                                            false, ref tp, Marshal.SizeOf(tp),
                                            IntPtr.Zero, IntPtr.Zero))
                                        {
                                            output.AppendLine("AdjustTokenPrivileges() OK");

                                            Int32 dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE;

                                            IntPtr pEnv = IntPtr.Zero;
                                            if (CreateEnvironmentBlock(ref pEnv, hUserTokenDup, true))
                                            {
                                                dwCreationFlags |= CREATE_UNICODE_ENVIRONMENT;
                                            }
                                            else
                                            {
                                                output.AppendLine(String.Format("CreateEnvironmentBlock() FAILED (Last Error: {0})", Marshal.GetLastWin32Error()));
                                                pEnv = IntPtr.Zero;
                                            }

                                            // Launch the process in the client's logon session.
                                            PROCESS_INFORMATION pi;

                                            STARTUPINFO si = new STARTUPINFO();
                                            si.cb = Marshal.SizeOf(si);
                                            si.lpDesktop = "winsta0\\default";

                                            output.AppendLine(String.Format("CreateProcess (Path:{0}, CurrDir:{1})", commandLine, workingDirectory));

                                            if (CreateProcessAsUser(hUserTokenDup,    // client's access token
                                                    null,                // file to execute
                                                    commandLine,        // command line
                                                    ref sa,                // pointer to process SECURITY_ATTRIBUTES
                                                    ref sa,                // pointer to thread SECURITY_ATTRIBUTES
                                                    false,                // handles are not inheritable
                                                    dwCreationFlags,    // creation flags
                                                    pEnv,                // pointer to new environment block 
                                                    workingDirectory,    // name of current directory 
                                                    ref si,                // pointer to STARTUPINFO structure
                                                    out pi                // receives information about new process
                                                ))
                                            {
                                                processStarted = true;
                                                output.AppendLine(String.Format("CreateProcessAsUser() OK (PID: {0})", pi.dwProcessId));
                                            }
                                            else
                                            {
                                                output.AppendLine(String.Format("CreateProcessAsUser() failed (Last Error: {0})", Marshal.GetLastWin32Error()));
                                            }
                                        }
                                        else
                                        {
                                            output.AppendLine(String.Format("AdjustTokenPrivileges() failed (Last Error: {0})", Marshal.GetLastWin32Error()));
                                        }
                                    }
                                    else
                                    {
                                        output.AppendLine(String.Format("SetTokenInformation() failed (Last Error: {0})", Marshal.GetLastWin32Error()));
                                    }

                                    CloseHandle(hUserTokenDup);
                                }
                                else
                                {
                                    output.AppendLine(String.Format("DuplicateTokenEx() failed (Last Error: {0})", Marshal.GetLastWin32Error()));
                                }
                            }
                            else
                            {
                                output.AppendLine(String.Format("LookupPrivilegeValue() failed (Last Error: {0})", Marshal.GetLastWin32Error()));
                            }

                            CloseHandle(hPToken);
                        }
                        else
                        {
                            output.AppendLine(String.Format("OpenProcessToken() failed (Last Error: {0})", Marshal.GetLastWin32Error()));
                        }

                        CloseHandle(hUserToken);
                    }
                    else
                    {
                        output.AppendLine(String.Format("WTSQueryUserToken failed: {0}", Marshal.GetLastWin32Error()));
                    }

                }
                catch (Exception ex)
                {
                    output.AppendLine("Exception occurred: " + ex.Message);
                }

                return processStarted;
            }

            #endregion
        }
    }
    
