using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BundlerMinifier
{
    internal class ChangeHandler : IEquatable<ChangeHandler>
    {
        private static string[] _ignorePatterns = { "node_modules".AsPathSegment(), "bower_components".AsPathSegment(), "jspm_packages".AsPathSegment() };
        private readonly Bundle _bundle;
        private readonly string _configFile;
        private readonly BundleFileProcessor _processor;

        public ChangeHandler(BundleFileProcessor processor, string configFile, Bundle bundle)
        {
            _processor = processor;
            _configFile = configFile;
            _bundle = bundle;
        }

        public Bundle Bundle => _bundle;

        public bool Equals(ChangeHandler other)
        {
            return other != null
                && string.Equals(_bundle.OutputFileName, other._bundle.OutputFileName, StringComparison.Ordinal)
                && _bundle.InputFiles.Count == other._bundle.InputFiles.Count
                && SetCompare(_bundle.InputFiles, other._bundle.InputFiles, StringComparer.Ordinal)
                && string.Equals(_bundle.SourceMapRootPath, other._bundle.SourceMapRootPath, StringComparison.Ordinal)
                && _bundle.SourceMap == other._bundle.SourceMap
                && SetCompare(_bundle.Minify, other._bundle.Minify, (l, r) => string.Equals(l.Key, r.Key, StringComparison.Ordinal) && Equals(l.Value, r.Value));
        }

        private class DelegateToComparer<T> : IEqualityComparer<T>
        {
            private Func<T, T, bool> _equals;
            private Func<T, int> _getHashCode;

            public DelegateToComparer(Func<T, T, bool> equals, Func<T, int> getHashCode)
            {
                _equals = equals;
                _getHashCode = getHashCode ?? (x => 0);
            }

            public bool Equals(T x, T y)
            {
                return _equals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return _getHashCode(obj);
            }
        }

        private bool SetCompare<T>(IEnumerable<T> left, IEnumerable<T> right, Func<T, T, bool> comparer, Func<T, int> getHashCode = null)
        {
            return SetCompare(left, right, new DelegateToComparer<T>(comparer, getHashCode));
        }

        private bool SetCompare<T>(IEnumerable<T> left, IEnumerable<T> right, IEqualityComparer<T> comparer)
        {
            if(left == null && right == null)
            {
                return true;
            }

            if((left != null) != (right != null))
            {
                return false;
            }

            HashSet<T> ls = new HashSet<T>(left, comparer);
            HashSet<T> rs = new HashSet<T>(right, comparer);

            ls.SymmetricExceptWith(rs);
            return ls.Count == 0;
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(_bundle.OutputFileName);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ChangeHandler);
        }

        public bool FilesChanged(FileSystemEventArgs e)
        {
            if (!IsFileValid(e.FullPath))
            {
                return false;
            }

            if (!BundleFileProcessor.IsFileConfigured(_configFile, e.FullPath).Any())
            {
                return false;
            }

            var inputs = _bundle.GetAbsoluteInputFiles();
            var inputLastModified = inputs.Count > 0 ? inputs.Max(inputFile => File.GetLastWriteTimeUtc(inputFile)) : DateTime.MaxValue;

            if ((_bundle.GetAbsoluteInputFiles().Count > 1 || _bundle.InputFiles.FirstOrDefault() != _bundle.OutputFileName)
                && inputLastModified > File.GetLastWriteTimeUtc(_bundle.GetAbsoluteOutputFile()))
            {
                _processor.Process(_configFile, new Bundle[] { _bundle });
            }

            return true;
        }

        private bool IsFileValid(string file)
        {
            string fileName = Path.GetFileName(file);

            // VS adds ~ to temp file names so let's ignore those
            if (fileName.Contains('~') || fileName.Contains(".min."))
                return false;

            if (_ignorePatterns.Any(p => file.IndexOf(p) > -1))
            {
                //var fsw = (FileSystemWatcher)sender;
                //fsw.EnableRaisingEvents = false;
                return false;
            }

            if (!BundleFileProcessor.IsSupported(file))
                return false;

            return true;
        }
    }
}