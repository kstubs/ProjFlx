using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ProjectFlx.Utility;

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
                return (from a in _resources select a.RelativeName).ToList<string>();
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
            string name;
            if (Path.IsPathRooted(Resource))
                name = Resource;
            else
                name = Utility.Paths.CombinePaths(Root, Resource);

            IndexOf = -1;
            IndexOf = _resources.FindIndex(a => { return a.RelativeName.ToLower() == name.ToLower(); });

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
            return Utility.Paths.CombinePaths(_resources[Index].RelativeName);
        }

        /// <summary>
        /// Full URI path to resource - http://website.com/YourProjectFile/YourFile.xml
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        public string FullWebPath(int Index)
        {
            return Utility.Paths.CombinePaths(_source, _resources[Index].RelativeName);
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
                if (String.IsNullOrEmpty(Path.GetFileName(s)))
                    continue;

                var regobj = Regex.Match(Path.GetFileName(s), @"(\.\d{1,3})?(\.[a-zA-Z]{1,4})$");
                string name = Path.GetFileName(s);
                int version = 0;
                // Version + File Extension Match
                if (regobj.Groups[1].Success && regobj.Groups[2].Success)
                {
                    int.TryParse(regobj.Groups[1].Value.Substring(1), out version);
                    name = String.Format("{0}{1}", Path.GetFileName(s).Substring(0, regobj.Groups[1].Index), Path.GetExtension(s));
                }

                FileResource fr;
                resources.Add(fr = new FileResource()
                {
                    RelativeName = Paths.BeginWhack(s),
                    Extension = Path.GetExtension(s),
                    Name = name,
                    Host = Host,
                    Version = version
                });
            }
            return new FileResources(resources, Host, Root);
        }

        public static FileResources getFileResources(String ProjectFlxPath, String Host, String RelativePath)
        {
            List<FileResource> resources = new List<FileResource>();
            var dir = new DirectoryInfo(ProjectFlxPath);
            recurseResources(resources, new DirectoryObj() { Root = ProjectFlxPath.Substring(0, ProjectFlxPath.Length - RelativePath.Length), DirectoryInfo = dir }, Host);

            return new FileResources(resources, Host, RelativePath);
        }

        internal static void recurseResources(List<FileResource> resources, DirectoryObj DirObj, String Host)
        {
            foreach (var d in DirObj.DirectoryInfo.GetDirectories())
                recurseResources(resources, new DirectoryObj() { Root = DirObj.Root, DirectoryInfo = d }, Host);

            foreach (var f in DirObj.DirectoryInfo.GetFiles())
            {
                int version = 0;
                string name;
                var regobj = Regex.Match(f.FullName, @"(\.\d{1,3})?(\.[a-zA-Z]{1,4})$");
                var newresource = new FileResource();

                // Version + File Extension Match
                if (regobj.Groups[1].Success && regobj.Groups[2].Success)
                {
                    int.TryParse(regobj.Groups[1].Value.Substring(1), out version);
                    newresource.Version = version;
                    // strip version from fullname
                    name = String.Format("{0}{1}", f.FullName.Substring(0, regobj.Groups[1].Index), f.Extension);
                }
                else
                    name = f.FullName;

                resources.Add(new FileResource()
                {
                    RelativeName = Paths.BeginWhack(f.FullName.Substring(DirObj.Root.Length).ToLower().Replace("\\", "/")),
                    Name = name,
                    Host = Host,
                    Extension = Path.GetExtension(f.FullName.ToLower()),
                    Version = version
                });
            }
        }

        /// <summary>
        /// Collect resource from the subpath
        /// </summary>
        /// <param name="SubPath"></param>
        /// <param name="FileExtension"></param>
        /// <returns></returns>
        public List<string> collectResources(string SubPath, string FileExtension)
        {
            var name = Paths.BeginWhack(Paths.CombinePaths(Root, SubPath));
            var list = (from r in _resources
                         where r.getPath().ToLower().Equals(name.ToLower()) && (r.Extension.ToLower() == FileExtension.ToLower() || FileExtension == "*.*" || String.IsNullOrEmpty(FileExtension))
                         select r).ToList();

            var maxlist = new List<FileResource>();

            foreach (var resource in list)
            {
                var item = maxlist.FirstOrDefault(m => m.Name == resource.Name);
                if(item == null) {
                    maxlist.Add(resource);
                }
                else
                {
                    if(resource.Version > item.Version)
                    {
                        item.Version = resource.Version;
                        item.RelativeName = resource.RelativeName;
                        item.Name = resource.Name;
                    }
                }
            }

            // select Utility.Paths.CombinePaths(_root, r.FullName)
            var result = maxlist.Select(s => s.RelativeName);
            return result.ToList();

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
                        select Utility.Paths.CombinePaths(_root, r.RelativeName);
            var x = query.ToList();
            return query.ToList();
        }


        public static string _base { get; set; }
    }

    public class FileResource
    {
        internal string Host { get; set; }
        internal string Name { get; set; }
        internal string Extension { get; set; }
        internal string RelativeName { get; set; }
        internal int Version { get; set; }
        internal string getPath()
        {
            if (RelativeName.LastIndexOf("/") > 0)
                return RelativeName.Substring(0, RelativeName.LastIndexOf("/"));
            else
                return RelativeName;
        }
    }

    internal class DirectoryObj
    {
        internal string Root { get; set; }
        internal DirectoryInfo DirectoryInfo { get; set; }
    }

}
