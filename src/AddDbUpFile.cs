namespace KennethScott.AddDbUpFile
{
    using System;
    
    /// <summary>
    /// Helper class that exposes all GUIDs used across VS Package.
    /// </summary>
    internal sealed partial class PackageGuids
    {
        public const string guidAddDbUpFilePkgString = "FDDB1547-9DE3-4600-9E98-E8A15A1C6F3C";
        public const string guidAddDbUpFileCmdSetString = "660D3287-59AD-4508-8DE5-B938F2A983DA";
        public static Guid guidAddDbUpFilePkg = new Guid(guidAddDbUpFilePkgString);
        public static Guid guidAddDbUpFileCmdSet = new Guid(guidAddDbUpFileCmdSetString);
    }
    /// <summary>
    /// Helper class that encapsulates all CommandIDs uses across VS Package.
    /// </summary>
    internal sealed partial class PackageIds
    {
        public const int cmdidMyCommand = 0x0100;
    }
}
