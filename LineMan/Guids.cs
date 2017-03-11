// Guids.cs
// MUST match guids.h
using System;

namespace OlegShilo.LineMan
{
    static class GuidList
    {
        public const string guidLineManPkgString = "6ca2ecdf-a32a-45b3-b507-a0aac4fcb5a8";
        public const string guidLineManCmdSetString = "5217d436-ddf1-4b1f-8793-a60ac244c24e";

        public static readonly Guid guidLineManCmdSet = new Guid(guidLineManCmdSetString);

        public const string guidLineMan1PkgString = "6ca2ecdf-a32a-45b1-b507-a0aac4fcb5a8";
        public const string guidLineMan1CmdSetString = "5217d436-ddf1-421f-8793-a60ac244c24e";

        public static readonly Guid guidLineMan1CmdSet = new Guid(guidLineMan1CmdSetString);

        public const string guidLineMan2PkgString = "6ca21cdf-a32a-45b1-b507-a0aac4fcb5a8";
        public const string guidLineMan2CmdSetString = "5212d436-ddf1-421f-8793-a60ac244c24e";

        public static readonly Guid guidLineMan2CmdSet = new Guid(guidLineMan2CmdSetString);

        public const string guidLineMan3PkgString = "6c321cdf-a32a-45b1-b507-a0aac4fcb5a8";
        public const string guidLineMan3CmdSetString = "5212d433-ddf1-421f-8793-a60ac244c24e";

        public static readonly Guid guidLineMan3CmdSet = new Guid(guidLineMan3CmdSetString);
    };
}