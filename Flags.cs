public enum CreationFlags
{
    DefaultErrorMode = 0x04000000,
    NewConsole = 0x00000010,
    NewProcessGroup = 0x00000200,
    SeparateWOWVDM = 0x00000800,
    Suspended = 0x00000004,
    UnicodeEnvironment = 0x00000400,
    ExtendedStartupInfoPresent = 0x00080000
}
public enum LogonFlags
{
    WithProfile = 1,
    NetCredentialsOnly
}

public enum PIPE_Token
{
    PIPE_ACCESS_DUPLEX = 0x3,
    PIPE_TYPE_BYTE = 0x0,
    PIPE_WAIT = 0x0,
    TOKEN_ALL_ACCESS = 0xF01FF,
    TOKENUSER = 1,
    SECURITY_IMPERSONATION = 2,
    TOKEN_PRIMARY = 1,
}