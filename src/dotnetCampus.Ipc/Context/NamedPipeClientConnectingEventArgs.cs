using System;

namespace dotnetCampus.Ipc.Context
{
    /// <summary>
    /// 命名管道客户端连接验证参数。
    /// </summary>
    public sealed class NamedPipeClientConnectingEventArgs : EventArgs
    {
        public NamedPipeClientConnectingEventArgs(string pipeName, int? clientProcessId)
        {
            PipeName = pipeName;
            ClientProcessId = clientProcessId;
        }

        /// <summary>
        /// 服务端管道名。
        /// </summary>
        public string PipeName { get; }

        /// <summary>
        /// 客户端进程 Id。非 Windows 或无法获取时为 <see langword="null"/>。
        /// </summary>
        public int? ClientProcessId { get; }

        /// <summary>
        /// 是否允许此次连接，默认允许。
        /// </summary>
        public bool AllowConnection { get; set; } = true;
    }
}
