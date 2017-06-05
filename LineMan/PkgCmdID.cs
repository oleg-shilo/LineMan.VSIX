// PkgCmdID.cs
// MUST match PkgCmdID.h
using System;

namespace OlegShilo.LineMan
{
    static class PkgCmdIDList
    {
        public const uint cmdidDuplicateLine = 0x100;
        public const uint cmdidDeleteLine = 0x101;
        public const uint cmdidLineUp = 0x102;
        public const uint cmdidLineDown = 0x103;
        public const uint cmdidToggleComments = 0x104;
        public const uint cmdidCommentDuplicateLine = 0x105;
    };
}