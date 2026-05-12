using System.IO;

namespace MRModuleEditor.Core.Serialization
{
    public static class ModuleFilePaths
    {
        public const string SampleModuleRelativePathFromAssets =
            "MRModuleEditor/Samples/SampleModules/ForwardKinematicsMini/module.json";
        
        public static string FromAssetsDirectory(string assetsDirectoryAbsolutePath, string relativePathFromAssets)
        {
            return Path.Combine(assetsDirectoryAbsolutePath, relativePathFromAssets);
        }
    }
}