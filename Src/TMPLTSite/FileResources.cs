using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ProjectFlx
{
    public interface IProjectResources
    {
        List<string> collectResources(string SubPath, string FileExtension);
    }

    public abstract class FileResourcesAbstract
    {
        protected string _source;
        protected string _root;
        protected List<FileResource> _resources;

        /// <summary>
        /// Host - http://website.com
        /// </summary>
        public string Host
        {
            get { return _source; }
            set { _source = value; }
        }

        public List<String> Resources
        {
            get
            {
                return (from a in _resources select a.FullName).ToList<string>();
            }
        }

        /// <summary>
        /// Subpath to resources - http://website.com/subpath
        /// </summary>
        public string Root
        {
            get
            {
                return _root;
            }
        }

        public int IndexOf { get; set; }
        public bool Exists(string Resource)
        {
            IndexOf = -1;
            IndexOf = _resources.FindIndex(a => { return a.FullName.ToLower().BeginWhack() == Resource.ToLower().BeginWhack(); });

            return IndexOf > -1;
        }

        public FileResource FileResource
        {
            get
            {
                try
                {
                    return _resources[IndexOf];
                }
                catch
                {
                    return null;
                }
            }


        }

        /// <summary>
        /// Absolute path for resource - /YourProjectFile/YourFile.xml
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        public string AbsolutePath(int Index)
        {
            return Extensions.CombinePaths(_root, _resources[Index].FullName);
        }

        /// <summary>
        /// Full URI path to resource - http://website.com/YourProjectFile/YourFile.xml
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        public string FullWebPath(int Index)
        {
            return Extensions.CombinePaths(_source, _root, _resources[Index].FullName);
        }

        public string FullWebPath(string Resource)
        {
            if (Exists(Resource))
            {
                return FullWebPath(IndexOf);
            }

            return null;
        }
    }
    /// <summary>
    /// Keeps track of ProjectFlx Resources and enables parsing and lookup
    /// </summary>
    public class FileResources : FileResourcesAbstract
    {
        /// <summary>
        /// Create new instance of FileResources with give Resources file
        /// </summary>
        /// <param name="Resources"></param>
        /// <param name="Host"></param>
        /// <param name="Root"></param>
        public FileResources(List<FileResource> Resources, String Host, String Root)
        {
            _source = Host;
            _root = Root;
            _resources = Resources;
        }

        public static FileResources getFileResources(List<String> Resources, String Host, String Root)
        {
            List<FileResource> resources = new List<FileResource>();
            foreach (String s in Resources)
            {
                FileResource fr;
                resources.Add(fr = new FileResource()
                {
                    FullName = s,
                    Extension = Path.GetExtension(s),
                    Name = Path.GetFileName(s)
                });

                if (fr.FullName.BeginWhack().StartsWith(Root.BeginWhack().Replace("\\", "/")))
                    fr.FullName = fr.FullName.Substring(Root.Length);
            }
            return new FileResources(resources, Host, Root);
        }

        public static FileResources getFileResources(String ProjectFlxPath, String Host, String Root)
        {
            List<FileResource> resources = new List<FileResource>();
            var dir = new DirectoryInfo(ProjectFlxPath);
            recurseResources(resources, new DirectoryObj() { Root = ProjectFlxPath, DirectoryInfo = dir });

            return new FileResources(resources, Host, Root);
        }

        internal static void recurseResources(List<FileResource> resources, DirectoryObj Dir)
        {
            foreach (var d in Dir.DirectoryInfo.GetDirectories())
                recurseResources(resources, new DirectoryObj() { Root = Dir.Root, DirectoryInfo = d } );

            foreach (var f in Dir.DirectoryInfo.GetFiles())
            {
                resources.Add(new FileResource()
                {
                    FullName = f.FullName.Substring(Dir.Root.Length).ToLower().Replace("\\", "/"),
                    Name = Path.GetFileName(f.FullName.Substring(Dir.Root.Length).ToLower()),
                    Extension = Path.GetExtension(f.FullName.ToLower())
                });
            }
        }

        /// <summary>
        /// Collecte resource from the subpath
        /// </summary>
        /// <param name="SubPath"></param>
        /// <param name="FileExtension"></param>
        /// <returns></returns>
        public List<string> collectResources(string SubPath, string FileExtension)
        {
            var query = from r in _resources
                        where r.getPath().BeginWhack().ToLower().Equals(SubPath.ToLower().BeginWhack()) && (r.Extension.ToLower() == FileExtension.ToLower() || FileExtension == "*.*" || String.IsNullOrEmpty(FileExtension))
                        select Extensions.CombinePaths(_root, r.FullName);
            var x = query.ToList();
            return query.ToList();
        }

        /// <summary>
        /// Collecte resource from the subpath
        /// </summary>
        /// <param name="SubPath"></param>
        /// <param name="FileExtension"></param>
        /// <returns></returns>
        public List<string> collectResources(string FileExtension)
        {
            var query = from r in _resources
                        where (r.Extension.ToLower() == FileExtension.ToLower() || FileExtension == "*.*" || String.IsNullOrEmpty(FileExtension))
                        select Extensions.CombinePaths(_root, r.FullName);
            var x = query.ToList();
            return query.ToList();
        }


        public static string _base { get; set; }
    }

    public class FileResource
    {
        internal string Name { get; set; }
        internal string Extension { get; set; }
        internal string FullName { get; set; }
        internal string getPath()
        {
            if (FullName.LastIndexOf("/") > 0)
                return FullName.Substring(0, FullName.LastIndexOf("/"));
            else
                return FullName;
        }
    }

    internal class DirectoryObj
    {
        internal string Root { get; set; }
        internal DirectoryInfo DirectoryInfo { get; set; }
    }

}
