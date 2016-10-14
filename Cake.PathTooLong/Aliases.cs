using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;

namespace Cake.PathTooLong
{
    /// <summary>
    /// Helpers to deal with PathToLong error in Win32
    /// </summary>
    [CakeAliasCategory("Helpers to deal with PathToLong error in Win32")]
    public static class PathToLongAliases
    {
        private static Func<IFileSystemInfo, bool> IncludeAllFiles => file => true;
        private static Func<IDirectory, bool> ScanAllDirectories => file => true;

        /// <summary>
        /// Recursivly walks through the directory looking for the files.
        /// </summary>
        /// <param name="ctx">Cake context</param>
        /// <param name="includeFilter">What files/directories should be included in result.</param>
        /// <param name="scanDirFilter">What directories should be scanned.</param>
        [CakeMethodAlias]
        public static IEnumerable<IFileSystemInfo> FindFilesRecursive(
            this ICakeContext ctx,
            Func<IFileSystemInfo, bool> includeFilter = null,
            Func<IDirectory, bool> scanDirFilter = null)
        {
            if (includeFilter == null) includeFilter = IncludeAllFiles;
            if (scanDirFilter == null) scanDirFilter = ScanAllDirectories;

            var root = ctx.FileSystem.GetDirectory(ctx.Environment.WorkingDirectory.FullPath);

            return FindFilesRecursive(ctx, root, includeFilter, scanDirFilter);
        }

        private static IEnumerable<IFileSystemInfo> FindFilesRecursive(
            ICakeContext context,
            IDirectory directory,
            Func<IFileSystemInfo, bool> includeFilter,
            Func<IDirectory, bool> scanDirPredicate)
        {
            context.Debug($"Step into dir: {directory.Path.FullPath}");

            var found = new List<IFileSystemInfo>();

            // Add files inside this directory
            var files = directory.GetFiles("*", SearchScope.Current);
            if (files.Any())
                found.AddRange(files);

            // Add directories inside this directory
            var directories = directory
                .GetDirectories("*", SearchScope.Current)
                .Where(scanDirPredicate);

            if (directories.Any())
            {
                found.AddRange(directories);

                // Continue recursive file scan
                foreach (var dirPath in directories.Select(dir => dir.Path.FullPath))
                {
                    var dir = context.FileSystem.GetDirectory(dirPath);
                    var foundDeeper = FindFilesRecursive(context, dir, includeFilter, scanDirPredicate);
                    found.AddRange(foundDeeper);
                }
            }
            files.First().Delete();
            return found.Where(includeFilter);
        }
    }

}